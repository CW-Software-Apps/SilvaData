using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SilvaData.FontAwesome;
using SilvaData.Models;
using SilvaData.Pages.PopUps;
using SilvaData.Services;
using ISIInstitute.Views.LoteViews;
using Newtonsoft.Json;
using Microsoft.Maui.Storage; // Preferences
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq; // LINQ para ordena��o

namespace SilvaData.ViewModels
{
    public class AtualizarGraficosMessage
    {
        public Graficos Graficos { get; }
        public AtualizarGraficosMessage(Graficos graficos) => Graficos = graficos;
    }

    /// <summary>
    /// ViewModel principal da Dashboard (abas Meus Resultados/Empresa/Global).
    /// Migrado de HomeViewModel incorporando l�gica de compara��o, mensagens e carregamento de dados.
    /// </summary>
    public partial class DashboardViewModel : ViewModelBase, IDisposable
    {
        private readonly GraficosService _graficosService;
        private bool _disposed;
        // Throttle contra loops reativos
        private DateTime _lastFullReload = DateTime.MinValue;
        private static readonly TimeSpan MinReloadInterval = TimeSpan.FromSeconds(3);

        // Sub-ViewModels dos gr�ficos (inje��o)
        [ObservableProperty] private GraficoResultadosViewModel meusResultadosViewModel;
        [ObservableProperty] private GraficoResultadosViewModel empresaViewModel;
        [ObservableProperty] private GraficoResultadosViewModel globalViewModel;

        // Dados do Dashboard (m�dias cacheadas)
        [ObservableProperty] private DashboardMedia dadosDashboard = new();

        // Lotes em Alerta
        [ObservableProperty] private ObservableCollection<Lote> lotesEmAlerta = new();
        private DateTime _lastAlertRefresh = DateTime.MinValue;
        private bool _updatingAlerts; // evita reentr�ncia

        // Score total da aba atual
        [ObservableProperty] private double iSIScoreTotal;
        public string ISIScoreText => ISIMacro.StatusText(ISIScoreTotal);
        public Color ISIScoreTextColor => ISIMacro.StatusColor(ISIScoreTotal);

        // Compara��o
        [ObservableProperty] private Color mediaBackground = Colors.Transparent;
        [ObservableProperty] private Color mediaTextColor = Colors.Gray;
        [ObservableProperty] private string mediaIcon = string.Empty;
        [ObservableProperty] private string textoMedia = string.Empty;
        [ObservableProperty] private string textoMedia2 = string.Empty;

        // Visibilidades
        [ObservableProperty] private bool meusResultadosVisible;
        [ObservableProperty] private bool empresaVisible;
        [ObservableProperty] private bool globalVisible;

        // Tab selecionada
        [ObservableProperty] private int tabIndexSelecionado;

        // Lazy loading das abas (criadas apenas na primeira visita)
        [ObservableProperty] private bool isEmpresaTabLoaded;
        [ObservableProperty] private bool isGlobalTabLoaded;

        // Visibilidade calculada de cada aba (notificada em OnTabIndexSelecionadoChanged)
        public bool IsMeusResultadosTabVisible => TabIndexSelecionado == 0;
        public bool IsEmpresaTabVisible        => TabIndexSelecionado == 1;
        public bool IsGlobalTabVisible         => TabIndexSelecionado == 2;

        public DashboardViewModel(
            GraficosService graficosService,
            GraficoResultadosViewModel meusResultadosViewModel,
            GraficoResultadosViewModel empresaViewModel,
            GraficoResultadosViewModel globalViewModel)
        {
            _graficosService = graficosService;
            MeusResultadosViewModel = meusResultadosViewModel;
            EmpresaViewModel = empresaViewModel;
            GlobalViewModel = globalViewModel;

            // Mensagens
            WeakReferenceMessenger.Default.Register<ISIMacroSalvoMessage>(this, async (r, m) => await RefreshDashboard());
            WeakReferenceMessenger.Default.Register<ISIMacroScoreMedioAtualizadoMessage>(this, async (r, m) => await RefreshDashboard());
            WeakReferenceMessenger.Default.Register<RequestDashboardRefreshMessage>(this, async (r, m) => await RefreshDashboard());
            WeakReferenceMessenger.Default.Register<AtualizarGraficosMessage>(this, (r, m) => OnGraficosAtualizados(m.Graficos));

            WeakReferenceMessenger.Default.Register<UpdateDadosIniciaisMessage>(this, async (r, m) => await ForceReloadAsync());
            WeakReferenceMessenger.Default.Register<ShowDashboardMessage>(this, async (r, m) => await OnShowDashboardAsync());
        }

        public async Task InitializeAsync(bool forceRefresh = false)
        {
            if (IsBusy || _disposed) return;

            try
            {
                IsBusy = true;
                await AtualizaDadosGraficos(forceRefresh); // J� inclui CarregaLotesEmAlerta
                Debug.WriteLine("[Dashboard] Inicializa��o conclu�da");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Dashboard] Erro na inicializa��o: {ex.Message}");
                // N�o crasha a app, s� loga
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RefreshDashboard() => await AtualizaDadosGraficos(false);
        private void OnGraficosAtualizados(Graficos g) { /* j� tratado */ }

        private async Task OnShowDashboardAsync()
        {
            if (TabIndexSelecionado != 0)
                TabIndexSelecionado = 0;
            await ForceReloadAsync();
        }

        /// <summary>
        /// Comando para atualizar manualmente os dados dos gr�ficos (Sync/Refresh externo).
        /// </summary>
        [RelayCommand]
        public async Task AtualizaDadosGraficos(bool showError = true)
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                string resultado = await _graficosService.AtualizaDadosGraficos(showError);
                if (!string.IsNullOrEmpty(resultado) && showError)
                    await Snackbar.Make(resultado, duration: TimeSpan.FromSeconds(8), visualOptions: new SnackbarOptions
                    {
                        BackgroundColor = Color.FromArgb("#0D3D72"),
                        TextColor = Colors.White,
                        ActionButtonTextColor = Color.FromArgb("#41ABD9"),
                        CornerRadius = new CornerRadius(14),
                        CharacterSpacing = 0.2
                    }).Show();

                // Carrega DashboardMedia do cache (Preferences)
                var dadosDashboardJson = Preferences.Get("DadosDashboard", "");
                DashboardMedia cache = new();
                if (!string.IsNullOrEmpty(dadosDashboardJson))
                {
                    try { cache = JsonConvert.DeserializeObject<DashboardMedia>(dadosDashboardJson) ?? new(); } catch { cache = new(); }
                }
                DadosDashboard = cache;

                WeakReferenceMessenger.Default.Send(new AtualizarGraficosMessage(_graficosService.Graficos));
                AtualizaUI();
                await CarregaLotesEmAlerta();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro AtualizaDadosGraficos: {ex.Message}");
                if (showError)
                    await Snackbar.Make($"Erro ao atualizar dados: {ex.Message}", duration: TimeSpan.FromSeconds(8), visualOptions: new SnackbarOptions
                    {
                        BackgroundColor = Color.FromArgb("#0D3D72"),
                        TextColor = Colors.White,
                        ActionButtonTextColor = Color.FromArgb("#41ABD9"),
                        CornerRadius = new CornerRadius(14),
                        CharacterSpacing = 0.2
                    }).Show();
            }
            finally { IsBusy = false; }
        }

        // NOVO: for�a recarga completa (dados + lotes em alerta) ignorando popups de erro.
        private async Task ForceReloadAsync()
        {
            try
            {
                if (IsBusy) return;
                if (DateTime.UtcNow - _lastFullReload < MinReloadInterval) return;
                _lastFullReload = DateTime.UtcNow;
                await AtualizaDadosGraficos(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Dashboard] ForceReloadAsync erro: {ex.Message}");
            }
        }

        private async Task CarregaLotesEmAlerta()
        {
            try
            {
                if (_updatingAlerts) return; // prote��o reentrante
                _updatingAlerts = true;
                if ((DateTime.UtcNow - _lastAlertRefresh).TotalSeconds < 3) return;
                _lastAlertRefresh = DateTime.UtcNow;

                var lotes = await Lote.PegaListaLotesEmAlertaAsync();
                var ordenados = lotes
                    .Where(l => l != null)
                    .OrderByDescending(l => l.ISIMacroScoreMedio)
                    .ToList();

                LotesEmAlerta.Clear();
                foreach (var l in ordenados)
                      LotesEmAlerta.Add(l);

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro seguro CarregaLotesEmAlerta: {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(() => LotesEmAlerta.Clear());
            }
            finally
            {
                _updatingAlerts = false;
            }
        }

        private void AtualizaUI()
        {
            if (DadosDashboard == null) return;
            switch (TabIndexSelecionado)
            {
                case 0:
                    ISIScoreTotal = DadosDashboard.mediaIsiMacroClienteUsuario;
                    MeusResultadosVisible = true; EmpresaVisible = false; GlobalVisible = false;
                    AtualizaMedia(DadosDashboard.mediaIsiMacroClienteUsuario, DadosDashboard.mediaIsiMacroCliente, true);
                    break;
                case 1:
                    ISIScoreTotal = DadosDashboard.mediaIsiMacroCliente;
                    MeusResultadosVisible = false; EmpresaVisible = true; GlobalVisible = false;
                    AtualizaMedia(DadosDashboard.mediaIsiMacroCliente, DadosDashboard.mediaIsiMacroGlobal, false);
                    break;
                case 2:
                    ISIScoreTotal = DadosDashboard.mediaIsiMacroGlobal;
                    MeusResultadosVisible = false; EmpresaVisible = false; GlobalVisible = true;
                    ResetMedia();
                    break;
            }
        }

        private void AtualizaMedia(double mediaAtual, double mediaComparacao, bool comparaComEmpresa)
        {
            double percentual = CalculateChange(mediaComparacao, mediaAtual);
            bool melhorOuIgual = mediaAtual <= mediaComparacao;
            if (melhorOuIgual)
            {
                MediaTextColor = ISIUtils.GetResourceColor("MediaBoaTextColor", Colors.Green);
                MediaBackground = ISIUtils.GetResourceColor("MediaBoaBackground", Color.FromArgb("#E8F5E9"));
                if (mediaAtual < mediaComparacao)
                {
                    MediaIcon = FontAwesomeIcons.CircleArrowUp;
                    TextoMedia = $"{DoubleToPercentageString(percentual)} {Traducao.Melhor}";
                }
                else
                {
                    MediaIcon = FontAwesomeIcons.CirclePause;
                    TextoMedia = Traducao.MesmoValor;
                }
            }
            else
            {
                MediaTextColor = ISIUtils.GetResourceColor("MediaRuimTextColor", Colors.Red);
                MediaBackground = ISIUtils.GetResourceColor("MediaRuimBackground", Color.FromArgb("#FFEBEE"));
                MediaIcon = FontAwesomeIcons.CircleArrowDown;
                TextoMedia = $"{DoubleToPercentageString(percentual)} {Traducao.Pior}";
            }
            TextoMedia2 = comparaComEmpresa ? Traducao.QueAM�diaDaEmpresa : Traducao.QueAM�diaGlobal;
        }

        private void ResetMedia()
        {
            MediaTextColor = ISIUtils.GetResourceColor("TextEnabled", Colors.Gray);
            MediaBackground = Colors.Transparent;
            MediaIcon = string.Empty; TextoMedia = string.Empty; TextoMedia2 = string.Empty;
        }

        private double CalculateChange(double previous, double current)
        {
            if (previous == 0) return current > 0 ? 1 : 0;
            var change = current - previous; return Math.Abs(change / previous);
        }
        private string DoubleToPercentageString(double d) => $"{d * 100f:N1}%";

        partial void OnTabIndexSelecionadoChanged(int value)
        {
            AtualizaUI();
            OnPropertyChanged(nameof(IsMeusResultadosTabVisible));
            OnPropertyChanged(nameof(IsEmpresaTabVisible));
            OnPropertyChanged(nameof(IsGlobalTabVisible));
            if (value == 1) IsEmpresaTabLoaded = true;
            if (value == 2) IsGlobalTabLoaded = true;
        }

        #region Comandos de navega��o e intera��o

        [RelayCommand]
        private async Task ShowUsuario(View button)
        { if (button == null) return; var popup = new PopUpUsuario(); await Task.Yield(); popup.AnchorX = button.X; popup.AnchorY = button.Y; await NavigationUtils.ShowPopupAsync(popup); }
        [RelayCommand] private void MeusResultados() => TabIndexSelecionado = 0;
        [RelayCommand] private void Empresa() => TabIndexSelecionado = 1;
        [RelayCommand] private void Global() => TabIndexSelecionado = 2;

        // Abre p�gina de gr�ficos com tipo inicial
        [RelayCommand]
        private async Task AbrirGraficoAsync(DashboardTipoGrafico tipo)
        {
            try
            {
                var page = new GraficoResultadosPage(tipo);
                await NavigationUtils.ShowPageAsModalAsync(page);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro AbrirGraficoAsync: {ex.Message}");
            }
        }

        [RelayCommand] private async Task GraficoScore() => await AbrirGraficoAsync(DashboardTipoGrafico.ISIScoreTotal);
        [RelayCommand] private async Task GraficoDispersao() => await AbrirGraficoAsync(DashboardTipoGrafico.ISIDispersaoScore);
        [RelayCommand] private async Task GraficoAcometimento() => await AbrirGraficoAsync(DashboardTipoGrafico.Acometimento);

        public void Dispose()
        {
            if (_disposed) return; WeakReferenceMessenger.Default.UnregisterAll(this); _disposed = true;
        }
        #endregion

        [RelayCommand]
        public async Task VaiParaLote(Lote lote)
        {
            if (lote == null) return;
            var monitoramentoVm = ServiceHelper.GetRequiredService<LoteMonitoramentoViewModel>();
            monitoramentoVm.SetInitialState(lote);
            await NavigationUtils.ShowViewAsModalAsync<LoteMonitoramentoView>().ConfigureAwait(false);
        }

    }
}
