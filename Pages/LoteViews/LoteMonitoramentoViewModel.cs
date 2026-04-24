using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using ISIInstitute.Views.LoteViews;

using SilvaData.Models;
using SilvaData.Pages.PopUps;
using SilvaData.Utilities;

using System.Collections.ObjectModel;
using System.Diagnostics;

namespace SilvaData.ViewModels
{
    public partial class ParametroGalpaoResumo : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IconText))]
        private Parametro parametro = null!;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(BadgeColor))]
        private int count;

        public string Nome => Parametro?.nome ?? "";

        public string TipoLabel => Parametro?.campoTipo switch
        {
            null or "" or "0" => "Quantitativo",
            "1" => "Qualitativo Único",
            "2" => "Qualitativo Múltiplo",
            _   => ""
        };

        public Brush BadgeColor => Count > 0
            ? new SolidColorBrush(Color.FromArgb("#548C3C"))
            : new SolidColorBrush(Color.FromArgb("#BBBBBB"));

        public string IconText => Parametro?.campoTipo switch
        {
            null or "" or "0" => "≡",
            "1" => "◯",
            "2" => "☑",
            _   => "☑"
        };
    }
    /// <summary>
    /// ViewModel para monitoramento do lote (migrado de Xamarin.Forms para MAUI).
    /// </summary>
    public partial class LoteMonitoramentoViewModel : ViewModelBase
    {
        private readonly CacheService _cacheService;

        [ObservableProperty]
        private Lote? loteAtual;

        [ObservableProperty]
        public bool sanidadeVisible = false;

        [ObservableProperty]
        public string title = string.Empty;

        [ObservableProperty]
        public string monitoramentoLoteText = string.Empty;

        [ObservableProperty]
        bool podeFecharLote;
        
        [ObservableProperty] private bool isManejoVisible = false;
        [ObservableProperty] private bool isZootecnicoVisible = false;
        [ObservableProperty] private bool isIsiMacroVisible = false;
        [ObservableProperty] private bool isNutricaoVisible = false;
        [ObservableProperty] private bool isSanidadeVisible = false;
        [ObservableProperty] private bool isIsiMicroVisible = false;
        [ObservableProperty] private bool isAvaliacaoGalpaoVisible = false;
        [ObservableProperty] private bool isEditarLoteVisible = false;

        // Permissões para sub-módulos de Sanidade
        [ObservableProperty] private bool isDiagnosticoVisible = false;
        [ObservableProperty] private bool isTratamentoVisible = false;
        [ObservableProperty] private bool isSalmonellaVisible = false;
        [ObservableProperty] private bool isVacinasVisible = false;

        // Avaliações de galpão resumo
        [ObservableProperty] private ObservableCollection<ParametroGalpaoResumo> parametrosGalpaoResumo = new();
        [ObservableProperty] private bool isAvaliacaoGalpaoSectionVisible = false;
        [ObservableProperty] private bool isLoadingGalpaoResumo = false;

        public LoteMonitoramentoViewModel(CacheService cacheService)
        {
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));

            Title = Permissoes.TratamentoEmVezDeLote
                ? Traducao.InformaçõesDoTratamento
                : Traducao.InformaçõesDoLote;

            MonitoramentoLoteText = Permissoes.TratamentoEmVezDeLote
                ? Traducao.MonitoramentoDoTratamento
                : Traducao.MonitoramentoDoLote;

            WeakReferenceMessenger.Default.Register<LoteAlteradoMessage>(this, (sender, message) =>
            {
                message.Lote.EnsureNames(_cacheService);
                LoteAtual = message.Lote;
                AtualizaPodeFecharLote();
            });

            WeakReferenceMessenger.Default.Register<ISIMacroScoreMedioAtualizadoMessage>(this, (sender, message) =>
            {
                if (message.LoteId == null || LoteAtual == null || LoteAtual.id == null || LoteAtual.id != message.LoteId) return;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (LoteAtual == null) return;

                    LoteAtual.ISIMacroScoreMedio = message.NovoISIMacroScoreMedio;
                    OnPropertyChanged(nameof(LoteAtual));
                });
            });

            WeakReferenceMessenger.Default.Register<FormularioSalvoMessage>(this, (sender, message) =>
            {
                if (LoteAtual != null && message.FormularioSalvo?.loteId == LoteAtual.id && message.FormularioSalvo?.parametroTipoId == 20)
                {
                    _ = Task.Run(async () => await LoadParametrosGalpaoResumoAsync());
                }
            });

            // Registrar listener para mudanças nas permissões
            Permissoes.StaticPropertyChanged += OnPermissoesChanged;

            AtualizaVisibilidadeModulos();
        }

        private void OnPermissoesChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Atualiza visibilidade dos módulos quando qualquer permissão mudar
            MainThread.BeginInvokeOnMainThread(() =>
            {
                AtualizaVisibilidadeModulos();
            });
        }

        private void AtualizaVisibilidadeModulos()
        {
            IsManejoVisible = Permissoes.PodeVerManejo;
            IsZootecnicoVisible = Permissoes.PodeVerZootecnico;
            IsIsiMacroVisible = Permissoes.PodeVerIsiMacro;
            IsNutricaoVisible = Permissoes.PodeVerNutricao;
            IsSanidadeVisible = Permissoes.PodeVerSanidade;
            if (!IsSanidadeVisible)
                SanidadeVisible = false;
            IsIsiMicroVisible = Permissoes.PodeVerIsiMicro;
            IsAvaliacaoGalpaoVisible = Permissoes.PodeVerAvaliacaoGalpao; // Agora usa galpao.consultar corretamente
            IsEditarLoteVisible = Permissoes.UsuarioPermissoes?.lotes.atualizar ?? false;
            
            // Permissões para sub-módulos de Sanidade
            IsDiagnosticoVisible = Permissoes.UsuarioPermissoes?.lotes.monitoramento.sanidade.consultar ?? false;
            IsTratamentoVisible = Permissoes.UsuarioPermissoes?.lotes.monitoramento.sanidade.consultar ?? false;
            IsSalmonellaVisible = Permissoes.UsuarioPermissoes?.lotes.monitoramento.sanidade.consultar ?? false;
            IsVacinasVisible = Permissoes.UsuarioPermissoes?.lotes.monitoramento.sanidade.consultar ?? false;
            
#if DEBUG
            Debug.WriteLine($"[LoteMonitoramento] Atualizando visibilidade:");
            Debug.WriteLine($"  - Manejo: {IsManejoVisible} (PodeVerManejo={Permissoes.PodeVerManejo})");
            Debug.WriteLine($"  - Zootecnico: {IsZootecnicoVisible} (PodeVerZootecnico={Permissoes.PodeVerZootecnico})");
            Debug.WriteLine($"  - ISIMacro: {IsIsiMacroVisible} (PodeVerIsiMacro={Permissoes.PodeVerIsiMacro})");
            Debug.WriteLine($"  - Nutricao: {IsNutricaoVisible} (PodeVerNutricao={Permissoes.PodeVerNutricao})");
            Debug.WriteLine($"  - Sanidade: {IsSanidadeVisible} (PodeVerSanidade={Permissoes.PodeVerSanidade})");
            Debug.WriteLine($"  - ISIMicro: {IsIsiMicroVisible} (PodeVerIsiMicro={Permissoes.PodeVerIsiMicro})");
            Debug.WriteLine($"  - AvaliacaoGalpao: {IsAvaliacaoGalpaoVisible} (galpao.consultar={Permissoes.UsuarioPermissoes?.lotes.monitoramento.galpao.consultar})");
            Debug.WriteLine($"  - Diagnostico: {IsDiagnosticoVisible} (sanidade.consultar={Permissoes.UsuarioPermissoes?.lotes.monitoramento.sanidade.consultar})");
            Debug.WriteLine($"  - Tratamento: {IsTratamentoVisible} (sanidade.consultar={Permissoes.UsuarioPermissoes?.lotes.monitoramento.sanidade.consultar})");
            Debug.WriteLine($"  - Salmonella: {IsSalmonellaVisible} (sanidade.consultar={Permissoes.UsuarioPermissoes?.lotes.monitoramento.sanidade.consultar})");
            Debug.WriteLine($"  - Vacinas: {IsVacinasVisible} (sanidade.consultar={Permissoes.UsuarioPermissoes?.lotes.monitoramento.sanidade.consultar})");
#endif
        }

        /// <summary>
        /// PADRÃO MAUI: Define o estado inicial com o Lote.
        /// </summary>
        public void SetInitialState(Lote lote)
        {
            if (lote == null)
                throw new ArgumentNullException(nameof(lote));

            // Garante preenchimento de nomes caso não tenham vindo via JOIN (fallback)
            lote.EnsureNames(_cacheService);
            LoteAtual = lote;
            AtualizaPodeFecharLote();
        }

        /// <summary>
        /// Chamado após a página aparecer para carregar os resumos de avaliação de galpão.
        /// </summary>
        public async Task LoadDataAfterAppear()
        {
            await LoadParametrosGalpaoResumoAsync();
        }

        public async Task GetItemOrCreateANew()
        {
            AtualizaPodeFecharLote();
            await Task.CompletedTask;
        }

        [RelayCommand]
        public void ShowHideSanidade()
        {
            SanidadeVisible = !SanidadeVisible;
        }

        [RelayCommand]
        public async Task EditarLote()
        {
            if (LoteAtual == null) return;

            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                await NavigationUtils.ShowViewAsModalAsync<LoteEditView>(LoteAtual);
            }
            catch (Exception ex)
            {
                await PopUpOK.ShowAsync(Traducao.Erro, $"{Traducao.ErroAoAbrir}: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task FecharLote()
        {
            if (LoteAtual == null) return;

            try
            {
                var confirmacao = await PopUpYesNo.ShowAsync(
                    Traducao.FecharLote,
                    Traducao.ConfirmeOFechamentoDolote);

                if (!confirmacao) return;

                IsBusy = true;

                LoteAtual.dataAbate = DateTime.Now;
                LoteAtual.conversaoAlimentarReal ??= 0;
                LoteAtual.mortalidade ??= 0;
                LoteAtual.pesoFinal ??= 0;
                LoteAtual.loteStatus = 2;

                await Lote.SaveLote(LoteAtual);
                AtualizaPodeFecharLote();

                WeakReferenceMessenger.Default.Send(new LoteAlteradoMessage(LoteAtual));

                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                await PopUpOK.ShowAsync(Traducao.Sucesso, Traducao.OLote0FoiFechado);
            }
            catch (Exception ex)
            {
                await PopUpOK.ShowAsync(Traducao.Erro, $"{Traducao.ErroAoFecharLote}: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task ISIMacro()
        {
            if (LoteAtual == null) return;
            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                await Task.Delay(250);
                var isiMacroView = ServiceHelper.GetRequiredService<LoteISIMacroView>();
                isiMacroView.SetInitialState(LoteAtual);

                if (DeviceInfo.Platform == DevicePlatform.iOS)
                {
                    await NavigationUtils.ShowPageAsModalAsync(isiMacroView, animated: false);
                }
                else
                {
                    await NavigationUtils.ShowPageAsModalAsync(isiMacroView);
                }

                await RefreshLoteAtualAsync();
            }
            catch (Exception ex)
            {
                await PopUpOK.ShowAsync(Traducao.Erro, ex.Message);
            }
        }

        [RelayCommand]
        public async Task ISIMicro()
        {
            if (LoteAtual == null) return;
            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                await Task.Delay(250);
                await NavigationUtils.ShowViewAsModalAsync<LoteISIMicroView>(LoteAtual);
            }
            catch (Exception ex)
            {
                await PopUpOK.ShowAsync(Traducao.Erro, ex.Message);
            }
        }

        [RelayCommand]
        public async Task Manejo()
        {
            if (LoteAtual == null) return;
            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                await Task.Delay(250);
                await NavigationUtils.ShowViewAsModalAsync<LoteManejoView>(LoteAtual);
            }
            catch (Exception ex)
            {
                await PopUpOK.ShowAsync(Traducao.Erro, ex.Message);
            }
        }

        [RelayCommand]
        public async Task Zootecnico()
        {
            if (LoteAtual == null) return;
            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                await Task.Delay(250);
                await NavigationUtils.ShowViewAsModalAsync<LoteZootecnicoView>(LoteAtual);
            }
            catch (Exception ex)
            {
                await PopUpOK.ShowAsync(Traducao.Erro, ex.Message);
            }
        }

        [RelayCommand]
        public async Task Nutricao()
        {
            if (LoteAtual == null) return;
            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                await Task.Delay(250);
                await NavigationUtils.ShowViewAsModalAsync<LoteNutricaoView>(LoteAtual);
            }
            catch (Exception ex)
            {
                await PopUpOK.ShowAsync(Traducao.Erro, ex.Message);
            }
        }

        [RelayCommand]
        public async Task Salmonella()
        {
            if (LoteAtual == null) return;
            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                await Task.Delay(250);
                await NavigationUtils.ShowViewAsModalAsync<LoteSalmonellaView>(LoteAtual);
            }
            catch (Exception ex)
            {
                await PopUpOK.ShowAsync(Traducao.Erro, ex.Message);
            }
        }

        [RelayCommand]
        public async Task Vacinas()
        {
            if (LoteAtual == null) return;
            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                await Task.Delay(250);
                await NavigationUtils.ShowViewAsModalAsync<LoteVacinasView>(LoteAtual);
            }
            catch (Exception ex)
            {
                await PopUpOK.ShowAsync(Traducao.Erro, ex.Message);
            }
        }

        [RelayCommand]
        public async Task Diagnostico()
        {
            if (LoteAtual == null) return;
            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                await Task.Delay(250);
                await NavigationUtils.ShowViewAsModalAsync<LoteDiagnosticoView>(LoteAtual);
            }
            catch (Exception ex)
            {
                await PopUpOK.ShowAsync(Traducao.Erro, ex.Message);
            }
        }

        [RelayCommand]
        public async Task Tratamento()
        {
            if (LoteAtual == null) return;
            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                await Task.Delay(250);
                await NavigationUtils.ShowViewAsModalAsync<LoteTratamentoView>(LoteAtual);
            }
            catch (Exception ex)
            {
                await PopUpOK.ShowAsync(Traducao.Erro, ex.Message);
            }
        }

        [RelayCommand]
        public async Task AvaliacoesGalpao()
        {
            if (LoteAtual == null) return;
            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                await Task.Delay(250);
                await NavigationUtils.ShowViewAsModalAsync<LoteAvaliacaoGalpaoView>(LoteAtual);
            }
            catch (Exception ex)
            {
                await PopUpOK.ShowAsync(Traducao.Erro, ex.Message);
            }
        }

        [RelayCommand]
        public async Task VaiParaAvaliacaoGalpao(ParametroGalpaoResumo resumo)
        {
            if (LoteAtual == null || resumo?.Parametro == null) return;
            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                await Task.Delay(250);
                var vm = ServiceHelper.GetRequiredService<LoteAvaliacaoGalpaoViewModel>();
                vm.ParametroInicial = resumo.Parametro;
                await NavigationUtils.ShowViewAsModalAsync<LoteAvaliacaoGalpaoView>(LoteAtual);
                await LoadParametrosGalpaoResumoAsync();
            }
            catch (Exception ex)
            {
                await PopUpOK.ShowAsync(Traducao.Erro, ex.Message);
            }
        }

        private async Task LoadParametrosGalpaoResumoAsync()
        {
            Debug.WriteLine($"[Resumo] LoadParametrosGalpaoResumoAsync chamado | LoteAtual={LoteAtual?.numero} | id={LoteAtual?.id}");
            if (LoteAtual?.id == null)
            {
                Debug.WriteLine($"[Resumo] ABORTADO — LoteAtual.id é null");
                return;
            }

            // Só mostra spinner se ainda não tem dados carregados
            if (ParametrosGalpaoResumo.Count == 0)
                IsLoadingGalpaoResumo = true;

            try
            {
                Debug.WriteLine($"[Resumo] ═══ LoadParametrosGalpaoResumoAsync INICIADO (LoteId={LoteAtual.id}) ═══");

                var parametros = await LoteFormAvaliacaoGalpao.ListaParametrosAvalicaoGalpao();
                Debug.WriteLine($"[Resumo] Parâmetros carregados: {parametros?.Count ?? 0}");
                if (parametros == null || parametros.Count == 0)
                {
                    IsAvaliacaoGalpaoSectionVisible = false;
                    return;
                }

                // Query otimizada: agrupa por parametroId e conta formulários únicos
                var formTable = await Db.Table<LoteForm>();
                var forms = await formTable
                    .Where(lf => lf.loteId == (int)LoteAtual.id
                              && lf.parametroTipoId == 20
                              && (lf.excluido == null || lf.excluido != 1))
                    .ToListAsync();

                var formIds = forms.Select(lf => lf.id).ToList();
                Debug.WriteLine($"[Resumo] Formulários encontrados: {formIds.Count}");

                if (formIds.Count == 0)
                {
                    IsAvaliacaoGalpaoSectionVisible = false;
                    return;
                }

                // Carrega respostas somente para os formIds do lote
                var respostasTable = await Db.Table<LoteFormParametro>();
                var todasRespostas = await respostasTable
                    .ToListAsync(); // Carrega tudo e filtra em memória

                var respostasFiltradasPorForm = todasRespostas
                    .Where(lfp => lfp.LoteFormId.HasValue && formIds.Contains((int)lfp.LoteFormId))
                    .ToList();

                Debug.WriteLine($"[Resumo] Total de respostas: {todasRespostas.Count}, Filtradas: {respostasFiltradasPorForm.Count}");

                var resumos = new List<ParametroGalpaoResumo>();
                foreach (var param in parametros)
                {
                    // Conta quantos formulários DIFERENTES têm respostas para este parâmetro
                    var count = respostasFiltradasPorForm
                        .Where(r => r.parametroId == param.id)
                        .Select(r => r.LoteFormId)
                        .Distinct()
                        .Count();

                    resumos.Add(new ParametroGalpaoResumo
                    {
                        Parametro = param,
                        Count = count
                    });

                    Debug.WriteLine($"  [{param.id}] {param.nome} - Count: {count}");
                }

                var resumosOrdenados = resumos.OrderByDescending(r => r.Count).ToList();

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ParametrosGalpaoResumo.Clear();
                    foreach (var resumo in resumosOrdenados)
                    {
                        Debug.WriteLine($"[Resumo UI] {resumo.Nome} - Count: {resumo.Count}");
                        ParametrosGalpaoResumo.Add(resumo);
                    }
                    IsAvaliacaoGalpaoSectionVisible = ParametrosGalpaoResumo.Count > 0;
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoteMonitoramento] Erro ao carregar resumo de galpão: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                IsLoadingGalpaoResumo = false;
            }
        }

        private void AtualizaPodeFecharLote()
        {
            PodeFecharLote = (Permissoes.UsuarioPermissoes?.lotes.fechar ?? false)
                && (LoteAtual != null && LoteAtual.EstaFechado == false);
        }

        private async Task RefreshLoteAtualAsync()
        {
            if (LoteAtual?.id is not int loteId) return;

            try
            {
                var loteAtualizado = await Lote.PegaLoteAsync(loteId, forceRefresh: true);
                if (loteAtualizado == null) return;

                loteAtualizado.EnsureNames(_cacheService);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    LoteAtual = loteAtualizado;
                    AtualizaPodeFecharLote();
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoteMonitoramento] Erro ao recarregar lote após ISIMacro: {ex.Message}");
            }
        }

        ~LoteMonitoramentoViewModel()
        {
            WeakReferenceMessenger.Default.Unregister<ISIMacroScoreMedioAtualizadoMessage>(this);
            WeakReferenceMessenger.Default.Unregister<LoteAlteradoMessage>(this);
            WeakReferenceMessenger.Default.Unregister<FormularioSalvoMessage>(this);

            // Remover listener de permissões para evitar memory leak
            Permissoes.StaticPropertyChanged -= OnPermissoesChanged;
        }
    }
}
