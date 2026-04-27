using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using ISIInstitute.Views.LoteViews;

using SilvaData.FontAwesome;
using SilvaData.Models;
using SilvaData.Pages.PopUps;

using Newtonsoft.Json;

using Syncfusion.Maui.ListView;

using System.Collections.ObjectModel;
using System.Diagnostics;

using static SilvaData.Utilities.ISIUtils;

namespace SilvaData.ViewModels
{
    public partial class LoteViewModel : ViewModelBase, IDisposable
    {
        // ═══════════════════════════════════════════════════════════
        // ★★★ PROPRIEDADES OBSERVÁVEIS ★★★
        // ═══════════════════════════════════════════════════════════

        [ObservableProperty]
        private ObservableRangeCollection<Lote> listaLotes = new();

        [ObservableProperty]
        private Lote? loteSelecionado;

        [ObservableProperty]
        private string title = string.Empty;

        [ObservableProperty]
        private string pesquisaLoteText = string.Empty;

        [ObservableProperty]
        private bool pesquisaVisible = true;

        [ObservableProperty]
        private bool isNovoLoteVisible = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(OrdemLotesIcone))]
        private bool lotesMaisNovosPrimeiro = true;

        public SfListView? SfListView;

        public string OrdemLotesIcone => LotesMaisNovosPrimeiro
            ? FontAwesomeIcons.ArrowDownWideShort
            : FontAwesomeIcons.ArrowUpWideShort;

        // ═══════════════════════════════════════════════════════════
        // ★★★ CAMPOS PRIVADOS ★★★
        // ═══════════════════════════════════════════════════════════

        private readonly CacheService _cacheService;
        private bool _isDisposed = false;
        private readonly SemaphoreSlim _carregaLotesLock = new(1, 1);
        private bool _jaInicializou = false;
        private PropertyChangedEventHandler? _permissoesChangedHandler;

        // Debounce
        private CancellationTokenSource? _debounceTokenSource;
        private const int DEBOUNCE_DELAY_MS = 300;

        // Cache de filtros (otimização)
        private string? _pesquisaNormalizada;
        private int? _filtroUEId;

        // Dicionários para lookup O(1)
        private Dictionary<int, UnidadeEpidemiologicaComDetalhes> _ueDict = new();

        // ═══════════════════════════════════════════════════════════
        // ★★★ CONSTRUTOR ★★★
        // ═══════════════════════════════════════════════════════════

        public LoteViewModel(CacheService cacheService)
        {
            _cacheService = cacheService;

            // Inicializa dicionários de lookup
            RebuildLookupDictionaries();

            // Registra listeners do Messenger
            WeakReferenceMessenger.Default.Register<NovoLoteMessage>(this, (r, m) => HandleNovoLote(m));
            WeakReferenceMessenger.Default.Register<LoteAlteradoMessage>(this, (r, m) => HandleLoteAlterado(m));
            WeakReferenceMessenger.Default.Register<ISIMacroSalvoMessage>(this, (r, m) => HandleISIMacroSalvo(m));
            WeakReferenceMessenger.Default.Register<UpdateDadosIniciaisMessage>(this, (r, m) => ForceRefreshAsync());

            // Listener para atualizar IsNovoLoteVisible quando permissões mudarem (iOS issue fix)
            _permissoesChangedHandler = (s, e) =>
            {
                if (!_isDisposed)
                    IsNovoLoteVisible = Permissoes.UsuarioPermissoes?.lotes.cadastrar ?? false;
            };
            Permissoes.StaticPropertyChanged += _permissoesChangedHandler;
        }

        // ═══════════════════════════════════════════════════════════
        // ★★★ INICIALIZAÇÃO ★★★
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Método de inicialização EXPLÍCITA (chamado apenas pela MainPageModel)
        /// </summary>
        public async Task InitializeAsync(bool forceRefresh = false)
        {
            if (_jaInicializou && !forceRefresh) return;

            if (IsBusy)
            {
                Debug.WriteLine("[LoteViewModel] InitializeAsync ignorado: já está ocupado");
                return;
            }

            try
            {
                IsBusy = true;
                RebuildLookupDictionaries();
                await CarregaLotes(forceRefresh);
                _jaInicializou = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoteViewModel] Erro na inicialização: {ex.Message}");
                await ErrorHandler.ShowErrorAsync("Erro ao carregar lotes!", ex);
            }
            finally
            {
                IsBusy = false;
            }

            // Verifica formulário em andamento apenas na inicialização,
            // não em cada CarregaLotes (que pode ser chamado por filtros/pesquisa).
            await VerificaSeTemFormularioEmAndamento();

            IsNovoLoteVisible = Permissoes.UsuarioPermissoes?.lotes.cadastrar ?? false;
        }

        /// <summary>
        /// Força um refresh completo (usado por mensagens de atualização)
        /// </summary>
        private void ForceRefreshAsync()
        {
            if (_isDisposed || IsBusy) return;
            _ = ForceRefreshInternalAsync();
        }

        /// <summary>
        /// Implementação interna do refresh com tratamento de exceções
        /// </summary>
        private async Task ForceRefreshInternalAsync()
        {
            try
            {
                await InitializeAsync(forceRefresh: true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoteViewModel] Erro em ForceRefreshInternalAsync: {ex.Message}");
            }
        }

        /// <summary>
        /// Reconstrói os dicionários de lookup (chamar quando cache mudar)
        /// </summary>
        private void RebuildLookupDictionaries()
        {
            try
            {
                // ★ Filtra nulos e converte para int (não nullable)
                _ueDict = _cacheService.UEList
                      .Where(ue => ue.id > 0)
                      .ToDictionary(ue => ue.id);

                Debug.WriteLine($"[LoteViewModel] Dicionários reconstruídos: {_ueDict.Count} UEs");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoteViewModel] Erro ao construir dicionários: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ★★★ HANDLERS DE MENSAGENS (Thread-Safe) ★★★
        // ═══════════════════════════════════════════════════════════

        private void HandleNovoLote(NovoLoteMessage m)
        {
            if (_isDisposed || m.Lote == null) return;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (ListaLotes.FirstOrDefault(l => l?.id == m.Lote.id) == null)
                    ListaLotes.Insert(0, m.Lote);
            });
        }

        private void HandleLoteAlterado(LoteAlteradoMessage m)
        {
            if (_isDisposed || m.Lote == null) return;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var existente = ListaLotes.FirstOrDefault(l => l?.id == m.Lote.id);
                if (existente != null)
                {
                    m.Lote.TransferMetadataFrom(existente);
                    var index = ListaLotes.IndexOf(existente);
                    ListaLotes[index] = m.Lote;
                }
            });
        }

        private void HandleISIMacroSalvo(ISIMacroSalvoMessage m)
        {
            if (_isDisposed || m.LoteId == null) return;
            _ = HandleISIMacroSalvoInternalAsync(m);
        }

        private async Task HandleISIMacroSalvoInternalAsync(ISIMacroSalvoMessage m)
        {
            try
            {
                if (m.LoteId == null) return;

                var loteId = m.LoteId.GetValueOrDefault();

                var loteAtualizado = await Lote.PegaLoteAsync(loteId, forceRefresh: true);
                if (loteAtualizado == null) return;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var existente = ListaLotes.FirstOrDefault(l => l?.id == loteId);
                    if (existente != null)
                    {
                        loteAtualizado.TransferMetadataFrom(existente);
                        var index = ListaLotes.IndexOf(existente);
                        ListaLotes[index] = loteAtualizado;
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoteViewModel] Erro ao atualizar após ISIMacroSalvo: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ★★★ CARREGAMENTO DE LOTES (OTIMIZADO) ★★★
        // ═══════════════════════════════════════════════════════════

        [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
        public async Task CarregaLotes(bool forceRefresh = false)
        {
            // Proteção contra múltiplas execuções simultâneas
            if (!await _carregaLotesLock.WaitAsync(TimeSpan.FromMilliseconds(300)))
            {
                Debug.WriteLine("[LoteViewModel] CarregaLotes ignorado: lock ocupado");
                return;
            }

            try
            {
                Title = Permissoes.TratamentoEmVezDeLote
                    ? Traducao.MeusTratamentos
                    : Traducao.MeusLotes;

                IsBusy = true;

                var sw = Stopwatch.StartNew();

                // ★★★ Processamento em background (não bloqueia UI) ★★★
                var listaFiltrada = await Task.Run(async () =>
                {
                    var novalistaLotes = await Lote.PegaListaLotesAsync(LotesMaisNovosPrimeiro);
                    novalistaLotes.EnsureNames(_cacheService);
                    return novalistaLotes.Where(LoteFilter).ToList();
                });

                // ★★★ Atualização da UI em batch (ReplaceRange = 1 notificação) ★★★
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Debug.WriteLine($"[LoteViewModel] ReplaceRange com {listaFiltrada.Count} lotes");
                    ListaLotes.ReplaceRange(listaFiltrada);
                });

                sw.Stop();
                Debug.WriteLine($"[LoteViewModel] ✅ CarregaLotes concluído em {sw.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoteViewModel] Erro ao carregar lotes: {ex.Message}");
                await ErrorHandler.ShowErrorAsync(Traducao.Erro, ex.Message);
            }
            finally
            {
                _carregaLotesLock.Release();
                IsBusy = false;
            }
        }

        private async Task VerificaSeTemFormularioEmAndamento()
        {
            if (_isDisposed) return;

            try
            {
                var formularioSave = Preferences.Get("FormularioEmAndamento", "");
                if (string.IsNullOrEmpty(formularioSave)) return;

                bool retomar = await PopUpYesNo.ShowAsync(
                    Traducao.FormulárioEmAndamento,
                    Traducao.RetomarPreenchimentoFormulario,
                    Traducao.Sim,
                    Traducao.Não
                );

                if (!retomar)
                {
                    Preferences.Set("FormularioEmAndamento", "");
                    return;
                }

                LoteFormulario? dadosFormulario = null;
                try
                {
                    dadosFormulario = JsonConvert.DeserializeObject<LoteFormulario>(formularioSave);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Falha ao desserializar FormularioEmAndamento: {ex.Message}");
                    Preferences.Set("FormularioEmAndamento", "");
                    return;
                }

                if (dadosFormulario?.LoteForm?.loteId is not { } loteId || loteId <= 0)
                {
                    Debug.WriteLine("FormularioEmAndamento inválido (loteId ausente/inválido)");
                    Preferences.Set("FormularioEmAndamento", "");
                    return;
                }

                var loteParaRetomar = await Lote.PegaLoteAsync(loteId);
                if (loteParaRetomar == null)
                {
                    Debug.WriteLine($"Lote não encontrado para retomar (ID: {loteId})");
                    Preferences.Set("FormularioEmAndamento", "");
                    return;
                }

                var parametroTipoId = dadosFormulario.LoteForm?.parametroTipoId;
                if (parametroTipoId == null || parametroTipoId <= 0)
                {
                    // parametroTipoId inválido: não é possível retomar o formulário.
                    // Limpa a preferência e retorna; o usuário pode navegar ao lote normalmente.
                    Debug.WriteLine("FormularioEmAndamento: parametroTipoId inválido — limpando preferência");
                    Preferences.Set("FormularioEmAndamento", "");
                    return;
                }

                var loteFormId = dadosFormulario.LoteForm?.id ?? 0;
                // id == 0 significa que o formulário nunca foi salvo (novo) → passa -1
                if (loteFormId <= 0) loteFormId = -1;

                await NavigationUtils.OpenLoteFormularioAsync(
                    lote: loteParaRetomar,
                    loteFormId: loteFormId,
                    parametroTipoId: (int)parametroTipoId,
                    fase: dadosFormulario.LoteForm?.loteFormFase,
                    isReadOnly: false,
                    podeEditar: true,
                    item: dadosFormulario.LoteForm?.item,
                    modeloIsiMacroSelecionado: dadosFormulario.LoteForm?.modeloisimacro,
                    parametroSelecionado: dadosFormulario.ParametroSelecionado,
                    recoveredForm: dadosFormulario);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro em VerificaSeTemFormularioEmAndamento: {ex.Message}");
                Preferences.Set("FormularioEmAndamento", "");
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ★★★ COMANDOS DE UI ★★★
        // ═══════════════════════════════════════════════════════════

        private bool CanExecuteCommands() => !IsBusy && !_isDisposed;

        [RelayCommand]
        public void ShowHidePesquisa() => PesquisaVisible = !PesquisaVisible;

        [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
        public async Task LimparFiltros()
        {
            SelectedFiltroUE = null;
            PesquisaLoteText = string.Empty;
            await CarregaLotes();
        }

        [RelayCommand]
        public async Task NovoLote()
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            await NavigationUtils.ShowViewAsModalAsync<LoteEditView>();
        }

        [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
        public async Task InverteOrdemLotes()
        {
            LotesMaisNovosPrimeiro = !LotesMaisNovosPrimeiro;
            await CarregaLotes();
        }

        [RelayCommand]
        private async Task VaiParaLote(Lote lote)
        {
            if (lote == null) return;

            Debug.WriteLine($"[LoteViewModel] VaiParaLote chamado — lote: {lote.numero}");

            try
            {
                // Configura o ViewModel ANTES de abrir (view é Singleton, já está na memória)
                var monitoramentoVm = ServiceHelper.GetRequiredService<LoteMonitoramentoViewModel>();
                monitoramentoVm.SetInitialState(lote);
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);

                await NavigationUtils.ShowViewAsModalAsync<LoteMonitoramentoView>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoteViewModel] Erro ao navegar para LoteMonitoramentoView: {ex.Message}");
                await ErrorHandler.ShowErrorAsync(Traducao.Erro, ex.Message);
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ★★★ FILTRO OTIMIZADO (100x+ mais rápido) ★★★
        // ═══════════════════════════════════════════════════════════

        public bool LoteFilter(object item)
        {
            if (item is not Lote lote || _isDisposed)
                return false;

            if (!lote.unidadeEpidemiologicaId.HasValue)
                return false;

            var ueId = lote.unidadeEpidemiologicaId.Value; // ← int (não nullable)

            // ★★★ FILTRO 1: UE ★★★
            if (_filtroUEId.HasValue && ueId != _filtroUEId.Value)
                return false;

            // ★★★ FILTRO 4: Texto ★★★
            if (string.IsNullOrEmpty(_pesquisaNormalizada))
                return true;

            var numeroNorm = Helpers.NormalizeForSearch(lote.numero);
            if (numeroNorm.Contains(_pesquisaNormalizada))
                return true;

            var ueNomeNorm = Helpers.NormalizeForSearch(lote.UnidadeEpidemiologicaNome);
            if (ueNomeNorm.Contains(_pesquisaNormalizada))
                return true;

            var regionalNorm = Helpers.NormalizeForSearch(lote.RegionalNome);
            if (regionalNorm.Contains(_pesquisaNormalizada))
                return true;

            var propriedadeNorm = Helpers.NormalizeForSearch(lote.PropriedadeNome);
            return propriedadeNorm.Contains(_pesquisaNormalizada);
        }

        // ═══════════════════════════════════════════════════════════
        // ★★★ FILTROS E LISTAS ★★★
        // ═══════════════════════════════════════════════════════════

        [ObservableProperty]
        private UnidadeEpidemiologicaComDetalhes? selectedFiltroUE;

        public ObservableCollection<UnidadeEpidemiologicaComDetalhes> UEList =>
            _isDisposed ? new() : _cacheService.UEList;

        // ★★★ Handlers de mudança de filtro (com cache) ★★★
        partial void OnSelectedFiltroUEChanged(UnidadeEpidemiologicaComDetalhes? value)
        {
            _filtroUEId = value?.id;
            if (!IsBusy) _ = CarregaLotes();
        }

        // ★★★ Handler de pesquisa com DEBOUNCE (300ms) ★★★
        partial void OnPesquisaLoteTextChanged(string value)
        {
            // Normaliza UMA VEZ (cache para reutilização no filtro)
            _pesquisaNormalizada = string.IsNullOrWhiteSpace(value)
                ? null
                : Helpers.NormalizeForSearch(value);

            // Cancela busca anterior (debounce)
            _debounceTokenSource?.Cancel();
            _debounceTokenSource?.Dispose();
            _debounceTokenSource = new CancellationTokenSource();

            var token = _debounceTokenSource.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(DEBOUNCE_DELAY_MS, token);

                    if (!token.IsCancellationRequested && !IsBusy)
                    {
                        Debug.WriteLine($"[LoteViewModel] Pesquisa executada: '{value}'");
                        await CarregaLotes();
                    }
                }
                catch (TaskCanceledException)
                {
                    Debug.WriteLine("[LoteViewModel] Pesquisa cancelada (debounce)");
                }
            }, token);
        }

        // ═══════════════════════════════════════════════════════════
        // ★★★ OVERRIDE DE ISBUSY (Notifica CanExecute) ★★★
        // ═══════════════════════════════════════════════════════════

        protected override void OnIsBusyChanged()
        {
            base.OnIsBusyChanged();

            CarregaLotesCommand.NotifyCanExecuteChanged();
            LimparFiltrosCommand.NotifyCanExecuteChanged();
            InverteOrdemLotesCommand.NotifyCanExecuteChanged();
        }

        // ═══════════════════════════════════════════════════════════
        // ★★★ IDISPOSABLE ★★★
        // ═══════════════════════════════════════════════════════════

        public void Dispose() => Dispose(true);

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (disposing)
            {
                WeakReferenceMessenger.Default.UnregisterAll(this);
                if (_permissoesChangedHandler != null)
                    Permissoes.StaticPropertyChanged -= _permissoesChangedHandler;
                _carregaLotesLock.Dispose();
                _debounceTokenSource?.Cancel();
                _debounceTokenSource?.Dispose();
            }

            _isDisposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
