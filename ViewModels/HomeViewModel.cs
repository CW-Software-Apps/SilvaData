using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using ISIInstitute.Views.LoteViews;

using SilvaData.FontAwesome;
using SilvaData.Models;
using SilvaData.Pages.PopUps;
using SilvaData.Services; // Para HomeService
using SilvaData.Utilities;

using System.Collections.ObjectModel;
using System.Diagnostics;

namespace SilvaData.ViewModels
{
    /// <summary>
    /// ViewModel para a tela principal (Home), que exibe os cart�es de
    /// pontua��o e os lotes em alerta.
    /// </summary>
    public partial class HomeViewModel : ViewModelBase, IDisposable
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MeusResultadosVisible))]
        [NotifyPropertyChangedFor(nameof(EmpresaVisible))]
        [NotifyPropertyChangedFor(nameof(GlobalVisible))]
        [NotifyPropertyChangedFor(nameof(MeusResultadosTextColor))]
        [NotifyPropertyChangedFor(nameof(EmpresaTextColor))]
        [NotifyPropertyChangedFor(nameof(GlobalTextColor))]
        int selectedIndex = 0;

        [ObservableProperty]
        private ObservableCollection<Lote> lotesEmAlerta = new();

        [ObservableProperty]
        private DashboardMedia dadosDashboard = new();

        private readonly HomeService _homeService;
        private bool _isDisposed = false;

        private readonly SemaphoreSlim _carregaDadosLock = new(1, 1);
        private DateTime _ultimaExecucaoConcluida = DateTime.MinValue;
        private const int MinimoTempoEntreExecucoes = 2000;

        public HomeViewModel(HomeService homeService)
        {
            _homeService = homeService; // Injeta o novo servi�o
        }

        public async Task InitializeAsync(bool forceRefresh = false)
        {
            if (IsBusy || _isDisposed) return;

            try
            {
                IsBusy = true;
                await CarregaDados(forceRefresh); // Usa a vers�o segura que voc� j� tem
                Debug.WriteLine("[Home] Inicializa��o conclu�da");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Home] Erro na inicializa��o: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        #region Dispose
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;
            if (disposing)
            {
                WeakReferenceMessenger.Default.UnregisterAll(this);
            }
            _isDisposed = true;
        }
        #endregion

        public async Task CarregaDados(bool forceWebFetch = false)
        {
            // 1. Ignora se j� est� em execu��o (prote��o imediata)
            if (IsBusy)
            {
                Debug.WriteLine("CarregaDados ignorado: j� em execu��o");
                return;
            }

            // 2. Ignora se dentro do per�odo de cooldown (a menos que seja for�ado)
            if (!forceWebFetch &&
                (DateTime.Now - _ultimaExecucaoConcluida).TotalMilliseconds < MinimoTempoEntreExecucoes)
            {
                Debug.WriteLine($"CarregaDados ignorado: cooldown ativo ({MinimoTempoEntreExecucoes}ms)");
                return;
            }

            // 3. Tenta adquirir lock com timeout (evita deadlock)
            if (!await _carregaDadosLock.WaitAsync(TimeSpan.FromMilliseconds(500)))
            {
                Debug.WriteLine("CarregaDados ignorado: lock ocupado por outra opera��o");
                return;
            }

            try
            {
                // 4. Re-verifica��o cr�tica ap�s adquirir o lock
                if (!forceWebFetch &&
                    (DateTime.Now - _ultimaExecucaoConcluida).TotalMilliseconds < MinimoTempoEntreExecucoes)
                {
                    Debug.WriteLine("CarregaDados ignorado: cooldown verificado ap�s lock");
                    return;
                }

                await CarregaDadosInterno(forceWebFetch);
            }
            finally
            {
                _carregaDadosLock.Release(); // Libera o lock SEMPRE
            }
        }

        /// <summary>
        /// Carrega os dados de resumo do Dashboard e os lotes em alerta.
        /// </summary>
        public async Task CarregaDadosInterno(bool forceWebFetch = false)
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                var ultimaAtualizacao = Preferences.Get("UltimaAtualizacaoTodasMediasISIMacro", DateTime.MinValue);
                if (ultimaAtualizacao == DateTime.MinValue)
                {
                    // Executa em background para n�o travar a UI
                    await Task.Run(Lote.AtualizaTodasMediasISIMacro).ConfigureAwait(false);
                }

                // Busca Lotes em Alerta
                var lotesEmAlertaList = await Lote.PegaListaLotesEmAlertaAsync().ConfigureAwait(false);
                // Garante nomes (UE/Propriedade/Regional) caso a consulta mude e n�o traga JOINs
                lotesEmAlertaList.EnsureNames();

                // Busca Dados do Dashboard (do cache ou web) usando o servi�o
                var dashboardData = await _homeService.AtualizaDadosMediaAsync(forceWebFetch).ConfigureAwait(false);

                // Atualiza a UI na thread principal
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    LotesEmAlerta.Clear();
                    foreach (var lote in lotesEmAlertaList)
                    {
                        LotesEmAlerta.Add(lote);
                    }

                    DadosDashboard = dashboardData;
                    AtualizaUI(); // Atualiza as propriedades calculadas (Media, Score, etc.)
                });

                _ultimaExecucaoConcluida = DateTime.Now; // Atualiza timestamp S� SE CONCLUIR
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao carregar dados do HomeViewModel: {ex.Message}");
                SentryHelper.CaptureExceptionWithUser(ex, ISIWebService.Instance.LoggedUser.nome, "CarregaDados");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Comando chamado pelo bot�o "Atualizar". Solicita ao Dashboard (pai)
        /// para iniciar uma sincroniza��o completa.
        /// </summary>
        [RelayCommand]
        private async Task PerformAtualizaAgora()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                // Obt�m o DashboardViewModel diretamente (sem mensagens)
                var dashboardVm = ServiceHelper.GetRequiredService<DashboardViewModel>();
                if (dashboardVm != null)
                {
                    await dashboardVm.InitializeAsync(forceRefresh: true);
                }
                else
                {
                    await PopUpOK.ShowAsync(Traducao.Aten��o, "Dashboard n�o carregado");
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        #region Comandos de Abas (Meus Resultados, Empresa, Global)

        [RelayCommand]
        public void MeusResultados() => SelectTab(0);

        [RelayCommand]
        public void Empresa() => SelectTab(1);

        [RelayCommand]
        public void Global() => SelectTab(2);

        private void SelectTab(int index)
        {
            SelectedIndex = index;
            AtualizaUI();
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

        public void AtualizaUI()
        {
            OnPropertyChanged(nameof(LotesEmAlerta)); // Notifica a lista
            AtualizaMedia();
        }

        public void AtualizaMedia()
        {
            if (DadosDashboard == null) return;

            var media = MeusResultadosVisible ? DadosDashboard.mediaIsiMacroClienteUsuario : EmpresaVisible ? DadosDashboard.mediaIsiMacroCliente : DadosDashboard.mediaIsiMacroGlobal;
            ISIScoreTotal = media;

            if (MeusResultadosVisible)
            {
                if (DadosDashboard.mediaIsiMacroClienteUsuario <= DadosDashboard.mediaIsiMacroCliente)
                {
                    MediaTextColor = ISIUtils.GetResourceColor("MediaBoaTextColor", Colors.Black);
                    MediaBackground = ISIUtils.GetResourceColor("MediaBoaBackground", Colors.Transparent);
                    MediaIcon = (DadosDashboard.mediaIsiMacroClienteUsuario < DadosDashboard.mediaIsiMacroCliente) ? FontAwesomeIcons.CircleArrowUp : FontAwesomeIcons.CirclePause;
                    TextoMedia = (DadosDashboard.mediaIsiMacroClienteUsuario < DadosDashboard.mediaIsiMacroCliente) ? $"{DoubleToPercentageString(CalculateChange(DadosDashboard.mediaIsiMacroCliente, DadosDashboard.mediaIsiMacroClienteUsuario))} " + Traducao.Melhor : Traducao.MesmoValor;
                }
                else
                {
                    MediaTextColor = ISIUtils.GetResourceColor("MediaRuimTextColor", Colors.Black);
                    MediaBackground = ISIUtils.GetResourceColor("MediaRuimBackground", Colors.Transparent);
                    MediaIcon = FontAwesomeIcons.CircleArrowDown;
                    TextoMedia = $"{DoubleToPercentageString(CalculateChange(DadosDashboard.mediaIsiMacroCliente, DadosDashboard.mediaIsiMacroClienteUsuario))} " + Traducao.Pior;
                }
                TextoMedia2 = Traducao.QueAM�diaDaEmpresa;
            }
            else if (EmpresaVisible)
            {
                if (DadosDashboard.mediaIsiMacroCliente <= DadosDashboard.mediaIsiMacroGlobal)
                {
                    MediaTextColor = ISIUtils.GetResourceColor("MediaBoaTextColor", Colors.Black);
                    MediaBackground = ISIUtils.GetResourceColor("MediaBoaBackground", Colors.Transparent);
                    MediaIcon = (DadosDashboard.mediaIsiMacroCliente < DadosDashboard.mediaIsiMacroGlobal) ? FontAwesomeIcons.CircleArrowUp : FontAwesomeIcons.CirclePause;
                    TextoMedia = (DadosDashboard.mediaIsiMacroCliente < DadosDashboard.mediaIsiMacroGlobal) ? $"{DoubleToPercentageString(CalculateChange(DadosDashboard.mediaIsiMacroGlobal, DadosDashboard.mediaIsiMacroCliente))} " + Traducao.Melhor : Traducao.MesmoValor;
                }
                else
                {
                    MediaTextColor = ISIUtils.GetResourceColor("MediaRuimTextColor", Colors.Black);
                    MediaBackground = ISIUtils.GetResourceColor("MediaRuimBackground", Colors.Transparent);
                    MediaIcon = FontAwesomeIcons.CircleArrowDown;
                    TextoMedia = $"{DoubleToPercentageString(CalculateChange(DadosDashboard.mediaIsiMacroGlobal, DadosDashboard.mediaIsiMacroCliente))} " + Traducao.Pior;
                }
                TextoMedia2 = Traducao.QueAM�diaGlobal;
            }
            else // GlobalVisible
            {
                // Reseta para valores padr�o
                MediaTextColor = ISIUtils.GetResourceColor("TextEnabled", Colors.Black);
                MediaBackground = Colors.Transparent;
                MediaIcon = string.Empty;
                TextoMedia = string.Empty;
                TextoMedia2 = string.Empty;
            }
        }

        public bool MeusResultadosVisible => SelectedIndex == 0;
        public bool EmpresaVisible => SelectedIndex == 1;
        public bool GlobalVisible => SelectedIndex == 2;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ISIScoreTextColor))]
        [NotifyPropertyChangedFor(nameof(ISIScoreText))]
        private double iSIScoreTotal;

        public Color ISIScoreTextColor => ISIMacro.StatusColor(ISIScoreTotal);
        public string ISIScoreText => ISIMacro.StatusText(ISIScoreTotal);

        public Color MeusResultadosTextColor => SelectedIndex == 0 ? Colors.White : ISIUtils.GetResourceColor("QuaseBranco", Colors.LightGray);
        public Color EmpresaTextColor => SelectedIndex == 1 ? Colors.White : ISIUtils.GetResourceColor("QuaseBranco", Colors.LightGray);
        public Color GlobalTextColor => SelectedIndex == 2 ? Colors.White : ISIUtils.GetResourceColor("QuaseBranco", Colors.LightGray);

        [ObservableProperty] private Color mediaTextColor = Colors.White;
        [ObservableProperty] private Color mediaBackground = Colors.Transparent;
        [ObservableProperty] private string textoMedia = string.Empty;
        [ObservableProperty] private string textoMedia2 = string.Empty;
        [ObservableProperty] private string mediaIcon = string.Empty;

        double CalculateChange(double previous, double current)
        {
            if (previous == 0) return (current > 0) ? 1 : 0;
            var change = current - previous;
            return Math.Abs(change / previous);
        }

        string DoubleToPercentageString(double d) => $"{d * 100f:N1}%";

        [RelayCommand]
        public void GraficoScore()
        {
            WeakReferenceMessenger.Default.Send(new ShowGraficoMessage(DashboardTipoGrafico.ISIScoreTotal));
        }

        [RelayCommand]
        public void GraficoDispesao()
        {
            WeakReferenceMessenger.Default.Send(new ShowGraficoMessage(DashboardTipoGrafico.ISIDispersaoScore));
        }

        [RelayCommand]
        public void GraficoAcometimento()
        {
            WeakReferenceMessenger.Default.Send(new ShowGraficoMessage(DashboardTipoGrafico.Acometimento));
        }
    }
}
