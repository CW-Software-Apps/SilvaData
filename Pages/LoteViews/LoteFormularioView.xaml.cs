using CommunityToolkit.Mvvm.Messaging;

using SilvaData.Models;
using SilvaData.Pages.PopUps;
using SilvaData.Utilities;
using SilvaData.Utils;
using SilvaData.ViewModels;

using System.Diagnostics;

namespace SilvaData.Controls
{
    /// <summary>
    /// Pagina otimizada de formulario para preenchimento de dados do lote
    /// - Lazy Loading, Debounce, Virtualizacao, Background Loading
    /// - CORRIGIDO: Campos de avaliacao aparecem corretamente
    /// - CORRIGIDO: Previne inicializacao dupla com flag _isInitialized
    /// </summary>
    public partial class LoteFormularioView : ContentPage
{
    #region Private Fields

    private readonly LoteFormularioViewModel _loteFormViewModel;

    private CancellationTokenSource? _filterCts;
    private const int FILTER_DEBOUNCE_MS = 300;

    private List<View> CamposRequeridos = new();

    // ★★★ NOVO: Previne inicialização dupla ★★★
    private bool _isInitialized = false;
    private bool _isBusyHandlerSubscribed = false;

    #endregion

    #region Public Properties

    public LoteFormularioViewModel LoteFormViewModel => _loteFormViewModel;
    public static bool CarregandoFoto { get; set; }

    #endregion

    #region Constructor

    /// <summary>
    /// Construtor com injeção de dependência via construtor
    /// </summary>
    public LoteFormularioView(LoteFormularioViewModel loteFormViewModel)
    {
        _loteFormViewModel = loteFormViewModel;
        InitializeRealContent();

        Debug.WriteLine($"[LoteFormularioView] ★ Construtor com DI chamado - ViewModel HashCode: {_loteFormViewModel.GetHashCode()}");
    }

    private void InitializeRealContent()
    {
        InitializeComponent();
        _loteFormViewModel.SetValidationHost(this);
        BindingContext = _loteFormViewModel;
        SubscribeIsBusyHandler();
    }

    #endregion

    #region Lifecycle Methods

    /// <summary>
    /// ★★★ CORRIGIDO: Previne inicialização dupla + Detecta retorno de modal ★★★
    /// </summary>
    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        try
        {
            SubscribeIsBusyHandler();

            Debug.WriteLine($"[LoteFormularioView] ═══════════════════════════════════");
            Debug.WriteLine($"[LoteFormularioView] OnNavigatedTo");
            Debug.WriteLine($"  _isInitialized: {_isInitialized}");

            if (NavigationUtils.TemModalAberta(this))
            {
                Debug.WriteLine($"[LoteFormularioView] ⏸️ Retornando de modal");
                return;
            }

            if (_isInitialized)
            {
                Debug.WriteLine($"[LoteFormularioView] ⏸️ JÁ INICIALIZADO - ignorando");
                return;
            }

            Debug.WriteLine($"[LoteFormularioView] ▶️ Navegação real - registrando mensagens");

            // ★★★ OTIMIZAÇÃO: apenas registra mensagens; o carregamento ocorre no OnAppearing ★★★
            RegisterMessages();
            _isInitialized = true;

            // ★★★ NÃO CARREGA DADOS AQUI: evita competição com animação/modal no iOS ★★★
            Debug.WriteLine($"[LoteFormularioView] ✅ Mensagens registradas (carregamento será disparado no OnAppearing)");
            Debug.WriteLine($"[LoteFormularioView] ═══════════════════════════════════");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LoteFormularioView] ✖ Erro OnNavigatedTo: {ex.Message}");
        }
    }


    private bool _heavyTemplatesInjected = false;

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (!_heavyTemplatesInjected)
            _ = InjectHeavyTemplatesAsync();
        else
            _ = CarregarAposAppearingAsync();
    }

    private async Task InjectHeavyTemplatesAsync()
    {
        if (_heavyTemplatesInjected) return;
        _heavyTemplatesInjected = true;

        // iOS: OnAppearing dispara DURANTE a animação do PushModalAsync (~400ms).
        // Aguarda a animação terminar antes de instanciar templates Syncfusion,
        // que criam views nativas via GCD e causam deadlock se chamados durante animação.
        if (DeviceInfo.Platform == DevicePlatform.iOS)
            await Task.Delay(700);

        camposaPreencher.HeaderTemplate = (DataTemplate)Resources["CommonHeaderTemplate"];
        camposaPreencher.FooterTemplate = (DataTemplate)Resources["CommonFooterTemplate"];
        camposaPreencher.AutoFitMode = Syncfusion.Maui.ListView.AutoFitMode.DynamicHeight;
        camposaPreencher.CachingStrategy = Syncfusion.Maui.ListView.CachingStrategy.RecycleTemplate;

        Debug.WriteLine("[LoteFormularioView] Templates pesados injetados após animação iOS (700ms delay)");

        Debug.WriteLine("[LoteFormularioView] ▶ Disparando Carregar() pós-injeção de templates");
        await _loteFormViewModel.Carregar();
    }

    // Chamado no OnAppearing a partir da segunda abertura (templates já injetados, singleton reutilizado).
    private async Task CarregarAposAppearingAsync()
    {
        if (DeviceInfo.Platform == DevicePlatform.iOS)
            await Task.Delay(700);

        Debug.WriteLine("[LoteFormularioView] ▶ Disparando Carregar() pós-OnAppearing (singleton reutilizado)");
        await _loteFormViewModel.Carregar();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        UnsubscribeIsBusyHandler();

        Debug.WriteLine($"[LoteFormularioView] OnDisappearing");

        // ★★★ ENHANCED: Limpeza segura de botões e recursos ★★★
        try
        {
            // Só limpa se não está abrindo modal
            if (!NavigationUtils.TemModalAberta(this))
            {
                Debug.WriteLine($"[LoteFormularioView] 🧹 Limpando recursos");

                // ★★★ SAFE BUTTON CLEANUP: Remove event handlers e limpa referências ★★★
                SafeCleanupButtons();

                if (ReferenceEquals(ISIUtils.ValidationTargetPage, this))
                {
                    ISIUtils.IsValidationActiveGlobal = false;
                    ISIUtils.ValidationTargetPage = null;
                }

                _isInitialized = false; // ★ Reset flag
                _loteFormViewModel.Cleanup();
                UnregisterMessages();

                WeakReferenceMessenger.Default.Send(new CloseFormularioMessage(mostraLoading: true));
            }
            else
            {
                Debug.WriteLine($"[LoteFormularioView] ⏸️ Modal aberta - mantém estado");
                // ★★★ EVEN WITH MODAL: Still ensure button safety during modal operations ★★★
                SafeButtonStateCheck();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LoteFormularioView] ❌ Erro em OnDisappearing: {ex.Message}");
        }

        _filterCts?.Cancel();
        _filterCts?.Dispose();
        _filterCts = null;
    }

    private void SubscribeIsBusyHandler()
    {
        if (_isBusyHandlerSubscribed) return;
        _loteFormViewModel.IsBusyChanged += OnIsBusyChangedRefreshButtons;
        _isBusyHandlerSubscribed = true;
    }

    private void UnsubscribeIsBusyHandler()
    {
        if (!_isBusyHandlerSubscribed) return;
        _loteFormViewModel.IsBusyChanged -= OnIsBusyChangedRefreshButtons;
        _isBusyHandlerSubscribed = false;
    }

    /// <summary>
    /// ★★★ NEW: Safe cleanup of button references and event handlers ★★★
    /// </summary>
    private void SafeCleanupButtons()
    {
        // LoteFormularioView e LoteFormularioViewModel são Singletons:
        // NÃO limpar Command nem desabilitar botões aqui — o mesmo objeto View é
        // reutilizado na próxima abertura e o binding XAML não é restaurado automaticamente.
        Debug.WriteLine($"[LoteFormularioView] 🧹 SafeCleanupButtons — singleton, nenhuma ação necessária");
    }

    private void SafeCleanupOtherButtons()
    {
        // Não usado (mantido por compatibilidade de chamada)
    }

    /// <summary>
    /// ★★★ NEW: Safe button state check during modal operations ★★★
    /// </summary>
    private void SafeButtonStateCheck()
    {
        try
        {
            if (btSalvar != null && btSalvar.Handler != null)
            {
                // Ensure button is in a safe state during modal operations
                var currentState = btSalvar.IsEnabled;
                Debug.WriteLine($"[LoteFormularioView] 🔍 Estado do botão durante modal: Enabled={currentState}");
                
                // If button is in an invalid state, try to fix it
                if (btSalvar.Handler == null)
                {
                    Debug.WriteLine($"[LoteFormularioView] ⚠️ Botão sem handler durante modal - desabilitando para segurança");
                    btSalvar.IsEnabled = false;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LoteFormularioView] ❌ Erro em SafeButtonStateCheck: {ex.Message}");
        }
    }

    #endregion

    #region Message Registration

    private void RegisterMessages()
    {
        Debug.WriteLine($"[LoteFormularioView] 📨 RegisterMessages");

        UnregisterMessages(); // Previne duplicatas

        WeakReferenceMessenger.Default.Register<UpdateScoreMessage>(this, (r, m) =>
        {
            try { _loteFormViewModel.UpdateTotal(); }
            catch (Exception ex) { Debug.WriteLine($"[UpdateScoreMessage] Erro: {ex.Message}"); }
        });

        WeakReferenceMessenger.Default.Register<VaiProProximoMessage>(this, async (r, m) =>
        {
            try { await Helpers.VaiParaProximo(m.View, camposaPreencher); }
            catch (Exception ex) { Debug.WriteLine($"[VaiProProximoMessage] Erro: {ex.Message}"); }
        });

        WeakReferenceMessenger.Default.Register<ISIMacroFotoRequestedMessage>(this, async (r, m) =>
        {
            try { await OnISIMacroFotoRequestedAsync(m.Nome, m.Parametro); }
            catch (Exception ex) { Debug.WriteLine($"[ISIMacroFotoRequestedMessage] Erro: {ex.Message}"); }
        });

        WeakReferenceMessenger.Default.Register<RefreshLoteFormularioMessage>(this, async (r, m) =>
        {
            try
            {
                Debug.WriteLine($"[LoteFormularioView] 🔄 Refresh: LoteForm={m.LoteFormId}, Tipo={m.ParametroTipoId}");
                _loteFormViewModel.IsBusy = true;

                if (m.DeveLimpar)
                    _loteFormViewModel.Cleanup();

                await CarregaDadosAsync(limpaFormularioAtual: m.DeveLimpar);
                _loteFormViewModel.IsBusy = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[RefreshLoteFormularioMessage] Erro: {ex.Message}");
                _loteFormViewModel.IsBusy = false;
            }
        });

        WeakReferenceMessenger.Default.Register<SetFormularioEstadoMessage>(this, async (r, m) =>
        {
            try
            {
                Debug.WriteLine($"[LoteFormularioView] ═══════════════════════════════════");
                Debug.WriteLine($"[LoteFormularioView] ⚙️ SetFormularioEstadoMessage recebida");
                Debug.WriteLine($"  Lote: {m.Lote.DescricaoCompleta}");
                Debug.WriteLine($"  LoteFormId: {m.LoteFormId}");
                Debug.WriteLine($"  ParametroTipoId: {m.ParametroTipoId}");
                Debug.WriteLine($"  DeveLimpar: {m.DeveLimpar}");

                // ★★★ MARCA COMO INICIALIZADO (previne OnNavigatedTo de carregar novamente) ★★★
                _isInitialized = true;


                // LIMPA E MOSTRA LOADING IMEDIATAMENTE (sempre no MainThread)
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    _loteFormViewModel.IsBusy = true;
                    if (m.DeveLimpar)
                        _loteFormViewModel.Cleanup();

                    // Cria novo formulário vazio para limpar ItemsSource imediatamente
                    _loteFormViewModel.LoteFormulario = new LoteFormulario();
                    _loteFormViewModel.ParametroSelecionado = null;
                    _loteFormViewModel.AlternativasParametroSelecionado?.Clear();

                    // Força reset do tipo para forçar troca de DataTemplate
                    _loteFormViewModel.ParametroTipo = -1;

                    // Força refresh imediato dos bindings
                    _loteFormViewModel.ForceRefreshAll();
                });

                await Task.Delay(100); // Garante refresh visual e criação de controles

                _loteFormViewModel.SetInitialState(
                    lote: m.Lote,
                    loteFormId: m.LoteFormId,
                    parametroTipoId: m.ParametroTipoId,
                    fase: m.Fase,
                    isReadOnly: m.IsReadOnly,
                    podeEditar: m.PodeEditar);

                if (m.ParametroSelecionado != null)
                {
                    _loteFormViewModel.ParametroSelecionado = m.ParametroSelecionado;
                    Debug.WriteLine($"[LoteFormularioView]   ParametroSelecionado definido: {m.ParametroSelecionado.nome}");
                }

                Debug.WriteLine($"[LoteFormularioView] ★ Iniciando CarregaDadosAsync VIA MENSAGEM");
                await CarregaDadosAsync(limpaFormularioAtual: m.DeveLimpar);

                _loteFormViewModel.IsBusy = false;

                Debug.WriteLine($"[LoteFormularioView] ✅ SetFormularioEstadoMessage processada");
                Debug.WriteLine($"[LoteFormularioView] ═══════════════════════════════════");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SetFormularioEstadoMessage] Erro: {ex.Message}");
                _loteFormViewModel.IsBusy = false;
            }
        });

        WeakReferenceMessenger.Default.Register<SetModeloISIMacroMessage>(this, (r, m) =>
        {
            try
            {
                if (m.ModeloId.HasValue)
                {
                    Debug.WriteLine($"[LoteFormularioView] 📋 Modelo ISI Macro: {m.ModeloId}");
                    _loteFormViewModel.ModeloIsiMacroSelecionado = m.ModeloId;
                }
            }
            catch (Exception ex) { Debug.WriteLine($"[SetModeloISIMacroMessage] Erro: {ex.Message}"); }
        });

        WeakReferenceMessenger.Default.Register<CloseFormularioMessage>(this, (r, m) =>
        {
            try
            {
                if (m.MostraLoading)
                    Debug.WriteLine($"[LoteFormularioView] 🔄 Formulário fechou - Mostrando loading");
            }
            catch (Exception ex) { Debug.WriteLine($"[CloseFormularioMessage] Erro: {ex.Message}"); }
        });

        Debug.WriteLine($"[LoteFormularioView] ✅ Mensagens registradas");
    }

    private void UnregisterMessages()
    {
        try
        {
            Debug.WriteLine($"[LoteFormularioView] 🗑️ UnregisterMessages");

            WeakReferenceMessenger.Default.Unregister<UpdateScoreMessage>(this);
            WeakReferenceMessenger.Default.Unregister<VaiProProximoMessage>(this);
            WeakReferenceMessenger.Default.Unregister<ISIMacroFotoRequestedMessage>(this);
            WeakReferenceMessenger.Default.Unregister<RefreshLoteFormularioMessage>(this);
            WeakReferenceMessenger.Default.Unregister<CloseFormularioMessage>(this);
            WeakReferenceMessenger.Default.Unregister<SetFormularioEstadoMessage>(this);
            WeakReferenceMessenger.Default.Unregister<SetModeloISIMacroMessage>(this);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LoteFormularioView] Erro UnregisterMessages: {ex.Message}");
        }
    }

    private async Task OnISIMacroFotoRequestedAsync(string nome, ParametroComAlternativas parametro)
    {
        if (parametro == null)
        {
            Debug.WriteLine($"[LoteFormularioView] ⚠️ Parâmetro nulo - ignorando");
            return;
        }

        try
        {
            Debug.WriteLine($"[LoteFormularioView] 📸 Abrindo fotos: {nome}");
            await NavigationUtils.ShowViewAsModalAsync<ISIMacroNotaSelecionaImagem>(nome, parametro);
            Debug.WriteLine($"[LoteFormularioView] ✅ Modal de fotos fechada");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LoteFormularioView] ❌ Erro ao abrir fotos: {ex.Message}");
            await PopUpOK.ShowAsync(Traducao.Erro, $"Erro ao abrir fotos: {ex.Message}");
        }
    }

    #endregion

    #region Data Loading

    /// <summary>
    /// ★★★ Carregamento otimizado com tasks paralelas ★★★
    /// </summary>
    public async Task CarregaDadosAsync(bool limpaFormularioAtual = true, int? modeloIsiMacroSelecionado = null)
    {
        try
        {
            Debug.WriteLine($"[LoteFormularioView.CarregaDadosAsync] ═══════════════════════════════════");
            Debug.WriteLine($"[LoteFormularioView.CarregaDadosAsync] ★ CHAMADO");
            Debug.WriteLine($"  limpaFormularioAtual: {limpaFormularioAtual}");
            Debug.WriteLine($"  modeloIsiMacroSelecionado: {modeloIsiMacroSelecionado}");

            _loteFormViewModel.IsBusy = true;

            // 1. Inicializa formulário primeiro para garantir ParametroSelecionado/alternativas.
            await InicializaFormulario(limpaFormularioAtual, modeloIsiMacroSelecionado);

            // 2. Após init, carrega dependências de UI.
            await CarregaImagensAsync();

            Debug.WriteLine($"[LoteFormularioView.CarregaDadosAsync] ✅ Dados carregados com sucesso");
            Debug.WriteLine($"[LoteFormularioView.CarregaDadosAsync] ═══════════════════════════════════");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LoteFormularioView.CarregaDadosAsync] ❌ Erro: {ex.Message}");
            await PopUpOK.ShowAsync(Traducao.Erro, $"Erro ao carregar: {ex.Message}");
        }
        finally
        {
            _loteFormViewModel.IsBusy = false;
            CarregandoFoto = false;
        }
    }

    private async Task CarregaImagensAsync()
    {
        try
        {
            if (_loteFormViewModel?.LoteFormulario?.LoteForm?.id > 0)
            {
                var imagens = await LoteFormImagem.PegaImagens(
                    (int)_loteFormViewModel.LoteFormulario.LoteForm.id);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    _loteFormViewModel.LoteFormImagem1 = imagens.Count >= 1 ? imagens[0] : null;
                    _loteFormViewModel.LoteFormImagem2 = imagens.Count >= 2 ? imagens[1] : null;
                    _loteFormViewModel.LoteFormImagem3 = imagens.Count >= 3 ? imagens[2] : null;
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LoteFormularioView] Erro ao carregar imagens: {ex.Message}");
        }
    }

    public async Task InicializaFormulario(bool limpaFormularioAtual = true, int? modeloIsiMacroSelecionado = null)
    {
        Debug.WriteLine($"[LoteFormularioView.InicializaFormulario] ═══════════════════════════════════");
        Debug.WriteLine($"[LoteFormularioView.InicializaFormulario] ★ CHAMADO");
        Debug.WriteLine($"  limpaFormularioAtual: {limpaFormularioAtual}");

        await _loteFormViewModel.InicializaFormulario(limpaFormularioAtual, modeloIsiMacroSelecionado);

        Debug.WriteLine($"[LoteFormularioView.InicializaFormulario] ═══════════════════════════════════");
    }

    #endregion

    #region Validation

    public async Task<bool> CheckCamposObrigatorios()
    {
        try
        {
            if (_loteFormViewModel.LoteFormulario?.Formulario_ParametrosComAlternativas == null)
                return false;

            ISIUtils.ValidationTargetPage = this;
            WeakReferenceMessenger.Default.Send(new HighlightRequiredFieldsMessage(this));

            foreach (var parametroComAlternativas in _loteFormViewModel.LoteFormulario.Formulario_ParametrosComAlternativas)
            {
                if (parametroComAlternativas.required == 1 && !parametroComAlternativas.EstaRespondida)
                {
                    var camposObrigatoriosNaoPreenchidos = _loteFormViewModel.LoteFormulario.Formulario_ParametrosComAlternativas
                        .Where(p => p.required == 1 && !p.EstaRespondida)
                        .Select(p => p.nome)
                        .ToList();

                    await Helpers.ShowRequiredFieldsPopupAsync(camposObrigatoriosNaoPreenchidos);
                    return false;
                }
            }

            return await Helpers.CheckCamposObrigatorios(null!, CamposRequeridos, null!, camposaPreencher);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LoteFormularioView] Erro CheckCamposObrigatorios: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region UI Interactions

    /// <summary>
    /// iOS fix: SfButton visual state pode ficar preso em "Disabled" após CanExecute voltar a true.
    /// Força o estado visual correto quando IsBusy muda.
    /// Enhanced with null checks to prevent NullReferenceException during layout operations.
    /// </summary>
    private void OnIsBusyChangedRefreshButtons(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                // ★★★ DEFENSIVE: Verifica se a view ainda está válida ★★★
                if (this == null || this.Handler == null)
                {
                    Debug.WriteLine("[LoteFormularioView] ⚠️ View inválida - ignorando refresh de botões");
                    return;
                }

                // ★★★ DEFENSIVE: Verifica se o botão existe e não é nulo ★★★
                if (btSalvar == null)
                {
                    Debug.WriteLine("[LoteFormularioView] ⚠️ btSalvar é nulo - ignorando refresh");
                    return;
                }

                // ★★★ DEFENSIVE: Verifica se o botão tem um handler válido ★★★
                if (btSalvar.Handler == null)
                {
                    Debug.WriteLine("[LoteFormularioView] ⚠️ btSalvar.Handler é nulo - botão não está pronto para layout");
                    return;
                }

                // ★★★ DEFENSIVE: Só atualiza se não estiver ocupado ★★★
                if (!_loteFormViewModel.IsBusy)
                {
                    Debug.WriteLine("[LoteFormularioView] 🔄 Atualizando estado visual do botão Salvar");
                    
                    // ★★★ DEFENSIVE: Verifica estado do botão antes de aplicar ★★★
                    try
                    {
                        var currentState = VisualStateManager.GetVisualStateGroups(btSalvar);
                        Debug.WriteLine($"[LoteFormularioView] Estado atual do botão: {currentState?.FirstOrDefault()?.Name ?? "Unknown"}");
                        
                        VisualStateManager.GoToState(btSalvar, "Normal");
                        Debug.WriteLine("[LoteFormularioView] ✅ Estado visual do botão atualizado para Normal");
                    }
                    catch (Exception visualStateEx)
                    {
                        Debug.WriteLine($"[LoteFormularioView] ⚠️ Erro ao atualizar VisualStateManager: {visualStateEx.Message}");
                        
                        // ★★★ FALLBACK: Tenta atualizar propriedades diretamente se VisualStateManager falhar ★★★
                        try
                        {
                            btSalvar.IsEnabled = true;
                            btSalvar.Opacity = 1.0;
                            Debug.WriteLine("[LoteFormularioView] ✅ Fallback: Propriedades do botão atualizadas diretamente");
                        }
                        catch (Exception fallbackEx)
                        {
                            Debug.WriteLine($"[LoteFormularioView] ❌ Erro no fallback do botão: {fallbackEx.Message}");
                        }
                    }
                }
                else
                {
                    Debug.WriteLine("[LoteFormularioView] ⏸️ IsBusy = true - mantém botão em estado atual");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoteFormularioView] ❌ Erro crítico em OnIsBusyChangedRefreshButtons: {ex.Message}");
                Debug.WriteLine($"[LoteFormularioView] StackTrace: {ex.StackTrace}");
                
                // ★★★ EMERGENCY: Tenta garantir estado mínimo do botão ★★★
                try
                {
                    if (btSalvar != null && btSalvar.Handler != null)
                    {
                        btSalvar.IsEnabled = !_loteFormViewModel.IsBusy;
                        Debug.WriteLine("[LoteFormularioView] 🚨 Emergency: Estado mínimo do botão garantido");
                    }
                }
                catch (Exception emergencyEx)
                {
                    Debug.WriteLine($"[LoteFormularioView] 🚨 Emergency falhou: {emergencyEx.Message}");
                }
            }
        });
    }

    private void date_Picker_DateChanged(object sender, DateChangedEventArgs e)
    {
        try
        {
            // ★★★ DEFENSIVE: Verifica se o ViewModel ainda é válido ★★★
            if (_loteFormViewModel == null)
            {
                Debug.WriteLine("[LoteFormularioView] ⚠️ ViewModel é nulo - ignorando DateChanged");
                return;
            }

            // ★★★ DEFENSIVE: Verifica se a nova data é válida ★★★
            if (e.NewDate == null)
            {
                Debug.WriteLine("[LoteFormularioView] ⚠️ Nova data é nula - ignorando DateChanged");
                return;
            }

            Debug.WriteLine($"[LoteFormularioView] 📅 Data alterada: {e.NewDate:dd/MM/yyyy}");
            _loteFormViewModel.UpdateIdadeLote();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LoteFormularioView] ❌ Erro em date_Picker_DateChanged: {ex.Message}");
        }
    }

    public async Task FocaIsiMacro(string nome, string categoria)
    {
        try
        {
            // ★★★ DEFENSIVE: Verifica parâmetros ★★★
            if (string.IsNullOrEmpty(nome))
            {
                Debug.WriteLine("[LoteFormularioView] ⚠️ Nome é nulo ou vazio - ignorando FocaIsiMacro");
                return;
            }

            // ★★★ DEFENSIVE: Verifica se a view ainda está válida ★★★
            if (this == null || this.Handler == null)
            {
                Debug.WriteLine("[LoteFormularioView] ⚠️ View inválida - ignorando FocaIsiMacro");
                return;
            }

            // ★★★ DEFENSIVE: Verifica se o ListView é válido ★★★
            if (camposaPreencher == null)
            {
                Debug.WriteLine("[LoteFormularioView] ⚠️ camposaPreencher é nulo - ignorando FocaIsiMacro");
                return;
            }

            Debug.WriteLine($"[LoteFormularioView] 🎯 Focando ISI Macro: {nome} / {categoria}");
            await Helpers.ScrollToISIMacroNotaPorNome(camposaPreencher, nome, categoria);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LoteFormularioView] ❌ Erro em FocaIsiMacro: {ex.Message}");
        }
    }

    protected override bool OnBackButtonPressed()
    {
        try
        {
            // ★★★ DEFENSIVE: Verifica se o ViewModel ainda é válido ★★★
            if (_loteFormViewModel == null)
            {
                Debug.WriteLine("[LoteFormularioView] ⚠️ ViewModel é nulo - usando comportamento padrão do back button");
                return base.OnBackButtonPressed();
            }

            Debug.WriteLine("[LoteFormularioView] 🔙 Back button pressionado");

            if (_loteFormViewModel.PesquisaISIMacroDialogVisible)
            {
                Debug.WriteLine("[LoteFormularioView] 📝 Fechando pesquisa ISI Macro");
                _loteFormViewModel.PesquisaISIMacroDialogVisible = false;
                return true;
            }

            // PesquisaAvaliacoesDialogVisible removido - substituído por VerRegistrosPopup

            Debug.WriteLine("[LoteFormularioView] ❓ Mostrando confirmação de pop modal");
            Helpers.MostraConfirmacaodePopModal();
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LoteFormularioView] ❌ Erro em OnBackButtonPressed: {ex.Message}");
            return base.OnBackButtonPressed();
        }
    }

    #endregion

    #region Search/Filter - Debounced

    private void ProcuraISIMacroTextChanged(object sender, TextChangedEventArgs e)
    {
        _ = ProcuraISIMacroTextChangedAsync();
    }

    private async Task ProcuraISIMacroTextChangedAsync()
    {
        try
        {
            // ★★★ DEFENSIVE: Cancela operação anterior ★★★
            _filterCts?.Cancel();
            _filterCts = new CancellationTokenSource();

            // ★★★ DEFENSIVE: Verifica se a view ainda está válida ★★★
            if (this == null || this.Handler == null)
            {
                Debug.WriteLine("[LoteFormularioView] ⚠️ View inválida - ignorando mudança de texto");
                return;
            }

            // ★★★ DEFENSIVE: Verifica se o ListView é válido ★★★
            if (listViewFiltroISIMacro == null)
            {
                Debug.WriteLine("[LoteFormularioView] ⚠️ listViewFiltroISIMacro é nulo - ignorando mudança de texto");
                return;
            }

            await Task.Delay(FILTER_DEBOUNCE_MS, _filterCts.Token);

            await Task.Run(() =>
            {
                try
                {
                    // ★★★ SAFE: Verifica se o DataSource é válido ★★★
                    if (listViewFiltroISIMacro?.DataSource != null)
                    {
                        listViewFiltroISIMacro.DataSource.Filter = FilterISIMacroParametro;

                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            try
                            {
                                // ★★★ SAFE: Verifica se o ListView ainda está válido ★★★
                                if (listViewFiltroISIMacro != null && listViewFiltroISIMacro.DataSource != null)
                                {
#pragma warning disable IL2026
                                    listViewFiltroISIMacro.DataSource.RefreshFilter();
                                    listViewFiltroISIMacro.RefreshView();
#pragma warning restore IL2026
                                    Debug.WriteLine("[LoteFormularioView] ✅ Filtro ISI Macro atualizado");
                                }
                                else
                                {
                                    Debug.WriteLine("[LoteFormularioView] ⚠️ ListView ou DataSource se tornou nulo durante refresh");
                                }
                            }
                            catch (Exception refreshEx)
                            {
                                Debug.WriteLine($"[LoteFormularioView] ❌ Erro ao fazer refresh do filtro: {refreshEx.Message}");
                            }
                        });
                    }
                    else
                    {
                        Debug.WriteLine("[LoteFormularioView] ⚠️ DataSource é nulo - não foi possível aplicar filtro");
                    }
                }
                catch (Exception taskEx)
                {
                    Debug.WriteLine($"[LoteFormularioView] ❌ Erro na task de filtro: {taskEx.Message}");
                }
            }, _filterCts.Token);
        }
        catch (TaskCanceledException) 
        {
            Debug.WriteLine("[LoteFormularioView] ℹ️ Task de filtro cancelada - comportamento esperado");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LoteFormularioView] ❌ Erro em ProcuraISIMacroTextChangedAsync: {ex.Message}");
        }
    }

    private bool FilterISIMacroParametro(object obj)
    {
        try
        {
            // ★★★ DEFENSIVE: Verifica se o texto de filtro é válido ★★★
            if (filtroISIMacroText == null || string.IsNullOrEmpty(filtroISIMacroText.Text))
            {
                return true; // Se não há filtro, mostra tudo
            }

            // ★★★ DEFENSIVE: Verifica se o objeto é válido ★★★
            if (obj == null)
            {
                Debug.WriteLine("[LoteFormularioView] ⚠️ Objeto de filtro é nulo");
                return false;
            }

            if (obj is ParametroComAlternativas parametro)
            {
                // ★★★ SAFE: Verifica se as propriedades do parâmetro são válidas ★★★
                if (string.IsNullOrEmpty(parametro.nome) && string.IsNullOrEmpty(parametro.Categoria))
                {
                    return false; // Não há nada para filtrar
                }

                var searchText = ISIUtils.RemoveDiacritics(filtroISIMacroText.Text).ToLowerInvariant();
                var nomeWithoutDiacritics = ISIUtils.RemoveDiacritics(parametro.nome ?? string.Empty).ToLowerInvariant();
                var categoriaWithoutDiacritics = ISIUtils.RemoveDiacritics(parametro.Categoria ?? string.Empty).ToLowerInvariant();

                var matches = nomeWithoutDiacritics.Contains(searchText) || categoriaWithoutDiacritics.Contains(searchText);
                Debug.WriteLine($"[LoteFormularioView] 🔍 Filtro: '{searchText}' vs '{parametro.nome}' - Match: {matches}");
                
                return matches;
            }

            Debug.WriteLine($"[LoteFormularioView] ⚠️ Objeto não é ParametroComAlternativas: {obj.GetType().Name}");
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LoteFormularioView] ❌ Erro em FilterISIMacroParametro: {ex.Message}");
            return false; // Em caso de erro, não mostra o item
        }
    }

    #endregion

    #region ListView Item Selection

    private void ListViewFiltroISIMacro_OnItemTapped(object sender, Syncfusion.Maui.ListView.ItemTappedEventArgs e)
    {
        _ = ListViewFiltroISIMacro_OnItemTappedAsync(e);
    }

    private async Task ListViewFiltroISIMacro_OnItemTappedAsync(Syncfusion.Maui.ListView.ItemTappedEventArgs e)
    {
        try
        {
            // ★★★ DEFENSIVE: Verifica se o evento é válido ★★★
            if (e == null)
            {
                Debug.WriteLine("[LoteFormularioView] ⚠️ ItemTappedEventArgs é nulo - ignorando");
                return;
            }

            // ★★★ DEFENSIVE: Verifica se o DataItem é válido ★★★
            if (e.DataItem == null)
            {
                Debug.WriteLine("[LoteFormularioView] ⚠️ DataItem é nulo - ignorando");
                return;
            }

            // ★★★ DEFENSIVE: Verifica se o ViewModel ainda é válido ★★★
            if (_loteFormViewModel == null)
            {
                Debug.WriteLine("[LoteFormularioView] ⚠️ ViewModel é nulo - ignorando item tap");
                return;
            }

            if (e.DataItem is not ParametroComAlternativas pa)
            {
                Debug.WriteLine($"[LoteFormularioView] ⚠️ DataItem não é ParametroComAlternativas: {e.DataItem?.GetType().Name}");
                return;
            }

            Debug.WriteLine($"[LoteFormularioView] 📝 ISI Macro selecionado: {pa.nome}");

            // ★★★ SAFE: Fecha diálogo antes de focar ★★★
            _loteFormViewModel.PesquisaISIMacroDialogVisible = false;
            
            // ★★★ SAFE: Verifica se o foco pode ser aplicado ★★★
            if (!string.IsNullOrEmpty(pa.nome))
            {
                await FocaIsiMacro(pa.nome, pa.Categoria);
            }
            else
            {
                Debug.WriteLine("[LoteFormularioView] ⚠️ Nome do parâmetro é vazio - não é possível focar");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LoteFormularioView] ❌ Erro em ListViewFiltroISIMacro_OnItemTappedAsync: {ex.Message}");
            
            // ★★★ RECOVERY: Tenta fechar o diálogo em caso de erro ★★★
            try
            {
                if (_loteFormViewModel != null)
                {
                    _loteFormViewModel.PesquisaISIMacroDialogVisible = false;
                }
            }
            catch (Exception recoveryEx)
            {
                Debug.WriteLine($"[LoteFormularioView] 🚨 Recovery falhou: {recoveryEx.Message}");
            }
        }
    }

    #endregion
}
}
