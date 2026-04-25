using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;


using SilvaData.Models;
using SilvaData.Pages.PopUps;
using SilvaData.Utils;
using SilvaData.Utilities;

using HapticHelper = SilvaData.Utilities.HapticHelper;

using System.Collections.ObjectModel;
using System.Diagnostics;

using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace SilvaData.ViewModels;

/// <summary>
/// ★★★ ViewModel otimizado para formulário de lote (MAUI) ★★★
/// ✅ Cache, Paralelização, Debounce
/// ✅ CORRIGIDO: Garante que campos de avaliação apareçam
/// </summary>
public partial class LoteFormularioViewModel : ViewModelBase
{
    /// <summary>
    /// Força atualização de todos os bindings (refresh total)
    /// </summary>
    public void ForceRefreshAll()
    {
        // Notifica manualmente todas as propriedades relevantes para forçar refresh dos bindings
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(SubTitle));
        OnPropertyChanged(nameof(LoteId));
        OnPropertyChanged(nameof(ParametroTipo));
        OnPropertyChanged(nameof(LoteFormId));
        OnPropertyChanged(nameof(Fase));
        OnPropertyChanged(nameof(ParametroSelecionado));
        OnPropertyChanged(nameof(AlternativasParametroSelecionado));
        OnPropertyChanged(nameof(Item));
        OnPropertyChanged(nameof(Lote));
        OnPropertyChanged(nameof(LoteFormVinculado));
        OnPropertyChanged(nameof(LoteFormulario));
        OnPropertyChanged(nameof(LoteFormImagem1));
        OnPropertyChanged(nameof(LoteFormImagem2));
        OnPropertyChanged(nameof(LoteFormImagem3));
        OnPropertyChanged(nameof(PesquisaISIMacroDialogVisible));
        OnPropertyChanged(nameof(ModeloIsiMacroSelecionado));
        OnPropertyChanged(nameof(MostrarBotaoCarregar));
        OnPropertyChanged(nameof(ScoreTotalDaAve));
        OnPropertyChanged(nameof(PodeEditar));
        OnPropertyChanged(nameof(IsReadOnly));
        OnPropertyChanged(nameof(RealizadoColor));
        OnPropertyChanged(nameof(IsISIMacro));
        OnPropertyChanged(nameof(IdadeLote));
        OnPropertyChanged(nameof(ScoreTotalDaAveCor));
    }
    #region Observable Properties

    [ObservableProperty] private string title = string.Empty;
    [ObservableProperty] private string subTitle = string.Empty;
    [ObservableProperty] private int loteId = -1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsISIMacro))]
    private int parametroTipo = -1;

    [ObservableProperty] private int loteFormId = -1;
    [ObservableProperty] private int? fase;

    [ObservableProperty]
    private Parametro? parametroSelecionado;

    [ObservableProperty] private ObservableCollection<ParametroAlternativas> alternativasParametroSelecionado = new();
    [ObservableProperty] private int? item;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IdadeLote))]
    private Lote? lote;

    [ObservableProperty] private int loteFormVinculado = -1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IdadeLote))]
    private LoteFormulario? loteFormulario;

    [ObservableProperty] private LoteFormImagem? loteFormImagem1;
    [ObservableProperty] private LoteFormImagem? loteFormImagem2;
    [ObservableProperty] private LoteFormImagem? loteFormImagem3;

    [ObservableProperty] private bool pesquisaISIMacroDialogVisible;
    [ObservableProperty] private int? modeloIsiMacroSelecionado;
    [ObservableProperty] private bool mostrarBotaoCarregar;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ScoreTotalDaAveCor))]
    private int scoreTotalDaAve;

    [ObservableProperty] private bool podeEditar;
    [ObservableProperty] private new bool isReadOnly = true;
    [ObservableProperty] private Color realizadoColor = Colors.Blue;

    #endregion

    #region Public Properties

    public List<LoteFormParametro> LoteFormParametros { get; set; } = new();
    public ObservableCollection<LoteFormImagem> LoteFormImagensList { get; set; } = new();
    public bool NovoFormulario { get; set; }

    #endregion

    #region Private Fields - Cache e Debounce

    private static readonly Dictionary<string, List<ParametroComAlternativas>> _parametrosCache = new();
    private CancellationTokenSource? _updateTotalCts;
    private CancellationTokenSource? _autoSaveCts;
    private const int UPDATE_DEBOUNCE_MS = 150;
    private const int AUTOSAVE_DEBOUNCE_MS = 500;

    private bool _autoSaveEnabled = false;
    private WeakReference<Page>? _validationHostPage;
    private LoteFormulario? _subscribedLoteFormulario;
    private LoteForm? _subscribedLoteForm;
    private ObservableCollection<ParametroComAlternativas>? _subscribedParametrosCollection;

    #endregion

    public void SetValidationHost(Page page)
    {
        _validationHostPage = new WeakReference<Page>(page);
    }

    private Page? GetValidationHostPage()
    {
        if (_validationHostPage != null && _validationHostPage.TryGetTarget(out var page))
        {
            return page;
        }

        return null;
    }

    #region Computed Properties

    public int FaseComoDias => Fase switch
    {
        1 => 7,
        2 => 14,
        3 => 21,
        4 => 28,
        5 => 35,
        6 => 42,
        7 => 0,  //Fase Personalizada
        _ => 0
    };

    public string IdadeLote
    {
        get
        {
            var loteFormData = LoteFormulario?.LoteForm?.data;
            var loteDataInicio = Lote?.dataInicio;

            if (loteFormData != null && loteDataInicio != null)
            {
                var totalDays = Math.Round((loteFormData.Value - loteDataInicio.Value).TotalDays);
                return $"{totalDays} {Traducao.Dias}";
            }
            return string.Empty;
        }
    }

    public Color ScoreTotalDaAveCor
    {
        get
        {
            return ScoreTotalDaAve <= 21
                ? Color.FromArgb("#48ba00")
                : ScoreTotalDaAve <= 40
                    ? Color.FromArgb("#ffba00")
                    : Color.FromArgb("#fc4c17");
        }
    }

    [ObservableProperty]
    private AvaliacaoType avaliacaoTypeSelecionada = AvaliacaoType.ISIMacro;

    public void SetAvaliacaoType(AvaliacaoType tipo)
    {
        AvaliacaoTypeSelecionada = tipo;
        Debug.WriteLine($"[LoteFormularioViewModel] Tipo: {tipo}");
    }

    public string LoteDescricaoCompleta => Lote?.DescricaoCompleta ?? string.Empty;

    public bool IsISIMacro => ParametroTipo == 15;
    public bool PrecisaMostrarEscoreAve => !IsBusy && ParametroTipo == 15;

    #endregion

    #region Constructor

    private bool _internalMessagesRegistered;

    public LoteFormularioViewModel()
    {
        RegisterInternalMessages();
    }

    private void RegisterInternalMessages()
    {
        if (_internalMessagesRegistered) return;

        WeakReferenceMessenger.Default.Register<PropriedadeMudouMessage>(this, (r, m) =>
        {
            UpdateTotal();
        });

        _internalMessagesRegistered = true;
    }

    public void EnsureSubscriptions() => RegisterInternalMessages();

    #endregion

    #region Property Changed Handlers

    partial void OnLoteFormularioChanging(LoteFormulario? oldValue, LoteFormulario? newValue)
    {
        if (oldValue != null)
        {
            DetachLoteFormularioSubscriptions(oldValue);
        }
    }

    partial void OnLoteFormularioChanged(LoteFormulario? value)
    {
        OnPropertyChanged(nameof(IdadeLote));

        if (value != null)
        {
            AttachLoteFormularioSubscriptions(value);
        }
    }

    private void AttachLoteFormularioSubscriptions(LoteFormulario value)
    {
        _subscribedLoteFormulario = value;
        value.PropertyChanged += OnLoteFormularioDataChanged;

        AttachLoteFormSubscription(value.LoteForm);
        AttachParametrosCollectionSubscription(value.Formulario_ParametrosComAlternativas);
    }

    private void DetachLoteFormularioSubscriptions(LoteFormulario value)
    {
        value.PropertyChanged -= OnLoteFormularioDataChanged;
        DetachLoteFormSubscription();
        DetachParametrosCollectionSubscription();

        if (ReferenceEquals(_subscribedLoteFormulario, value))
        {
            _subscribedLoteFormulario = null;
        }
    }

    private void OnLoteFormularioDataChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is not LoteFormulario loteFormulario)
            return;

        if (e.PropertyName == nameof(LoteFormulario.LoteForm))
        {
            AttachLoteFormSubscription(loteFormulario.LoteForm);
        }
        else if (e.PropertyName == nameof(LoteFormulario.Formulario_ParametrosComAlternativas))
        {
            AttachParametrosCollectionSubscription(loteFormulario.Formulario_ParametrosComAlternativas);
        }
    }

    private void AttachLoteFormSubscription(LoteForm? loteForm)
    {
        if (ReferenceEquals(_subscribedLoteForm, loteForm))
            return;

        if (_subscribedLoteForm != null)
        {
            _subscribedLoteForm.PropertyChanged -= OnFormDataChanged;
        }

        _subscribedLoteForm = loteForm;

        if (_subscribedLoteForm != null)
        {
            _subscribedLoteForm.PropertyChanged += OnFormDataChanged;
        }
    }

    private void DetachLoteFormSubscription()
    {
        if (_subscribedLoteForm != null)
        {
            _subscribedLoteForm.PropertyChanged -= OnFormDataChanged;
            _subscribedLoteForm = null;
        }
    }

    private void AttachParametrosCollectionSubscription(ObservableCollection<ParametroComAlternativas>? parametros)
    {
        if (ReferenceEquals(_subscribedParametrosCollection, parametros))
            return;

        DetachParametrosCollectionSubscription();
        _subscribedParametrosCollection = parametros;

        if (_subscribedParametrosCollection == null)
            return;

        _subscribedParametrosCollection.CollectionChanged += OnFormularioParametrosCollectionChanged;

        foreach (var parametro in _subscribedParametrosCollection)
        {
            parametro.PropertyChanged += OnParametroDataChanged;
        }
    }

    private void DetachParametrosCollectionSubscription()
    {
        if (_subscribedParametrosCollection == null)
            return;

        _subscribedParametrosCollection.CollectionChanged -= OnFormularioParametrosCollectionChanged;

        foreach (var parametro in _subscribedParametrosCollection)
        {
            parametro.PropertyChanged -= OnParametroDataChanged;
        }

        _subscribedParametrosCollection = null;
    }

    private void OnFormularioParametrosCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        var oldItems = e.OldItems;
        if (oldItems != null)
        {
            foreach (var parametro in oldItems.Cast<object?>().OfType<ParametroComAlternativas>())
            {
                parametro.PropertyChanged -= OnParametroDataChanged;
            }
        }

        var newItems = e.NewItems;
        if (newItems != null)
        {
            foreach (var parametro in newItems.Cast<object?>().OfType<ParametroComAlternativas>())
            {
                parametro.PropertyChanged += OnParametroDataChanged;
            }
        }
    }

    // ✅ Auto-save triggers para seleção de imagens
    partial void OnLoteFormImagem1Changed(LoteFormImagem? value) => _ = SalvaEmAndamentoDebounced();
    partial void OnLoteFormImagem2Changed(LoteFormImagem? value) => _ = SalvaEmAndamentoDebounced();
    partial void OnLoteFormImagem3Changed(LoteFormImagem? value) => _ = SalvaEmAndamentoDebounced();

    private void OnFormDataChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (!_autoSaveEnabled) return;

        if (e.PropertyName == nameof(LoteForm.observacoes))
        {
            _ = SalvaEmAndamentoDebounced();
        }
    }

    private void OnParametroDataChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (!_autoSaveEnabled) return;

        // Se mudou a seleção ou o valor, salva em andamento
        if (e.PropertyName == nameof(ParametroComAlternativas.SelectedIndex) ||
            e.PropertyName == nameof(ParametroComAlternativas.ValorString) ||
            e.PropertyName == nameof(ParametroComAlternativas.ValorInt) ||
            e.PropertyName == nameof(ParametroComAlternativas.ValorDouble) ||
            e.PropertyName == nameof(ParametroComAlternativas.ValorSimNao))
        {
            _ = SalvaEmAndamentoDebounced();
        }
    }

    private async Task SalvaEmAndamentoDebounced()
    {
        _autoSaveCts?.Cancel();
        _autoSaveCts = new CancellationTokenSource();

        try
        {
            await Task.Delay(AUTOSAVE_DEBOUNCE_MS, _autoSaveCts.Token);
            _ = LoteFormulario?.SalvaEmAndamento();
        }
        catch (TaskCanceledException) { }
    }

    partial void OnFaseChanged(int? value)
    {
        OnPropertyChanged(nameof(IdadeLote));
    }

    #endregion

    #region Overrides

    /// <summary>
    /// ✅ REESCRITO: Garante que comandos sejam notificados na Thread Principal quando IsBusy mudar.
    /// </summary>
    protected override void OnIsBusyChanged()
    {
        base.OnIsBusyChanged();
        SalvarCommand.NotifyCanExecuteChanged();
        EditarCommand.NotifyCanExecuteChanged();
        AddNewISIMacroCommand.NotifyCanExecuteChanged();
    }

    #endregion

    #region Public Methods

    public void SetInitialState(
        Lote lote,
        int loteFormId,
        int parametroTipoId,
        int? fase,
        bool isReadOnly,
        bool podeEditar,
        LoteFormulario? recoveredForm = null,
        int loteFormVinculado = -1)
    {
        int loteFormVinculadoResolved = loteFormVinculado;
        if (loteFormVinculadoResolved == -1)
            loteFormVinculadoResolved = recoveredForm?.LoteForm?.loteFormVinculado ?? -1;

        LoteId = (int)lote.id;
        LoteFormId = loteFormId;
        ParametroTipo = parametroTipoId;
        Fase = fase;
        IsReadOnly = isReadOnly;
        PodeEditar = podeEditar;
        Lote = lote;
        LoteFormVinculado = loteFormVinculadoResolved;

        if (recoveredForm != null)
        {
            LoteFormulario = recoveredForm;
            ParametroSelecionado = recoveredForm.ParametroSelecionado;
            Item = recoveredForm.LoteForm?.item;
            Debug.WriteLine($"[LoteFormularioViewModel] Formulario recuperado atribuído.");
        }
        else
        {
            LoteFormulario = new LoteFormulario();
            ParametroSelecionado = null;
            Item = null;
        }

        LoteFormParametros.Clear();
        LoteFormImagensList.Clear();
        AlternativasParametroSelecionado.Clear();

        LoteFormImagem1 = null;
        LoteFormImagem2 = null;
        LoteFormImagem3 = null;
        ModeloIsiMacroSelecionado = null;
        NovoFormulario = false;
        ScoreTotalDaAve = 0;
        PesquisaISIMacroDialogVisible = false;

        if (LoteFormulario?.LoteForm != null && (LoteFormVinculado != -1 || LoteFormulario.LoteForm.loteFormVinculado == null))
            LoteFormulario.LoteForm.loteFormVinculado = LoteFormVinculado;

        Debug.WriteLine($"[LoteFormularioViewModel] ═══ SetInitialState ═══");
        Debug.WriteLine($"  LoteId: {LoteId}");
        Debug.WriteLine($"  LoteFormId: {LoteFormId}");
        Debug.WriteLine($"  ParametroTipo: {ParametroTipo}");
        Debug.WriteLine($"  Fase: {Fase}");
        Debug.WriteLine($"  LoteFormVinculado: {LoteFormVinculado}");
        Debug.WriteLine($"  Recuperado: {recoveredForm != null}");
    }

    // Params saved by NavigationUtils for manual/delayed load
    private bool _pendingLimpaFormulario;
    private int? _pendingModeloIsiMacro;

    public void SetPendingLoadParams(bool limpaFormularioAtual, int? modeloIsiMacroSelecionado)
    {
        _pendingLimpaFormulario = limpaFormularioAtual;
        _pendingModeloIsiMacro = modeloIsiMacroSelecionado;
    }

    [RelayCommand]
    public async Task Carregar()
    {
        Debug.WriteLine($"[LoteFormularioViewModel] ▶ Carregar() chamado | thread={Environment.CurrentManagedThreadId} | main={MainThread.IsMainThread} | pendingModelo={_pendingModeloIsiMacro} | pendingLimpa={_pendingLimpaFormulario}");
        await InicializaFormulario(limpaFormularioAtual: _pendingLimpaFormulario, modeloIsiMacroSelecionado: _pendingModeloIsiMacro);
        Debug.WriteLine($"[LoteFormularioViewModel] ✓ Carregar() concluído | thread={Environment.CurrentManagedThreadId} | main={MainThread.IsMainThread}");
    }

    partial void OnLoteFormVinculadoChanged(int value)
    {
        if (value == -1)
            return;

        if (LoteFormulario?.LoteForm != null)
            LoteFormulario.LoteForm.loteFormVinculado = value;
    }

    public async Task GetItemOrCreateANew() => await InicializaFormulario();

    public void UpdateIdadeLote()
    {
        OnPropertyChanged(nameof(IdadeLote));
    }

    #endregion

    #region Inicialização de Formulário

    public async Task InicializaFormulario(bool limpaFormularioAtual = true, int? modeloIsiMacroSelecionado = null)
    {
        var initSw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            Debug.WriteLine($"[LoteFormularioViewModel] ═══════════════════════════════════");
            Debug.WriteLine($"[LoteFormularioViewModel] InicializaFormulario");
            Debug.WriteLine($"  limpaFormularioAtual: {limpaFormularioAtual}");
            Debug.WriteLine($"  modeloIsiMacroSelecionado: {modeloIsiMacroSelecionado}");
            Debug.WriteLine($"  ParametroTipo: {ParametroTipo}");
            Debug.WriteLine($"  Thread: {System.Threading.Thread.CurrentThread.ManagedThreadId}, IsMainThread: {MainThread.IsMainThread}");

            Debug.WriteLine($"[LoteFormularioViewModel] ▶ Aguardando InvokeOnMainThreadAsync inicial... | thread={Environment.CurrentManagedThreadId} | main={MainThread.IsMainThread}");
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Debug.WriteLine($"[LoteFormularioViewModel] ▶ Dentro do InvokeOnMainThreadAsync inicial | thread={Environment.CurrentManagedThreadId} | main={MainThread.IsMainThread}");
                SubTitle = Traducao.DetalhesDaAnálise;
                ModeloIsiMacroSelecionado = modeloIsiMacroSelecionado;
                IsBusy = true;

                if (limpaFormularioAtual)
                    LoteFormulario = null;
                Debug.WriteLine($"[LoteFormularioViewModel] ✓ InvokeOnMainThreadAsync inicial concluído");
            }).ConfigureAwait(false);
            Debug.WriteLine($"[LoteFormularioViewModel] ✓ Após InvokeOnMainThreadAsync inicial | thread={Environment.CurrentManagedThreadId} | main={MainThread.IsMainThread}");

            // ★★★ OTIMIZAÇÃO: ConfigParametros.UpdateConfig() só atualiza se necessário (tem cache interno) ★★★
            // UnidadeEpidemiologica.PegaListaUE() também é cacheable, não precisa aguardar
            Debug.WriteLine($"[LoteFormularioViewModel] ▶ ConfigParametros.UpdateConfig iniciando... | thread={Environment.CurrentManagedThreadId} | main={MainThread.IsMainThread}");
            await ConfigParametros.UpdateConfig().ConfigureAwait(false);
            Debug.WriteLine($"[LoteFormularioViewModel] ✓ ConfigParametros.UpdateConfig concluído | thread={Environment.CurrentManagedThreadId} | main={MainThread.IsMainThread}");

            Debug.WriteLine($"[LoteFormularioViewModel] ▶ CarregaFormularioAsync iniciando... | thread={Environment.CurrentManagedThreadId} | main={MainThread.IsMainThread}");
            await CarregaFormularioAsync(modeloIsiMacroSelecionado);
            await MainThread.InvokeOnMainThreadAsync(() => MostrarBotaoCarregar = false);
            Debug.WriteLine($"[LoteFormularioViewModel] ✓ CarregaFormularioAsync concluído | ms={initSw.ElapsedMilliseconds} | thread={Environment.CurrentManagedThreadId} | main={MainThread.IsMainThread}");

            Debug.WriteLine($"[LoteFormularioViewModel] ═══════════════════════════════════");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LoteFormularioViewModel] ✖ Erro em InicializaFormulario: {ex.Message}");
            await MainThread.InvokeOnMainThreadAsync(() =>
                PopUpOK.ShowAsync(Traducao.Erro, $"Erro ao inicializar: {ex.Message}"));
        }
        finally
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                IsBusy = false;
                SalvarCommand.NotifyCanExecuteChanged();
                EditarCommand.NotifyCanExecuteChanged();
                Debug.WriteLine($"[LoteFormularioViewModel] finally: IsBusy=false | ms={initSw.ElapsedMilliseconds} | thread={Environment.CurrentManagedThreadId} | main={MainThread.IsMainThread}");
            }).ConfigureAwait(false);
        }
    }

    private async Task CarregaFormularioAsync(int? modeloIsiMacroSelecionado = null)
    {
        var loadSw = System.Diagnostics.Stopwatch.StartNew();
        Debug.WriteLine($"[LoteFormularioViewModel.CarregaFormularioAsync] ▶ Iniciando, LoteFormId={LoteFormId} | thread={Environment.CurrentManagedThreadId} | main={MainThread.IsMainThread}");
        Debug.WriteLine($"[LoteFormularioViewModel.CarregaFormularioAsync] ▶ Antes de SetTitle(MainThread.InvokeOnMainThreadAsync) | thread={Environment.CurrentManagedThreadId} | main={MainThread.IsMainThread}");
        await MainThread.InvokeOnMainThreadAsync(() => SetTitle()).ConfigureAwait(false);
        Debug.WriteLine($"[LoteFormularioViewModel.CarregaFormularioAsync] ✓ SetTitle concluído | ms={loadSw.ElapsedMilliseconds} | thread={Environment.CurrentManagedThreadId} | main={MainThread.IsMainThread}");

        bool novoLoteForm = LoteFormId == -1;

        // Se já temos um LoteFormulario (vindo de recovery ou navegação interna), 
        // e ele já tem dados (parâmetros ou avaliações), consideramos que estamos continuando.
        bool jaTemDados = LoteFormulario != null &&
                         (LoteFormulario.Formulario_ParametrosComAlternativas.Any() ||
                          LoteFormulario.ListaAvaliacoesGalpao.Any());

        var continuandoPreenchimento = LoteFormulario != null && jaTemDados;

        LoteFormulario ??= new LoteFormulario();
        await MainThread.InvokeOnMainThreadAsync(() => SetTitle()).ConfigureAwait(false);

        Debug.WriteLine($"[LoteFormularioViewModel.CarregaFormularioAsync] ▶ Carregando lote... | ms={loadSw.ElapsedMilliseconds} | thread={Environment.CurrentManagedThreadId} | main={MainThread.IsMainThread}");
        var loteResult = await Lote.PegaLoteAsync(LoteId).ConfigureAwait(false);
        Debug.WriteLine($"[LoteFormularioViewModel.CarregaFormularioAsync] ✓ Lote carregado | ms={loadSw.ElapsedMilliseconds} | thread={Environment.CurrentManagedThreadId} | main={MainThread.IsMainThread}");
        Debug.WriteLine($"[CF] B: loteResult={loteResult?.id}, chamando EnsureNames...");
        loteResult?.EnsureNames();
        Debug.WriteLine($"[CF] C: EnsureNames ok, carregando loteFormResult...");
        var loteFormResult = novoLoteForm || continuandoPreenchimento
            ? null
            : await LoteForm.PegaFormulariosLotePorLoteFormId(LoteFormId).ConfigureAwait(false);
        Debug.WriteLine($"[CF] D: loteFormResult ok");

        Debug.WriteLine($"[LoteFormularioViewModel.CarregaFormularioAsync] ▶ Postando para main thread Lote... | ms={loadSw.ElapsedMilliseconds} | thread={Environment.CurrentManagedThreadId} | main={MainThread.IsMainThread}");
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Debug.WriteLine($"[LoteFormularioViewModel.CarregaFormularioAsync] ▶ Dentro do BeginInvokeOnMainThread Lote | thread={Environment.CurrentManagedThreadId} | main={MainThread.IsMainThread}");
            Lote = loteResult;
            if (!novoLoteForm && !continuandoPreenchimento)
            {
                LoteFormulario.LoteForm = loteFormResult;
                LoteFormVinculado = loteFormResult?.loteFormVinculado ?? -1;
            }
            else if (LoteFormulario?.LoteForm != null)
            {
                LoteFormVinculado = LoteFormulario.LoteForm.loteFormVinculado ?? LoteFormVinculado;
            }
            Debug.WriteLine($"[LoteFormularioViewModel.CarregaFormularioAsync] ✓ BeginInvokeOnMainThread Lote processado | thread={Environment.CurrentManagedThreadId} | main={MainThread.IsMainThread}");
        });
        Debug.WriteLine($"[LoteFormularioViewModel.CarregaFormularioAsync] ✓ Após postar Lote para main thread | ms={loadSw.ElapsedMilliseconds} | thread={Environment.CurrentManagedThreadId} | main={MainThread.IsMainThread}");

        Debug.WriteLine($"[LoteFormularioViewModel.CarregaFormularioAsync] ▶ CarregaParametrosAsync...");
        await CarregaParametrosAsync(novoLoteForm, modeloIsiMacroSelecionado);
        Debug.WriteLine($"[LoteFormularioViewModel.CarregaFormularioAsync] ✓ CarregaParametrosAsync concluído");

        if (LoteFormulario.LoteForm == null)
        {
            Debug.WriteLine($"[LoteFormularioViewModel.CarregaFormularioAsync] ▶ Novo formulário — inicializando LoteForm");
            InicializaNovoLoteForm(modeloIsiMacroSelecionado);
            await MainThread.InvokeOnMainThreadAsync(() => SetTitle()).ConfigureAwait(false);
            NovoFormulario = true;

            if (IsISIMacro)
            {
                // fire-and-forget: localização obtida após ui aparecer.
                // getcurrentlocation aguarda isbusy=false antes de solicitar permissão/gps.
                _ = GetCurrentLocation();
            }
        }
        else
        {
            NovoFormulario = false;
            _ = CarregaParametrosPreenchidosAsync();
            async Task CarregaParametrosPreenchidosAsync()
            {
                try
                {
                    LoteFormParametros = await LoteFormParametro.ParametroPreenchidoFormularioLote(
                        LoteFormulario?.LoteForm?.id ?? 0);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Erro ao carregar parâmetros: {ex.Message}");
                }
            }
        }

        _ = CarregaImagensComDelayAsync();
        async Task CarregaImagensComDelayAsync()
        {
            await Task.Delay(300);
            await CarregaImagensAsync();
        }

        UpdateTotal();

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            WeakReferenceMessenger.Default.Send(new UpdateScoreMessage());
            OnPropertyChanged(nameof(PrecisaMostrarEscoreAve));
        }).ConfigureAwait(false);

        // Salva o estado como "em andamento" para recuperação em caso de encerramento repentino
        // Otimização: Não salva se já salvou recentemente (evita I/O excessivo na carga)
        _ = LoteFormulario?.SalvaEmAndamento();
        _autoSaveEnabled = true;
    }

    private void SetTitle()
    {
        Title = ParametroTipo switch
        {
            4 => $"{Traducao.Manejo} - {Traducao.Vazio}",
            5 => $"{Traducao.Manejo} - {Traducao.Alojamento}",
            6 => $"{Traducao.Manejo} - {Traducao.Inicial}",
            7 => $"{Traducao.Manejo} - {Traducao.Crescimento1}",
            8 => $"{Traducao.Manejo} - {Traducao.Crescimento2}",
            9 => $"{Traducao.Manejo} - {Traducao.Final}",
            10 => $"{Traducao.Manejo} - {Traducao.Biosseguridade}",
            11 => (Fase == 7) 
                ? $"{Traducao.Zootécnico} Geral - {(LoteFormulario?.LoteForm?.data ?? DateTime.Now):dd/MM/yyyy} - {IdadeLote}"
                : (Fase == null 
                    ? $"{Traducao.Zootécnico}" 
                    : $"{Traducao.Zootécnico} - {FaseComoDias} {Traducao.Dias}"),
            12 => $"{Traducao.Sanidade} - {Traducao.Nutrição}",
            13 => $"{Traducao.Sanidade} - {Traducao.DiagnósticoDeEnfermidade}",
            14 => $"{Traducao.Sanidade} - {Traducao.TratamentoViaÁgua}",
            18 => $"{Traducao.Sanidade} - {Traducao.AnáliseDeSalmonella}",
            15 => Traducao.ISIMacro,
            16 => Traducao.Vacinas,
            17 => Traducao.ISIMicro,
            21 => $"{Traducao.Manejo}", //Manejo Personalizado
            _ => Title
        };

        OnPropertyChanged(nameof(IsISIMacro));
    }

    private async Task CarregaParametrosAsync(bool novoLoteForm, int? modeloIsiMacroSelecionado)
    {
        // ★★★ RECUPERAÇÃO: Se já temos parâmetros, não carregamos novamente ★★★
        if (LoteFormulario.Formulario_ParametrosComAlternativas.Any())
        {
            Debug.WriteLine($"[CarregaParametrosAsync] ✓ Já possui dados de parâmetros (Recovery?) - Ignorando carga");
            return;
        }

        var cacheKey = $"{ParametroTipo}_{(novoLoteForm ? -1 : LoteFormId)}_{LoteFormVinculado}_{modeloIsiMacroSelecionado}";

        List<ParametroComAlternativas> parametros;

        if (novoLoteForm)
        {
            parametros = (await ParametroComAlternativas.LoteForm_PegaListaParametros(
                ParametroTipo,
                -1,
                LoteFormVinculado,
                modeloIsiMacroSelecionado)).ToList();
        }
        else if (!_parametrosCache.TryGetValue(cacheKey, out parametros))
        {
            parametros = (await ParametroComAlternativas.LoteForm_PegaListaParametros(
                ParametroTipo,
                LoteFormId,
                LoteFormVinculado,
                modeloIsiMacroSelecionado)).ToList();

            if (_parametrosCache.Count > 10)
                _parametrosCache.Clear();

            _parametrosCache[cacheKey] = parametros;
        }

        var parametrosCollection = new ObservableCollection<ParametroComAlternativas>(parametros);

        // BeginInvokeOnMainThread (fire-and-forget) em vez de await InvokeOnMainThreadAsync.
        //
        // MOTIVO DO HANG: InvokeOnMainThreadAsync posta para a main thread E bloqueia a
        // thread de background aguardando tcs.SetResult(). A main thread, ao processar o
        // callback, aciona o SfListView para re-layout síncrono de N itens com DataTemplates
        // complexos. Enquanto o layout nativo não termina, tcs.SetResult() nunca é chamado
        // → deadlock.
        //
        // Com BeginInvokeOnMainThread a thread de background não espera. A fila do main
        // thread garante que esta callback roda ANTES do InvokeOnMainThreadAsync do finally
        // (IsBusy=false), preservando a ordem correta. Isso também elimina o bug Android
        // de itens invisíveis até o scroll: o SfListView tem um render frame para medir
        // os itens antes de IsBusy=false mudar a visibilidade.
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (LoteFormulario != null)
                LoteFormulario.Formulario_ParametrosComAlternativas = parametrosCollection;
        });
    }

    private async Task CarregaImagensAsync()
    {
        if (LoteFormulario?.LoteForm?.id > 0)
        {
            try
            {
                // ★★★ OTIMIZAÇÃO: Carrega em background (não bloqueia UI) ★★★
                var imagens = await LoteFormImagem.PegaImagens((int)LoteFormulario.LoteForm.id)
                    .ConfigureAwait(false);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    LoteFormImagensList.Clear();

                    var imagensList = imagens.Take(3).ToList();
                    foreach (var img in imagensList)
                        LoteFormImagensList.Add(img);

                    LoteFormImagem1 = LoteFormImagensList.Count >= 1 ? LoteFormImagensList[0] : null;
                    LoteFormImagem2 = LoteFormImagensList.Count >= 2 ? LoteFormImagensList[1] : null;
                    LoteFormImagem3 = LoteFormImagensList.Count >= 3 ? LoteFormImagensList[2] : null;
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao carregar imagens: {ex.Message}");
            }
        }
    }

    private void InicializaNovoLoteForm(int? modeloIsiMacroSelecionado)
    {
        LoteFormulario.LoteForm = new LoteForm
        {
            data = DateTime.Now,
            id = 0,
            loteId = LoteId,
            loteFormVinculado = LoteFormVinculado,
            parametroTipoId = ParametroTipo,
            loteFormFase = Fase,
            item = Item,
            loteVisita = Lote?.VisitaAbertaId,
            modeloisimacro = modeloIsiMacroSelecionado,
            dataInicioPreenchimento = DateTime.Now
        };

        //Se for Zootécnico, seta a data conforme a fase (exceto Geral que mantém a data atual)
        if (ParametroTipo == 11 && Lote?.dataInicio != null && Fase != null && Fase != 7)
            LoteFormulario.LoteForm.data = ((DateTime)Lote.dataInicio).AddDays(FaseComoDias);
    }

    // Fire-and-forget: roda em background após o formulário estar visível.
    // NUNCA mostra dialogs (permission request ou popups) — qualquer apresentação
    // de VC em background conflita com navegação em andamento no iOS, causando hang.
    // Se a permissão ainda não foi concedida, apenas registra e sai silenciosamente.
    private async Task GetCurrentLocation()
    {
        try
        {
            // Aguarda IsBusy=false (formulário visível) + margem para navegação se estabilizar
            var waited = 0;
            while (IsBusy && waited < 5000)
            {
                await Task.Delay(100);
                waited += 100;
            }
            await Task.Delay(500); // margem para PushModalAsync / animações de navegação

            // Apenas CHECK — nunca RequestAsync de background (quebraria navegação iOS)
            var status = await MainThread.InvokeOnMainThreadAsync(
                () => Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>());

            Debug.WriteLine($"[GetCurrentLocation] Permissão: {status}");

            if (status != PermissionStatus.Granted)
            {
                Debug.WriteLine($"[GetCurrentLocation] ⚠️ Permissão não concedida — coordenadas não salvas");
                return;
            }

            // GetLastKnownLocationAsync: imediato, usa cache do SO
            Location? location = null;
            try
            {
                location = await Geolocation.GetLastKnownLocationAsync();
                if (location != null)
                    Debug.WriteLine($"[GetCurrentLocation] ✓ Última conhecida: lat={location.Latitude}, lon={location.Longitude}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GetCurrentLocation] ⚠️ GetLastKnown: {ex.Message}");
            }

            if (location == null)
            {
                // iOS: CLLocationManager deve iniciar na main thread.
                // Task.WhenAny = timeout externo garantido independente do comportamento
                // interno do GetLocationAsync com CancellationToken.
                var locationTask = MainThread.InvokeOnMainThreadAsync(
                    () => Geolocation.GetLocationAsync(
                        new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(8))));

                var winner = await Task.WhenAny(locationTask, Task.Delay(9000));
                if (winner == locationTask)
                {
                    location = await locationTask;
                    Debug.WriteLine($"[GetCurrentLocation] ✓ lat={location?.Latitude}, lon={location?.Longitude}");
                }
                else
                {
                    Debug.WriteLine($"[GetCurrentLocation] ⚠️ Timeout — coordenadas não salvas");
                }
            }

            if (location != null && LoteFormulario?.LoteForm != null)
            {
                LoteFormulario.LoteForm.latitude = location.Latitude;
                LoteFormulario.LoteForm.longitute = location.Longitude;
                Debug.WriteLine($"[GetCurrentLocation] ✅ Coordenadas salvas");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[GetCurrentLocation] ❌ {ex.GetType().Name} — {ex.Message}");
        }
    }

    #endregion

    #region Update Score - Debounced

    private async Task UpdateTotalDebounced()
    {
        _updateTotalCts?.Cancel();
        _updateTotalCts = new CancellationTokenSource();

        try
        {
            await Task.Delay(UPDATE_DEBOUNCE_MS, _updateTotalCts.Token);
            UpdateTotal();
        }
        catch (TaskCanceledException) { }
    }

    public void UpdateTotal()
    {
        if (LoteFormulario?.LoteForm == null) return;

        // ★★★ CORREÇÃO: Garante execução na UI thread para propriedades observáveis ★★★
        if (!MainThread.IsMainThread)
        {
            MainThread.BeginInvokeOnMainThread(() => UpdateTotal());
            return;
        }

        try
        {
            int oldScore = ScoreTotalDaAve;

            // ★★★ OTIMIZAÇÃO: Cache de parâmetros com alternativas ★★★
            var parametrosValidos = LoteFormulario.Formulario_ParametrosComAlternativas
                .Where(p => p?.AlternativaSelecionada != null)
                .ToList();

            if (parametrosValidos.Count == 0)
            {
                ScoreTotalDaAve = 0;
            }
            else
            {
                // Usa p.Nota (e não score * peso diretamente) para respeitar a regra
                // dos parâmetros "Isolados" (Tipo = 1): eles aparecem no formulário
                // mas a propriedade Nota retorna 0, portanto não somam ao score total.
                ScoreTotalDaAve = parametrosValidos
                    .Sum(p => p.Nota);

                if (oldScore != ScoreTotalDaAve)
                {
                    foreach (var p in parametrosValidos)
                    {
                        Debug.WriteLine($"Param: {p.nome}, Score: {p.AlternativaSelecionada.score}, Peso: {p.peso}, Total: {p.AlternativaSelecionada.score * p.peso}");
                    }
                    Debug.WriteLine($"Score Total: {ScoreTotalDaAve}");
                }
            }

            // ★★★ OTIMIZAÇÃO: Só atualiza UI se mudou ★★★
            if (oldScore != ScoreTotalDaAve)
            {
                OnPropertyChanged(nameof(ScoreTotalDaAve));
                OnPropertyChanged(nameof(ScoreTotalDaAveCor));
                OnPropertyChanged(nameof(PrecisaMostrarEscoreAve));
                OnPropertyChanged(nameof(RealizadoColor));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Erro UpdateTotal: {ex.Message}");
        }
    }

    #endregion

    #region Commands

    [RelayCommand(CanExecute = nameof(NotBusy), AllowConcurrentExecutions = false)]
    public async Task Salvar()
    {
        if (LoteFormulario == null || LoteFormulario.LoteForm == null)
            return;

        try
        {

            if (NovoFormulario)
                LoteFormulario.LoteForm.dataTerminoPreenchimento = DateTime.Now;

            {
                foreach (var parametro in LoteFormulario?.Formulario_ParametrosComAlternativas ?? Enumerable.Empty<ParametroComAlternativas>())
                {
                    if (parametro.SelectedIndex != -1) continue;

                    for (int index = 0; index < parametro.ListaAlternativas.Count; index++)
                    {
                        if (parametro.ListaAlternativas[index].valorPadrao == 1)
                        {
                            parametro.SelectedIndex = index;
                            break;
                        }
                    }

                    UpdateTotal();
                }

                if (ScoreTotalDaAve >= 40)
                {
                    if (!await PopUpYesNo.ShowAsync(
                        Traducao.SaveAndReturn_AveNãoSaudável,
                        Traducao.AveDoente,
                        Traducao.Sim,
                        Traducao.Não))
                        return;
                }
            }

            if (LoteFormulario?.Formulario_ParametrosComAlternativas == null)
            {
                WeakReferenceMessenger.Default.Send(new SilvaData.Utilities.HighlightRequiredFieldsMessage(GetValidationHostPage()));
                await Helpers.ShowRequiredFieldsPopupAsync(Array.Empty<string>());
                return;
            }

            var camposObrigatoriosNaoPreenchidos = LoteFormulario.Formulario_ParametrosComAlternativas
                .Where(p => p.required == 1 && !p.EstaRespondida)
                .Select(p => p.nome)
                .ToList();

            if (camposObrigatoriosNaoPreenchidos.Any())
            {
                WeakReferenceMessenger.Default.Send(new SilvaData.Utilities.HighlightRequiredFieldsMessage(GetValidationHostPage()));
                var campos = string.Join(", ", camposObrigatoriosNaoPreenchidos);
                Debug.WriteLine($"[VALIDATION ERROR] Tentativa de salvar negada. Campos obrigatórios não preenchidos: {campos}");
                await Helpers.ShowRequiredFieldsPopupAsync(camposObrigatoriosNaoPreenchidos);
                return;
            }

            IsBusy = true;
            await LoteForm.SalvaLoteFormularioAsync(LoteFormulario?.LoteForm);

            await Parametros.SalvaParametros(
                LoteFormulario?.Formulario_ParametrosComAlternativas,
                "LoteFormParametro",
                "loteFormId",
                (LoteFormulario?.LoteForm.id ?? 0).ToString());

            await LoteFormImagem.DeletaImagens(LoteFormulario?.LoteForm);

            var imagensTasks = new List<Task>();
            if (LoteFormImagem1 != null)
            {
                LoteFormImagem1.LoteFormId = LoteFormulario?.LoteForm.id ?? 0;
                imagensTasks.Add(LoteFormImagem.AdicionaImagem(LoteFormImagem1));
            }
            if (LoteFormImagem2 != null)
            {
                LoteFormImagem2.LoteFormId = LoteFormulario?.LoteForm.id ?? 0;
                imagensTasks.Add(LoteFormImagem.AdicionaImagem(LoteFormImagem2));
            }
            if (LoteFormImagem3 != null)
            {
                LoteFormImagem3.LoteFormId = LoteFormulario?.LoteForm.id ?? 0;
                imagensTasks.Add(LoteFormImagem.AdicionaImagem(LoteFormImagem3));
            }
            if (imagensTasks.Any())
                await Task.WhenAll(imagensTasks);

            LoteFormulario.ApagaEmAndamento();
            Salvou = true;
            HapticHelper.VibrateSuccess();

            if (IsISIMacro && LoteFormulario?.LoteForm?.loteId > 0)
            {
                var loteId = (int)LoteFormulario.LoteForm.loteId;
                await Lote.AtualizaISIMacroScoreMedio(loteId, true);
                WeakReferenceMessenger.Default.Send(new ISIMacroSalvoMessage(loteId));
            }

            WeakReferenceMessenger.Default.Send(new FormularioSalvoMessage(LoteFormulario.LoteForm));

            // ★ NOVO: Garante título atualizado para o popup (Data e Idade)
            SetTitle();

            if (NovoFormulario && IsISIMacro)
            {
                if (await PopUpYesNo.ShowAsync(
                    $"{Title} - {Traducao.ScoreSemDoisPontos}: {ScoreTotalDaAve}",
                    Traducao.ISIMacroAdicionadoComSucesso,
                    Traducao.CadastrarMais1,
                    Traducao.Voltar))
                {
                    await AddNewISIMacro();
                    return;
                }
            }
            else if (NovoFormulario)
            {
                await PopUpOK.ShowAsync(Title, Traducao.AdicionadoComSucesso);
            }
            else
            {
                await PopUpOK.ShowAsync(Title, Traducao.AsAlteraçõesForamSalvas);
            }

            await Voltar();
        }
        catch (Exception ex)
        {
            await PopUpOK.ShowAsync(Traducao.Erro, $"Erro ao salvar: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(PrecisaMostrarEscoreAve));
        }
    }

    [RelayCommand]
    public async Task AddNewISIMacro()
    {
        try
        {
            Cleanup();

            LoteFormulario = new LoteFormulario();
            LoteFormParametros.Clear();
            LoteFormImagensList.Clear();

            LoteFormImagem1 = null;
            LoteFormImagem2 = null;
            LoteFormImagem3 = null;

            NovoFormulario = true;
            IsReadOnly = false;
            Salvou = false;
            ScoreTotalDaAve = 0;
            LoteFormId = -1;

            // Evita nova consulta ao banco no iOS logo após salvar/fechar um formulário.
            // Para fluxo de cadastro contínuo, o próximo item é o item atual + 1.
            if (ParametroTipo == 15 && LoteId > 0)
            {
                try
                {
                    Item = Math.Max(Item.GetValueOrDefault(), 0) + 1;
                    Debug.WriteLine($"[AddNewISIMacro] 🔄 Item recalculado: {Item}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[AddNewISIMacro] ⚠️ Erro ao calcular próximo item: {ex.Message}");
                    // Plano B: incrementa item existente se disponível
                    if (Item.HasValue)
                        Item = Item.Value + 1;
                }
            }

            var cacheKey = $"{ParametroTipo}_-1_{LoteFormVinculado}_{ModeloIsiMacroSelecionado}";
            if (_parametrosCache.ContainsKey(cacheKey))
                _parametrosCache.Remove(cacheKey);

            await InicializaFormulario(limpaFormularioAtual: true, modeloIsiMacroSelecionado: ModeloIsiMacroSelecionado);
        }
        catch (Exception ex)
        {
            await PopUpOK.ShowAsync(Traducao.Erro, $"{Traducao.Erro}: {ex.Message}");
        }
    }

    [RelayCommand(CanExecute = nameof(NotBusy))]
    public async Task Editar()
    {
        IsBusy = true;
        IsReadOnly = false;
        Salvou = false;
        OnPropertyChanged(nameof(PrecisaMostrarEscoreAve));
        WeakReferenceMessenger.Default.Send(new UpdateScoreMessage());
        IsBusy = false;
        await Task.CompletedTask;
    }

    [RelayCommand]
    public void ProcurarISIMacro() => PesquisaISIMacroDialogVisible = !PesquisaISIMacroDialogVisible;

    #endregion

    #region Navigation

    public override async Task Voltar()
    {
        if (IsReadOnly || Salvou)
        {
            await base.Voltar();
            return;
        }

        if (await NavigationUtils.PopModalSeConfirmar())
            LoteFormulario?.ApagaEmAndamento();
    }

    #endregion

    #region Atualização de Média ISI Score

    /// <summary>
    /// ★★★ NOVO: Atualiza a média ISI Score de um lote específico após salvar formulário ★★★
    /// Este método garante que a média seja recalculada imediatamente após salvar um novo formulário,
    /// sem precisar esperar pela próxima sincronização.
    /// </summary>
    private async Task AtualizaMediaLoteEspecifico(int loteId)
    {
        try
        {
            Debug.WriteLine($"[LoteFormularioViewModel] 🔄 Atualizando média ISI Score do lote {loteId}");

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Usa a mesma lógica do SyncService.AtualizaTodasMediasISIMacroComTransacao()
            // mas focada apenas no lote específico para melhor performance
            await Db.ExecuteAsync("DROP TABLE IF EXISTS temp.tmp_form_scores");
            await Db.ExecuteAsync("CREATE TEMP TABLE IF NOT EXISTS tmp_form_scores (LoteId INT NOT NULL, ISIMacroScore REAL)");

            var insertSql = @"
                INSERT INTO tmp_form_scores (LoteId, ISIMacroScore)
                SELECT 
                    lf.loteId,
                    SUM(pa.score * p.peso) AS ISIMacroScore
                FROM LoteFormParametro lfp
                INNER JOIN LoteForm lf ON lf.id = lfp.LoteFormId
                INNER JOIN Parametro p ON p.id = lfp.parametroId AND p.Tipo = 0  -- Tipo = 0: exclui parâmetros Isolados do cálculo de score médio do lote
                INNER JOIN ParametroAlternativas pa ON pa.idParametro = p.id AND pa.id = lfp.valor
                WHERE lf.parametroTipoId = 15 AND lf.loteId = ?
                GROUP BY lf.loteId, lfp.LoteFormId;";

            await Db.ExecuteAsync(insertSql, loteId);

            var updateSql = @"
                UPDATE Lote AS l
                SET ISIMacroScoreMedio = (
                    SELECT AVG(t.ISIMacroScore)
                    FROM tmp_form_scores t
                    WHERE t.LoteId = l.Id
                )
                WHERE EXISTS (SELECT 1 FROM tmp_form_scores t2 WHERE t2.LoteId = l.Id) AND l.Id = ?;";

            await Db.ExecuteAsync(updateSql, loteId);
            await Db.ExecuteAsync("DROP TABLE IF EXISTS temp.tmp_form_scores");

            stopwatch.Stop();
            Debug.WriteLine($"[LoteFormularioViewModel] ⚡ Média ISI Score do lote {loteId} atualizada em {stopwatch.ElapsedMilliseconds}ms");

            // Busca o novo valor para enviar na mensagem
            var loteAtualizado = await Lote.PegaLoteAsync(loteId, forceRefresh: true);
            var novoScoreMedio = loteAtualizado?.ISIMacroScoreMedio ?? 0;

            // Envia mensagem para atualizar o Dashboard se estiver aberto
            WeakReferenceMessenger.Default.Send(new ISIMacroScoreMedioAtualizadoMessage(loteId, novoScoreMedio));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LoteFormularioViewModel] ❌ Erro ao atualizar média do lote {loteId}: {ex.Message}");
        }
    }

    #endregion

    #region Cleanup

    public override void Cleanup()
    {
        base.Cleanup(); // resets IsBusy, IsRefreshing, Salvou

        _updateTotalCts?.Cancel();
        _updateTotalCts?.Dispose();
        _updateTotalCts = null;

        if (LoteFormulario != null)
        {
            DetachLoteFormularioSubscriptions(LoteFormulario);
        }

        LoteFormParametros?.Clear();
        LoteFormImagensList?.Clear();
        LoteFormulario = null;

        // Limpa estado do formulário para evitar persistência entre cadastros (Singleton VM)
        LoteFormImagem1 = null;
        LoteFormImagem2 = null;
        LoteFormImagem3 = null;

        ScoreTotalDaAve = 0;
        IsReadOnly = false;
        LoteFormId = -1;
        LoteFormVinculado = -1;

        PesquisaISIMacroDialogVisible = false;
    }

    #endregion
}
