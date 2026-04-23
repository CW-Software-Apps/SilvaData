using SilvaData.Models;

using Microsoft.Maui.Controls;

using System.Diagnostics;

namespace SilvaData.Utilities
{
    // ═══════════════════════════════════════════════════════════════════════════════
    // SEÇÃO 1: NAVEGAÇÃO E INTERFACE (MainPage Tabs)
    // ═══════════════════════════════════════════════════════════════════════════════
    // Mensagens que controlam mudanças de abas na MainPage.
    // Enviadas por: ViewModels que precisam navegar entre telas principais.
    // Recebidas por: MainPageViewModel.
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Solicita mudança para a aba Dashboard.
    /// </summary>
    public class ShowDashboardMessage { }

    /// <summary>
    /// Solicita mudança para a aba Lotes.
    /// </summary>
    public class ShowLotesMessage { }

    /// <summary>
    /// Solicita mudança para a aba Sincronização.
    /// </summary>
    public class ShowSyncMessage { }

    /// <summary>
    /// Solicita mudança para a aba Configurações.
    /// </summary>
    public class ShowSettingsMessage { }

    /// <summary>
    /// Solicita mudança para a aba Suporte.
    /// </summary>
    public class ShowSuporteMessage { }

    // ═══════════════════════════════════════════════════════════════════════════════
    // SEÇÃO 2: ORIENTAÇÃO DE TELA
    // ═══════════════════════════════════════════════════════════════════════════════
    // Controla rotação da tela (Portrait/Landscape).
    // Enviadas por: ViewModels de formulários complexos.
    // Recebidas por: App.xaml.cs ou AppShell.xaml.cs.
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// ★ Força orientação Paisagem (Landscape).
    /// Usada em formulários que precisam de mais espaço horizontal.
    /// </summary>
    public class SetLandscapeModeOnMessage { }

    /// <summary>
    /// ★ Restaura orientação padrão (destravar).
    /// </summary>
    public class SetLandscapeModeOffMessage { }

    // ═══════════════════════════════════════════════════════════════════════════════
    // SEÇÃO 3: CRUD - ENTIDADES PRINCIPAIS (Create/Update)
    // ═══════════════════════════════════════════════════════════════════════════════
    // Mensagens disparadas após operações de criação ou alteração de entidades.
    // Padrão: NomeEntidadeAdicionadaMessage (novo) / NomeEntidadeSalvaMessage (edição).
    // Recebidas por: ViewModels de listagem (para atualizar lista).
    // ═══════════════════════════════════════════════════════════════════════════════

    #region CRUD - Lote

    /// <summary>
    /// ★ Disparado quando um NOVO Lote é criado.
    /// Enviada por: LoteEditViewModel.Salvar().
    /// Recebida por: LoteViewModel (adiciona item na lista).
    /// </summary>
    public class NovoLoteMessage
    {
        public Lote Lote { get; }
        public NovoLoteMessage(Lote lote) => Lote = lote;
    }

    /// <summary>
    /// ★ Disparado quando um Lote EXISTENTE é alterado.
    /// Enviada por: LoteEditViewModel.Salvar().
    /// Recebida por: LoteViewModel (atualiza item na lista).
    /// </summary>
    public class LoteAlteradoMessage
    {
        public Lote Lote { get; }
        public LoteAlteradoMessage(Lote lote) => Lote = lote;
    }

    #endregion

    #region CRUD - Unidade Epidemiológica (UE)

    /// <summary>
    /// ★ Disparado quando uma NOVA Unidade Epidemiológica é criada.
    /// Enviada por: UnidadeEpidemiologicaEditViewModel.Salvar().
    /// Recebida por: UnidadeEpidemiologicaViewModel, LoteEditView (recarrega combo).
    /// </summary>
    public class UEAdicionadaMessage
    {
        public UnidadeEpidemiologica UnidadeEpidemiologica { get; }
        public UEAdicionadaMessage(UnidadeEpidemiologica unidadeEpidemiologica) => UnidadeEpidemiologica = unidadeEpidemiologica;
    }

    /// <summary>
    /// ★ Disparado quando uma UE EXISTENTE é salva.
    /// Enviada por: UnidadeEpidemiologicaEditViewModel.Salvar().
    /// Recebida por: UnidadeEpidemiologicaViewModel (atualiza item).
    /// </summary>
    public class UESalvaMessage
    {
        public UnidadeEpidemiologica UnidadeEpidemiologica { get; }
        public UESalvaMessage(UnidadeEpidemiologica unidadeEpidemiologica) => UnidadeEpidemiologica = unidadeEpidemiologica;
    }

    #endregion

    #region CRUD - Propriedade

    /// <summary>
    /// ★ Disparado quando uma NOVA Propriedade é criada.
    /// Enviada por: PropriedadeEditViewModel.Salvar().
    /// Recebida por: PropriedadeViewModel, UEEditView (recarrega combo).
    /// </summary>
    public class PropriedadeAdicionadaMessage
    {
        public Propriedade Propriedade { get; }
        public PropriedadeAdicionadaMessage(Propriedade propriedade) => Propriedade = propriedade;
    }

    /// <summary>
    /// ★ Disparado quando uma Propriedade EXISTENTE é salva.
    /// Enviada por: PropriedadeEditViewModel.Salvar().
    /// Recebida por: PropriedadeViewModel (atualiza item).
    /// </summary>
    public class PropriedadeSalvaMessage
    {
        public Propriedade Propriedade { get; }
        public PropriedadeSalvaMessage(Propriedade propriedade) => Propriedade = propriedade;
    }

    #endregion

    #region CRUD - Proprietário

    /// <summary>
    /// ★ Disparado quando um NOVO Proprietário é criado.
    /// Enviada por: ProprietarioEditViewModel.Salvar().
    /// Recebida por: ProprietarioViewModel, UEEditView (recarrega combo).
    /// </summary>
    public class ProprietarioAdicionadoMessage
    {
        public Proprietario Proprietario { get; }
        public ProprietarioAdicionadoMessage(Proprietario proprietario) => Proprietario = proprietario;
    }

    /// <summary>
    /// ★ Disparado quando um Proprietário EXISTENTE é salvo.
    /// Enviada por: ProprietarioEditViewModel.Salvar().
    /// Recebida por: ProprietarioViewModel (atualiza item).
    /// </summary>
    public class ProprietarioSalvoMessage
    {
        public Proprietario Proprietario { get; }
        public ProprietarioSalvoMessage(Proprietario proprietario) => Proprietario = proprietario;
    }

    #endregion

    #region CRUD - Regional

    /// <summary>
    /// ★ Disparado quando uma NOVA Regional é criada.
    /// Enviada por: RegionalEditViewModel.Salvar().
    /// Recebida por: RegionalViewModel, PropriedadeEditView (recarrega combo).
    /// </summary>
    public class RegionalAdicionadaMessage
    {
        public Regional Regional { get; }
        public RegionalAdicionadaMessage(Regional regional) => Regional = regional;
    }

    /// <summary>
    /// ★ Disparado quando uma Regional EXISTENTE é salva.
    /// Enviada por: RegionalEditViewModel.Salvar().
    /// Recebida por: RegionalViewModel (atualiza item).
    /// </summary>
    public class RegionalSalvaMessage
    {
        public Regional Regional { get; }
        public RegionalSalvaMessage(Regional regional) => Regional = regional;
    }

    #endregion

    #region CRUD - Atividade

    /// <summary>
    /// ★ Disparado quando uma NOVA Atividade é criada.
    /// Enviada por: AtividadeEditViewModel.Salvar().
    /// Recebida por: AtividadeViewModel (atualiza lista).
    /// </summary>
    public class AtividadeAdicionadaMessage
    {
        public Atividade Atividade { get; }
        public AtividadeAdicionadaMessage(Atividade atividade) => Atividade = atividade;
    }

    /// <summary>
    /// ★ Disparado quando uma Atividade EXISTENTE é salva.
    /// Enviada por: AtividadeEditViewModel.Salvar().
    /// Recebida por: AtividadeViewModel (atualiza item).
    /// </summary>
    public class AtividadeSalvaMessage
    {
        public Atividade Atividade { get; }
        public AtividadeSalvaMessage(Atividade atividade) => Atividade = atividade;
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════════
    // SEÇÃO 4: FORMULÁRIOS E AVALIAÇÕES (LoteForm)
    // ═══════════════════════════════════════════════════════════════════════════════
    // Mensagens relacionadas ao fluxo de preenchimento de formulários de lote.
    // Inclui: ISI Macro, Avaliações do Galpão, Scores, etc.
    // ═══════════════════════════════════════════════════════════════════════════════

    #region Formulários - Configuração e Estado

    /// <summary>
    /// ★★★ Define o estado inicial do formulário (novo ou edição) ★★★
    /// Passa todos os parâmetros necessários para inicializar corretamente.
    /// Enviada por: NavigationUtils.OpenLoteFormularioAsync().
    /// Recebida por: LoteFormularioView (OnNavigatedTo ou via Message).
    /// </summary>
    public class SetFormularioEstadoMessage
    {
        public Lote Lote { get; set; }
        public int LoteFormId { get; set; }
        public int ParametroTipoId { get; set; }
        public int? Fase { get; set; }
        public bool IsReadOnly { get; set; }
        public bool PodeEditar { get; set; }
        public bool DeveLimpar { get; set; }
        public Parametro? ParametroSelecionado { get; set; }

        public SetFormularioEstadoMessage(
            Lote lote,
            int loteFormId,
            int parametroTipoId,
            int? fase,
            bool isReadOnly,
            bool podeEditar,
            bool deveLimpar,
            Parametro? parametroSelecionado = null)
        {
            Lote = lote;
            LoteFormId = loteFormId;
            ParametroTipoId = parametroTipoId;
            Fase = fase;
            IsReadOnly = isReadOnly;
            PodeEditar = podeEditar;
            DeveLimpar = deveLimpar;
            ParametroSelecionado = parametroSelecionado;
        }
    }

    /// <summary>
    /// ★ Sinaliza que LoteFormularioView deve fazer refresh dos dados.
    /// Utilizado após salvar ou quando dados externos mudam.
    /// Enviada por: ViewModels após operações que afetam o formulário.
    /// Recebida por: LoteFormularioView (recarrega dados).
    /// </summary>
    public class RefreshLoteFormularioMessage
    {
        public int LoteFormId { get; }
        public int ParametroTipoId { get; }
        public bool DeveLimpar { get; }

        public RefreshLoteFormularioMessage(int loteFormId, int parametroTipoId, bool deveLimpar = true)
        {
            LoteFormId = loteFormId;
            ParametroTipoId = parametroTipoId;
            DeveLimpar = deveLimpar;
        }
    }

    /// <summary>
    /// ★ Sinaliza que o formulário será fechado e loading deve ser mostrado.
    /// Enviada por: LoteFormularioView.OnDisappearing().
    /// Recebida por: LoadingView ou MainPage (mostra overlay).
    /// </summary>
    public class CloseFormularioMessage
    {
        public bool MostraLoading { get; }
        public CloseFormularioMessage(bool mostraLoading = true) => MostraLoading = mostraLoading;
    }

    /// <summary>
    /// ★ Define qual modelo ISI Macro foi selecionado.
    /// Utilizado para pré-preencher formulário com template específico.
    /// Enviada por: Popup/Modal de seleção de modelo.
    /// Recebida por: LoteFormularioViewModel (carrega template).
    /// </summary>
    public class SetModeloISIMacroMessage
    {
        public int? ModeloId { get; }
        public SetModeloISIMacroMessage(int? modeloId) => ModeloId = modeloId;
    }

    #endregion

    #region Formulários - Score e Avaliações

    /// <summary>
    /// ★★★ Solicita recálculo do score total de um formulário ★★★
    /// Disparado quando parâmetros, alternativas ou valores são alterados.
    /// Enviada por: Controles de entrada, LoteFormAvaliacaoGalpao, ParametroComAlternativas.
    /// Recebida por: LoteFormularioViewModel.UpdateTotal().
    /// </summary>
    public class UpdateScoreMessage { }

    /// <summary>
    /// ★★★ Recalcula totais e média de avaliações do galpão ★★★
    /// Disparado quando uma resposta (quantitativa ou qualitativa) é alterada.
    /// O LoteFormularioViewModel escuta e recalcula:
    /// - Total de avaliações respondidas
    /// - Média dos valores quantitativos
    /// 
    /// Enviada por: LoteFormAvaliacaoGalpao.OnRespostaQtdeChanged().
    /// Recebida por: LoteFormularioViewModel.RecalculaTotaisAvaliacaoGalpao().
    /// </summary>
    public class RecalcularAvaliacaoGalpaoMessage
    {
        public DateTime Timestamp { get; }

        public RecalcularAvaliacaoGalpaoMessage()
        {
            Timestamp = DateTime.Now;
            Debug.WriteLine($"[RecalcularAvaliacaoGalpaoMessage] ★ Enviada às {Timestamp:HH:mm:ss.fff}");
        }
    }

    /// <summary>
    /// ★ Notifica que o score médio (ISI Macro) de um lote foi recalculado.
    /// Dispara atualização da UI com novo score.
    /// Enviada por: Lote.AtualizaISIMacroScoreMedio().
    /// Recebida por: LoteViewModel, DashboardViewModel (atualiza cards/gráficos).
    /// </summary>
    public class ISIMacroScoreMedioAtualizadoMessage
    {
        public int? LoteId { get; }
        public double NovoISIMacroScoreMedio { get; }

        public ISIMacroScoreMedioAtualizadoMessage(int? loteId, double novoISIMacroScoreMedio)
        {
            LoteId = loteId;
            NovoISIMacroScoreMedio = novoISIMacroScoreMedio;
        }
    }

    /// <summary>
    /// ★ Notifica que um ISIMacro foi salvo com sucesso.
    /// Utilizado para atualizar dados do lote após avaliação de necropsia.
    /// Enviada por: ISIMacroViewModel.Salvar().
    /// Recebida por: LoteViewModel (recarrega score do lote).
    /// </summary>
    public class ISIMacroSalvoMessage
    {
        public int? LoteId { get; }
        public ISIMacroSalvoMessage(int? loteId) => LoteId = loteId;
    }

    /// <summary>
    /// ★ Notifica que um LoteForm foi salvo com sucesso.
    /// Dispara recarregamento de dados relacionados.
    /// Enviada por: LoteFormularioViewModel.Salvar().
    /// Recebida por: LoteViewModel, LoteAvaliacaoGalpaoView (recarrega lista).
    /// </summary>
    public class FormularioSalvoMessage
    {
        public LoteForm FormularioSalvo { get; }
        public FormularioSalvoMessage(LoteForm formularioSalvo) => FormularioSalvo = formularioSalvo;
    }

    #endregion

    #region Formulários - Avaliações do Galpão (Específico)

    /// <summary>
    /// ★ Notifica que uma avaliação qualitativa (com foto) foi selecionada.
    /// Passa a avaliação completa para permitir edição.
    /// Enviada por: LoteAvaliacaoGalpaoView (item tapped).
    /// Recebida por: Modal de edição de avaliação qualitativa.
    /// </summary>
    public class SelecionouAvaliacaoQualitativaMessage
    {
        public LoteFormAvaliacaoGalpao Avaliacao { get; }
        public SelecionouAvaliacaoQualitativaMessage(LoteFormAvaliacaoGalpao avaliacao) => Avaliacao = avaliacao;
    }

    /// <summary>
    /// ★ Solicita navegação até um registro específico na lista de avaliações.
    /// Utilizado para avaliação quantitativa.
    /// Enviada por: VerRegistrosPopup (após seleção).
    /// Recebida por: LoteFormularioView (faz scroll até o item).
    /// </summary>
    public class NavigateToRegistroMessage
    {
        public LoteFormAvaliacaoGalpao Registro { get; }
        public NavigateToRegistroMessage(LoteFormAvaliacaoGalpao registro) => Registro = registro;
    }

    #endregion

    #region Formulários - Datas e Mudanças

    /// <summary>
    /// ★ Notifica que a data de um LoteForm foi alterada.
    /// Dispara recálculo de idade do lote.
    /// Enviada por: LoteForm.data (setter).
    /// Recebida por: LoteFormularioView, controles que exibem idade.
    /// </summary>
    public class MudouDataLoteMessage { }

    /// <summary>
    /// ★ Notifica que uma LoteVisita foi alterada.
    /// Dispara recarregamento de formulários relacionados.
    /// Enviada por: LoteVisitaViewModel.Salvar().
    /// Recebida por: LoteViewModel (recarrega formulários da visita).
    /// </summary>
    public class MudouVisitaMessage
    {
        public int? LoteId { get; }
        public MudouVisitaMessage(int? loteId) => LoteId = loteId;
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════════
    // SEÇÃO 5: VALIDAÇÃO E CONTROLE DE FORMULÁRIOS (Base)
    // ═══════════════════════════════════════════════════════════════════════════════
    // Mensagens de validação e controle de fluxo de formulários.
    // Usadas pelo BaseEditViewModel para comunicação com a View.
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// ★ Solicita que a View execute validação dos campos.
    /// A View deve responder com ValidationCompleteMessage.
    /// Enviada por: BaseEditViewModel.SaveAndReturn().
    /// Recebida por: ContentPageEdit (code-behind).
    /// </summary>
    public class ValidateFormRequestMessage
    {
        public Page? TargetPage { get; }

        public ValidateFormRequestMessage(Page? targetPage = null)
        {
            TargetPage = targetPage;
        }
    }

    /// <summary>
    /// ★ Resposta da View com resultado da validação.
    /// Enviada por: ContentPageEdit.OnValidateFormRequest().
    /// Recebida por: BaseEditViewModel.ValidateViewAsync() (aguarda resultado).
    /// </summary>
    public class ValidationCompleteMessage
    {
        public bool IsValid { get; }
        public Page? SourcePage { get; }

        public ValidationCompleteMessage(bool isValid, Page? sourcePage = null)
        {
            IsValid = isValid;
            SourcePage = sourcePage;
        }
    }

    /// <summary>
    /// ★ Solicita que a View feche a página modal.
    /// Enviada por: BaseEditViewModel.SaveAndReturn() após salvar com sucesso.
    /// Recebida por: ContentPageEdit (chama Navigation.PopModalAsync()).
    /// </summary>
    public class ClosePageRequestMessage { }

    /// <summary>
    /// ★ Solicita confirmação de saída quando há dados não salvos (para popup de 3 opções).
    /// Enviada por: BaseEditViewModel.BackNow() quando DataSaved == false.
    /// Recebida por: ContentPageEdit (mostra PopUpThreeOptions).
    /// </summary>
    public class ConfirmExitRequestMessage { }

    /// <summary>
    /// ★ Ações possíveis ao sair de uma tela com dados não salvos.
    /// Usado pelo PopUpThreeOptions para determinar a ação do usuário.
    /// </summary>
    public enum ExitAction
    {
        /// <summary>Salva as alterações e fecha a página</summary>
        Save,
        /// <summary>Descarta as alterações e fecha a página</summary>
        Discard,
        /// <summary>Cancela a ação de sair e permanece na página</summary>
        Cancel
    }

    /// <summary>
    /// ★ Solicita confirmação de saída quando há dados não salvos (versão com 3 opções).
    /// Enviada por: BaseEditViewModel.BackNow() quando DataSaved == false.
    /// Recebida por: ContentPageEdit (mostra PopUpThreeOptions).
    /// </summary>
    public class ConfirmExitWithOptionsRequestMessage
    {
        public TaskCompletionSource<ExitAction> Result { get; }

        public ConfirmExitWithOptionsRequestMessage()
        {
            Result = new TaskCompletionSource<ExitAction>();
        }
    }

    /// <summary>
    /// ★ Notifica que o usuário escolheu salvar e fechar.
    /// Enviada por: ContentPageEdit após confirmação no PopUpThreeOptions.
    /// Recebida por: BaseEditViewModel (dispara SaveAndReturn).
    /// </summary>
    public class SaveAndCloseMessage { }

    /// <summary>
    /// ★ Notifica que o usuário escolheu descartar e fechar.
    /// Enviada por: ContentPageEdit após confirmação no PopUpThreeOptions.
    /// Recebida por: BaseEditViewModel (fecha página sem salvar).
    /// </summary>
    public class DiscardAndCloseMessage { }

    /// <summary>
    /// ★ Notifica que o usuário cancelou a ação de sair.
    /// Enviada por: ContentPageEdit quando usuário clica Cancelar no PopUpThreeOptions.
    /// </summary>
    public class CancelExitMessage { }

    /// <summary>
    /// ★ Sinal global: destaca campos obrigatórios vazios em vermelho.
    /// Enviada por: ViewModel ao clicar Salvar com campos obrigatórios vazios.
    /// Recebida por: Controles customizados (Entry, ComboBox) que implementam validação visual.
    /// </summary>
    public class HighlightRequiredFieldsMessage
    {
        public Page? TargetPage { get; }

        public HighlightRequiredFieldsMessage(Page? targetPage = null)
        {
            TargetPage = targetPage;
        }
    }

    /// <summary>
    /// Solicita que todos os controles obrigatórios limpem seu estado de erro visual.
    /// Enviada por: ContentPageEdit.OnAppearing ao reabrir a página.
    /// Recebida por: Controles customizados (ISITextField, ComboBox, etc.) que mostram erro visual.
    /// </summary>
    public class ClearValidationErrorsMessage
    {
        public Page? TargetPage { get; }

        public ClearValidationErrorsMessage(Page? targetPage = null)
        {
            TargetPage = targetPage;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // SEÇÃO 6: NAVEGAÇÃO E FOCO
    // ═══════════════════════════════════════════════════════════════════════════════
    // Mensagens que controlam foco e navegação entre campos.
    // ⚠️ ACOPLAMENTO: Algumas mensagens passam objetos View (não ideal).
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// ★ Solicita que o foco mova para o próximo campo.
    /// ⚠️ ACOPLAMENTO: Passa View diretamente (não ideal, melhor usar code-behind).
    /// Enviada por: Entry ao pressionar Enter.
    /// Recebida por: View code-behind (move foco programaticamente).
    /// </summary>
    public class VaiProProximoMessage
    {
        public View View { get; }
        public VaiProProximoMessage(View view) => View = view;
    }

    /// <summary>
    /// ★ Solicita abertura de modal de seleção de foto para ISI Macro.
    /// Passa o parâmetro que precisa de foto.
    /// Enviada por: ISIMacroNota control (botão de foto).
    /// Recebida por: LoteFormularioView (abre modal de câmera/galeria).
    /// </summary>
    public class ISIMacroFotoRequestedMessage
    {
        public string Nome { get; }
        public ParametroComAlternativas Parametro { get; }

        public ISIMacroFotoRequestedMessage(string nome, ParametroComAlternativas parametro)
        {
            Nome = nome;
            Parametro = parametro;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // SEÇÃO 7: CACHE E SINCRONIZAÇÃO
    // ═══════════════════════════════════════════════════════════════════════════════
    // Mensagens relacionadas ao gerenciamento de cache e sincronização de dados.
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// ★ Enumeração dos tipos de cache disponíveis.
    /// Utilizado para controlar qual seção do cache será recarregada.
    /// </summary>
    public enum CacheType
    {
        /// <summary>Cache de Unidades Epidemiológicas</summary>
        UnidadesEpidemiologicas,
        /// <summary>Cache de Propriedades</summary>
        Propriedades,
        /// <summary>Cache de Proprietários</summary>
        Proprietarios,
        /// <summary>Cache de Regionais</summary>
        Regionais,
        /// <summary>Atualiza TODO o cache</summary>
        All
    }

    /// <summary>
    /// ★ Solicita recarga de um setor específico do cache.
    /// Utilizado após operações de CRUD para sincronizar dados em memória.
    /// Enviada por: ViewModels após criar/editar/deletar entidades.
    /// Recebida por: CacheService (recarrega dados do banco).
    /// </summary>
    public class RefreshCacheMessage
    {
        public CacheType Type { get; }

        public RefreshCacheMessage(CacheType type = CacheType.All)
        {
            Type = type;
        }
    }

    /// <summary>
    /// ★ Notifica que sincronização (Download) completa foi finalizada.
    /// Todos os controles devem recarregar seus dados do CacheService.
    /// Utilizado por ComboBoxes e listas que dependem de dados baixados.
    /// Enviada por: SincronizacaoViewModel.BaixarDados() (após sucesso).
    /// Recebida por: Múltiplos ViewModels (recarregam combos e listas).
    /// </summary>
    public class UpdateDadosIniciaisMessage { }

    // ═══════════════════════════════════════════════════════════════════════════════
    // SEÇÃO 8: DASHBOARD E GRÁFICOS
    // ═══════════════════════════════════════════════════════════════════════════════
    // Mensagens relacionadas à Dashboard e visualização de gráficos.
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Nível de detalhamento do gráfico exibido (drilldown) dentro de "ISI Score Total".
    /// Foi renomeado de TipoGrafico para evitar conflito com DashboardTipoGrafico.
    /// </summary>
    public enum GraficoNivel
    {
        /// <summary>Gráfico de SuperCategoria (agrupamento maior)</summary>
        SuperCategoria,
        /// <summary>Gráfico de Categoria (nível intermediário)</summary>
        Categoria,
        /// <summary>Gráfico de Parâmetro (mais detalhado)</summary>
        Parametro,
        /// <summary>Gráfico de Dispersão (scatter plot, fora do drilldown)</summary>
        Dispersao
    }

    /// <summary>
    /// Tipo de gráfico principal da Dashboard (abas superiores), controla qual conjunto de visualizações mostrar.
    /// </summary>
    public enum DashboardTipoGrafico
    {
        /// <summary>Conjunto ISI Score Total (SuperCategoria → Categoria → Parâmetro)</summary>
        ISIScoreTotal,
        /// <summary>Conjunto Acometimento (séries de linhas por SuperCategoria)</summary>
        Acometimento,
        /// <summary>Conjunto Dispersão (Scatter plot por dia)</summary>
        ISIDispersaoScore
    }

    /// <summary>
    /// ★ Solicita mudança para aba de gráficos e exibe gráfico específico.
    /// Enviada por: Botões/Cards em home que querem mostrar análise visual.
    /// Recebida por: DashboardViewModel (muda aba e renderiza gráfico).
    /// </summary>
    public class ShowGraficoMessage
    {
        public DashboardTipoGrafico TipoGrafico { get; }

        public ShowGraficoMessage(DashboardTipoGrafico tipo)
        {
            TipoGrafico = tipo;
        }
    }

    /// <summary>
    /// Notifica mudança no total de alterações pendentes de sincronização.
    /// Enviada por: SincronizacaoPendentesViewModel (após buscar/alterar a lista).
    /// Recebida por: MainPageModel (para exibir badge/contador na aba de Sync).
    ///
    /// Exemplo de envio:
    /// WeakReferenceMessenger.Default.Send(new SyncPendentesTotalChangedMessage(total));
    ///
    /// Exemplo de registro:
    /// WeakReferenceMessenger.Default.Register<SyncPendentesTotalChangedMessage>(this, (r, m) =>
    /// {
    ///     // Se for atualizar UI, use o dispatcher:
    ///     _dispatcher.Dispatch(() => SyncPendingCount = m.Total);
    /// });
    /// </summary>
    public class SyncPendentesTotalChangedMessage
    {
        /// <summary>
        /// Quantidade total de mudanças pendentes para sincronizar.
        /// </summary>
        public int Total { get; }

        /// <summary>
        /// Cria a mensagem com o total de pendências.
        /// </summary>
        /// <param name="total">Número de registros pendentes (>= 0).</param>
        public SyncPendentesTotalChangedMessage(int total)
        {
            Total = total;
        }
    }


    /// <summary>
    /// ★ Solicita atualização completa dos dados da Dashboard.
    /// Dispara recarregamento de gráficos, cards e estatísticas.
    /// Enviada por: HomeViewModel quando dados ficam obsoletos.
    /// Recebida por: DashboardViewModel (dispara carregamento).
    /// </summary>
    public class RequestDashboardRefreshMessage { }

    // ═══════════════════════════════════════════════════════════════════════════════
    // SEÇÃO 9: AUTENTICAÇÃO E SESSÃO
    // ═══════════════════════════════════════════════════════════════════════════════
    // Mensagens relacionadas ao fluxo de login/logout.
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// ★ Notifica que o usuário fez logout com sucesso.
    /// O AppShell deve limpar navegação e retornar ao Login.
    /// Enviada por: MinhaContaViewModel.LogOff().
    /// Recebida por: AppShell (fecha sessão e volta ao LoginPage).
    /// </summary>
    public class LogoutSuccessMessage { }

    /// <summary>
    /// ★ Notifica que o usuário fez login com sucesso.
    /// Enviada por: LoginViewModel.LoginNowAsync().
    /// Recebida por: ViewModels que precisam atualizar dados do usuário logado.
    /// </summary>
    public class LoginSuccessMessage { }

    // ═══════════════════════════════════════════════════════════════════════════════
    // SEÇÃO 10: MENSAGENS GENÉRICAS E UTILITÁRIAS
    // ═══════════════════════════════════════════════════════════════════════════════
    // Mensagens de propósito geral que não se encaixam em categorias específicas.
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// ★ Mensagem genérica para notificar mudança em qualquer propriedade.
    /// Utilizada para rastrear alterações e disparar ações reativas.
    /// Enviada por: Qualquer ViewModel/Model quando uma propriedade muda.
    /// Recebida por: Listeners interessados em rastrear mudanças específicas.
    /// 
    /// Exemplo de uso:
    /// <code>
    /// WeakReferenceMessenger.Default.Send(
    ///     new PropriedadeMudouMessage("RespostaQtde", 10, 25));
    /// </code>
    /// </summary>
    public class PropriedadeMudouMessage
    {
        public string PropriedadeNome { get; }
        public object? ValorAntigo { get; }
        public object? ValorNovo { get; }

        public PropriedadeMudouMessage(string propriedadeNome, object? valorAntigo, object? valorNovo)
        {
            PropriedadeNome = propriedadeNome;
            ValorAntigo = valorAntigo;
            ValorNovo = valorNovo;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // DOCUMENTAÇÃO DE PADRÕES DE USO
    // ═══════════════════════════════════════════════════════════════════════════════
    /*
     * PADRÃO DE ENVIO:
     * ----------------
     * WeakReferenceMessenger.Default.Send(new NomeDaMensagem(parametros));
     * 
     * PADRÃO DE RECEBIMENTO:
     * ----------------------
     * // No construtor ou OnAppearing:
     * WeakReferenceMessenger.Default.Register<NomeDaMensagem>(this, (recipient, message) =>
     * {
     *     // Lógica de tratamento
     * });
     * 
     * // No OnDisappearing ou Cleanup:
     * WeakReferenceMessenger.Default.Unregister<NomeDaMensagem>(this);
     * 
     * BOAS PRÁTICAS:
     * --------------
     * 1. ✅ SEMPRE Unregister no OnDisappearing/Cleanup (evita memory leak)
     * 2. ✅ Use WeakReferenceMessenger (não mantém referências fortes)
     * 3. ✅ Prefira mensagens específicas a genéricas (ex: LoteAlteradoMessage vs PropriedadeMudouMessage)
     * 4. ✅ Documente QUEM envia e QUEM recebe
     * 5. ⚠️ Evite passar objetos View em mensagens (acoplamento)
     * 6. ✅ Use try-catch nos handlers (previne crashes)
     * 
     * EXEMPLO COMPLETO:
     * -----------------
     * // Envio (no ViewModel após salvar):
     * WeakReferenceMessenger.Default.Send(new LoteAlteradoMessage(lote));
     * 
     * // Recebimento (no LoteViewModel):
     * protected override void OnAppearing()
     * {
     *     WeakReferenceMessenger.Default.Register<LoteAlteradoMessage>(this, (r, m) =>
     *     {
     *         try 
     *         {
     *             var loteAtualizado = Lotes.FirstOrDefault(l => l.id == m.Lote.id);
     *             if (loteAtualizado != null) 
     *             {
     *                 // Atualiza propriedades
     *             }
     *         }
     *         catch (Exception ex) 
     *         {
     *             Debug.WriteLine($"Erro: {ex.Message}");
     *         }
     *     });
     * }
     * 
     * protected override void OnDisappearing()
     * {
     *     WeakReferenceMessenger.Default.Unregister<LoteAlteradoMessage>(this);
     * }
     */
}
