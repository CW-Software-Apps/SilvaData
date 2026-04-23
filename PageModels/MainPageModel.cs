using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using ISIInstitute.Views;

using SilvaData.Models;
using SilvaData.Pages.PopUps;
using SilvaData.ViewModels;

using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Storage;

using System.Diagnostics;

namespace SilvaData.PageModels
{
    /// <summary>
    /// ViewModel principal da aplicação, responsável pela navegação (Tabs)
    /// e pela lógica de inicialização/autenticação do app.
    /// MIGRADO: Usa CacheService ao invés de DadosStatic.
    /// </summary>
    public partial class MainPageModel : PageModelBase
    {
        private readonly SemaphoreSlim _initializationLock = new(1, 1); // 2. Lock thread-safe
        private readonly IDispatcher _dispatcher; // 3. Para UI thread
        private readonly ISIWebService _ISIWebService; // 4. Injeção de dependência

        private readonly CacheService _cacheService; // ADICIONADO
        private bool _jaInicializou = false;

        [ObservableProperty]
        private string loadingText = "Loading...";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MercadosSelected))]
        [NotifyPropertyChangedFor(nameof(GalpoesSelected))]
        [NotifyPropertyChangedFor(nameof(LotesSelected))]
        [NotifyPropertyChangedFor(nameof(SyncSelected))]
        [NotifyPropertyChangedFor(nameof(ContaSelected))]
        private int selectedIndex;

        // Propriedades calculadas para os estilos das Tabs
        public bool MercadosSelected => SelectedIndex == 0;
        public bool GalpoesSelected => SelectedIndex == 1;
        public bool LotesSelected => SelectedIndex == 2;
        public bool SyncSelected => SelectedIndex == 3;
        public bool ContaSelected => SelectedIndex == 4;

        // ★ Badge do Sync
        [ObservableProperty]
        private string syncPendingCount = string.Empty;

        SincronizacaoPendentesViewModel sincronizacaoPendentesViewModel;

        public MainPageModel(CacheService cacheService, IDispatcher dispatcher, ISIWebService webService, SincronizacaoPendentesViewModel syncVm)
        {
            _cacheService = cacheService;

            _dispatcher = dispatcher;
            _ISIWebService = webService;

            sincronizacaoPendentesViewModel = syncVm;

            // Registra os ouvintes de mensagens para mudar de aba
            WeakReferenceMessenger.Default.Register<ShowLotesMessage>(this, (r, m) => ShowLotes());
            WeakReferenceMessenger.Default.Register<ShowDashboardMessage>(this, (r, m) => ShowMercados());
            WeakReferenceMessenger.Default.Register<ShowSyncMessage>(this, (r, m) => ShowSync());
            WeakReferenceMessenger.Default.Register<ShowSuporteMessage>(this, (r, m) => ShowConta());

            // Recebe mudanças de total de pendências (badge)
            WeakReferenceMessenger.Default.Register<SyncPendentesTotalChangedMessage>(this, (r, m) =>
            {
                _dispatcher.Dispatch(() =>
                {
                    SyncPendingCount = $"{(m.Total>0?m.Total:string.Empty)}";
                });
            });
        }

        #region Lógica de Navegação (Tabs)

        public void ShowMercados() => SelectedIndex = 0;
        public void ShowGalpoes() => SelectedIndex = 1;
        public void ShowLotes() => SelectedIndex = 2;
        public void ShowSync() => SelectedIndex = 3;
        public void ShowConta() => SelectedIndex = 4;

        #endregion

        #region Lógica de Inicialização do App

        /// <summary>
        /// Comando principal que executa toda a lógica de inicialização do app.
        /// Chamado pelo OnAppearing da MainPage.
        /// </summary>
        [RelayCommand]
        private async Task InitializeAppAsync()
        {
            await _initializationLock.WaitAsync(); // Lock
            try
            {
                // Se já inicializou e não acabou de logar, sai
                if (_jaInicializou && !Login.AcabouDeLogar)
                {
                    Debug.WriteLine("[MainPageModel] InitializeAppAsync ignorado: já inicializado.");
                    return;
                }

                IsBusy = true;
                Debug.WriteLine("[MainPageModel] InitializeAppAsync iniciado.");

                // Reset flag de login para permitir nova inicialização completa
                if (Login.AcabouDeLogar)
                {
                    Debug.WriteLine("[MainPageModel] Voltou do Login - reinicializando.");
                    Login.AcabouDeLogar = false;
                    _jaInicializou = false; // garante reprocesso
                }

                // 1. Garante que as tabelas do banco existam
                SetLoadingText(Traducao.PorFavorAguarde);
                await SilvaData.Models.ManutencaoTabelas.ChecaSePrecisaAtualizarTabelas();
                Debug.WriteLine("[MainPageModel] Tabelas verificadas/criadas.");

                // 2. Checa Permissões
                Permissoes.ChecaPermissoes();
                Debug.WriteLine("[MainPageModel] Passo 2 concluído.");

                // 3. Checa Usuário Logado
                SetLoadingText(Traducao.ChecandoDados);
                _ISIWebService.CheckLoggedUser();
                Debug.WriteLine("[MainPageModel] Passo 3 concluído. LoggedUser=" + (ISIWebService.Instance.LoggedUser?.nome ?? "null"));

                // 4. Cria Notificações (se não for a primeira execução)
                var primeiraExecucao = Preferences.Get("PrimeiraExecucao", true);
                if (!primeiraExecucao)
                {
                    await Notificacao.CreateNotificationsAsync();
                }
                Debug.WriteLine("[MainPageModel] Passo 4 concluído. PrimeiraExecucao=" + primeiraExecucao);

                // 5. Checa Login e Sessão
                SetLoadingText(Traducao.ChecandoLogin);
                if (ISIWebService.Instance.LoggedUser == null || primeiraExecucao)
                {
                    Debug.WriteLine("[MainPageModel] Usuário não logado ou primeira execução. Abrindo Login.");
                    SetLoadingText(Traducao.IndoParaTelaDeLogin);
                    Preferences.Set("PrimeiraExecucao", false);
                    await _ISIWebService.LogOut();
                    await Task.Delay(100); // ✅ Delay para iOS Shell estabilizar
                    await NavigationUtils.ShowViewAsModalAsync<Login>();
                    // retorna para que OnAppearing ou mensagem dispare nova chamada
                    return;
                }

                // 6. Sessão válida?
                if (Connectivity.NetworkAccess == NetworkAccess.Internet)
                {
                    var temSessaoValida = await _ISIWebService.CheckSessionAindaAtiva();
                    Debug.WriteLine("[MainPageModel] CheckSessionAindaAtiva=" + temSessaoValida);
                    if (temSessaoValida == TipoEstadoSessao.NaoTemSessaoValida)
                    {
                        await PopUpOK.ShowAsync(Traducao.NaoTemSessaoValidaAtiva, Traducao.LogadoComMesmoUsuarioSenha);
#if !DEBUG
                        await ISIWebService.Instance.LogOut();
#endif
                        await Task.Delay(100); // ✅ Delay para iOS Shell estabilizar
                        await NavigationUtils.ShowViewAsModalAsync<Login>();
                        return;
                    }
                }

                // 7. Carrega dados principais
                SetLoadingText(Traducao.Processando);
                await _cacheService.PegaDadosIniciais();
                Debug.WriteLine("[MainPageModel] Cache inicial carregado.");

                // 8. Sincronização obrigatória?
                try
                {
                    if (Preferences.Get("PrecisaSincronizacaoCompleta", false))
                    {
                        Debug.WriteLine("[MainPageModel] Flag PrecisaSincronizacaoCompleta verdadeira.");
                        Preferences.Set("PrecisaSincronizacaoCompleta", false);
                        await Task.Delay(100); // ✅ Delay para iOS Shell estabilizar
                        await NavigationUtils.ShowViewAsModalAsync<SincronizacaoPageModal>();
                        return;
                    }

                    if (Preferences.Get("lastsyncdatetime", DateTime.MinValue) == DateTime.MinValue)
                    {
                        await PopUpOK.ShowAsync(Traducao.SincronizaçãoPendente, Traducao.HáUmaSincronizaçãoObrigatóriaPendenteElaSeráEfetuadaAgora);
                        await Task.Delay(100); // ✅ Delay para iOS Shell estabilizar
                        await NavigationUtils.ShowViewAsModalAsync<SincronizacaoPageModal>();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[MainPageModel] Erro verificação sync: " + ex.Message);
                    await Task.Delay(100); // ✅ Delay para iOS Shell estabilizar
                    await NavigationUtils.ShowViewAsModalAsync<SincronizacaoPageModal>();
                    return;
                }

                // 9. Carrega ViewModels das tabs
                SetLoadingText(Traducao.QuasePronto);
                try
                {
                    // Obtém os ViewModels
                    var dashboardVm = ServiceHelper.GetRequiredService<DashboardViewModel>();
                    var homeVm = ServiceHelper.GetRequiredService<HomeViewModel>();
                    var loteVm = ServiceHelper.GetRequiredService<LoteViewModel>();
                    var galpoesVm = ServiceHelper.GetRequiredService<GalpoesViewModel>();

                    // Inicializa cada um explicitamente (com tratamento de erros)
                    if (dashboardVm != null)
                    {
                        await dashboardVm.InitializeAsync();
                        await dashboardVm.AtualizaDadosGraficos();
                    }
                    if (homeVm != null) await homeVm.InitializeAsync();
                    if (loteVm != null) await loteVm.InitializeAsync();
                    if (galpoesVm != null) await galpoesVm.InitializeAsync();
                    // Pré-aquece Singletons de UI: roda InitializeComponent() durante o startup
                    // (spinner visível) para que a primeira abertura seja instantânea.
                    _ = ServiceHelper.GetRequiredService<ISIInstitute.Views.LoteViews.LoteMonitoramentoView>();
                    _ = ServiceHelper.GetRequiredService<SilvaData.Controls.LoteFormularioView>();
                    await AtualizaTotalSincronizacaoPendente();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MainPage] Erro ao inicializar ViewModels: {ex.Message}");
                    await PopUpOK.ShowAsync(Traducao.Atenção, "Erro ao carregar dados iniciais");
                }

                Debug.WriteLine("[MainPageModel] ViewModels carregados.");

                _jaInicializou = true; // marca sucesso final
                Debug.WriteLine("[MainPageModel] Inicialização concluída.");
            }
            catch (Exception ex)
            {
                await PopUpOK.ShowAsync(Traducao.Erro, $"Falha na inicialização - {ex.Message}");
                _jaInicializou = false; // permite nova tentativa
                Debug.WriteLine("[MainPageModel] Exceção: " + ex);
            }
            finally
            {
                _initializationLock.Release();
                IsBusy = false;
            }
        }

        public async Task AtualizaTotalSincronizacaoPendente()
        {
            if (sincronizacaoPendentesViewModel?.AtualizaListaAlteracoesCommand.CanExecute(null) == true)
            {
                await sincronizacaoPendentesViewModel.AtualizaListaAlteracoesCommand.ExecuteAsync(null);
            }
        }

        #endregion

        /// <summary>
        /// Garante que mudanças em LoadingText sempre ocorram na UI thread
        /// </summary>
        private void SetLoadingText(string value)
        {
            _dispatcher.Dispatch(() => LoadingText = value); // 3. Seguro para lifecycle
        }

        public void Dispose()
        {
            WeakReferenceMessenger.Default.UnregisterAll(this);
        }
    }


}
