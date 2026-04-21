using SilvaData.Models;

using Microsoft.Maui.Controls;

using System.Diagnostics;

namespace SilvaData.Utilities
{
    // -------------------------------------------------------------------------------
    // SE��O 1: NAVEGA��O E INTERFACE (MainPage Tabs)
    // -------------------------------------------------------------------------------
    // Mensagens que controlam mudan�as de abas na MainPage.
    // Enviadas por: ViewModels que precisam navegar entre telas principais.
    // Recebidas por: MainPageViewModel.
    // -------------------------------------------------------------------------------

    /// <summary>
    /// Solicita mudan�a para a aba Dashboard.
    /// </summary>
    public class ShowDashboardMessage { }

    /// <summary>
    /// Solicita mudan�a para a aba Lotes.
    /// </summary>
    public class ShowLotesMessage { }

    /// <summary>
    /// Solicita mudan�a para a aba Sincroniza��o.
    /// </summary>
    public class ShowSyncMessage { }

    /// <summary>
    /// Solicita mudan�a para a aba Configura��es.
    /// </summary>
    public class ShowSettingsMessage { }

    /// <summary>
    /// Solicita mudan�a para a aba Suporte.
    /// </summary>
    public class ShowSuporteMessage { }

    // -------------------------------------------------------------------------------
    // SE��O 2: ORIENTA��O DE TELA
    // -------------------------------------------------------------------------------
    // Controla rota��o da tela (Portrait/Landscape).
    // Enviadas por: ViewModels de formul�rios complexos.
    // Recebidas por: App.xaml.cs ou AppShell.xaml.cs.
    // -------------------------------------------------------------------------------

    /// <summary>
    /// ? For�a orienta��o Paisagem (Landscape).
    /// Usada em formul�rios que precisam de mais espa�o horizontal.
    /// </summary>
    public class SetLandscapeModeOnMessage { }

    /// <summary>
    /// ? Restaura orienta��o padr�o (destravar).
    /// </summary>
    public class SetLandscapeModeOffMessage { }

    // -------------------------------------------------------------------------------
    // SE��O 3: CRUD - ENTIDADES PRINCIPAIS (Create/Update)
    // -------------------------------------------------------------------------------
    // Mensagens disparadas ap�s opera��es de cria��o ou altera��o de entidades.
    // Padr�o: NomeEntidadeAdicionadaMessage (novo) / NomeEntidadeSalvaMessage (edi��o).
    // Recebidas por: ViewModels de listagem (para atualizar lista).
    // -------------------------------------------------------------------------------

    #region CRUD - Lote

    /// <summary>
    /// ? Disparado quando um NOVO Lote � criado.
    /// Enviada por: LoteEditViewModel.Salvar().
    /// Recebida por: LoteViewModel (adiciona item na lista).
    /// </summary>
    public class NovoLoteMessage
    {
        public Lote Lote { get; }
        public NovoLoteMessage(Lote lote) => Lote = lote;
    }

    /// <summary>
    /// ? Disparado quando um Lote EXISTENTE � alterado.
    /// Enviada por: LoteEditViewModel.Salvar().
    /// Recebida por: LoteViewModel (atualiza item na lista).
    /// </summary>
    public class LoteAlteradoMessage
    {
        public Lote Lote { get; }
        public LoteAlteradoMessage(Lote lote) => Lote = lote;
    }

    #endregion

    #region CRUD - Unidade Epidemiol�gica (UE)

    /// <summary>
    /// ? Disparado quando uma NOVA Unidade Epidemiol�gica � criada.
    /// Enviada por: UnidadeEpidemiologicaEditViewModel.Salvar().
    /// Recebida por: UnidadeEpidemiologicaViewModel, LoteEditView (recarrega combo).
    /// </summary>
    public class UEAdicionadaMessage
    {
        public UnidadeEpidemiologica UnidadeEpidemiologica { get; }
        public UEAdicionadaMessage(UnidadeEpidemiologica unidadeEpidemiologica) => UnidadeEpidemiologica = unidadeEpidemiologica;
    }

    /// <summary>
    /// ? Disparado quando uma UE EXISTENTE � salva.
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
    /// ? Disparado quando uma NOVA Propriedade � criada.
    /// Enviada por: PropriedadeEditViewModel.Salvar().
    /// Recebida por: PropriedadeViewModel, UEEditView (recarrega combo).
    /// </summary>
    public class PropriedadeAdicionadaMessage
    {
        public Propriedade Propriedade { get; }
        public PropriedadeAdicionadaMessage(Propriedade propriedade) => Propriedade = propriedade;
    }

    /// <summary>
    /// ? Disparado quando uma Propriedade EXISTENTE � salva.
    /// Enviada por: PropriedadeEditViewModel.Salvar().
    /// Recebida por: PropriedadeViewModel (atualiza item).
    /// </summary>
    public class PropriedadeSalvaMessage
    {
        public Propriedade Propriedade { get; }
        public PropriedadeSalvaMessage(Propriedade propriedade) => Propriedade = propriedade;
    }

    #endregion

    #region CRUD - Propriet�rio

    /// <summary>
    /// ? Disparado quando um NOVO Propriet�rio � criado.
    /// Enviada por: ProprietarioEditViewModel.Salvar().
    /// Recebida por: ProprietarioViewModel, UEEditView (recarrega combo).
    /// </summary>
    public class ProprietarioAdicionadoMessage
    {
        public Proprietario Proprietario { get; }
        public ProprietarioAdicionadoMessage(Proprietario proprietario) => Proprietario = proprietario;
    }

    /// <summary>
    /// ? Disparado quando um Propriet�rio EXISTENTE � salvo.
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
    /// ? Disparado quando uma NOVA Regional � criada.
    /// Enviada por: RegionalEditViewModel.Salvar().
    /// Recebida por: RegionalViewModel, PropriedadeEditView (recarrega combo).
    /// </summary>
    public class RegionalAdicionadaMessage
    {
        public Regional Regional { get; }
        public RegionalAdicionadaMessage(Regional regional) => Regional = regional;
    }

    /// <summary>
    /// ? Disparado quando uma Regional EXISTENTE � salva.
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
    /// ? Disparado quando uma NOVA Atividade � criada.
    /// Enviada por: AtividadeEditViewModel.Salvar().
    /// Recebida por: AtividadeViewModel (atualiza lista).
    /// </summary>
    public class AtividadeAdicionadaMessage
    {
        public Atividade Atividade { get; }
        public AtividadeAdicionadaMessage(Atividade atividade) => Atividade = atividade;
    }

    /// <summary>
    /// ? Disparado quando uma Atividade EXISTENTE � salva.
    /// Enviada por: AtividadeEditViewModel.Salvar().
    /// Recebida por: AtividadeViewModel (atualiza item).
    /// </summary>
    public class AtividadeSalvaMessage
    {
        public Atividade Atividade { get; }
        public AtividadeSalvaMessage(Atividade atividade) => Atividade = atividade;
    }

    #endregion

    // -------------------------------------------------------------------------------
    // SE��O 4: FORMUL�RIOS E AVALIA��ES (LoteForm)
    // -------------------------------------------------------------------------------
    // Mensagens relacionadas ao fluxo de preenchimento de formul�rios de lote.
    // Inclui: ISI Macro, Avalia��es do Galp�o, Scores, etc.
    // -------------------------------------------------------------------------------

    #region Formul�rios - Configura��o e Estado

    /// <summary>
    /// ??? Define o estado inicial do formul�rio (novo ou edi��o) ???
    /// Passa todos os par�metros necess�rios para inicializar corretamente.
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
    /// ? Sinaliza que LoteFormularioView deve fazer refresh dos dados.
    /// Utilizado ap�s salvar ou quando dados externos mudam.
    /// Enviada por: ViewModels ap�s opera��es que afetam o formul�rio.
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
    /// ? Sinaliza que o formul�rio ser� fechado e loading deve ser mostrado.
    /// Enviada por: LoteFormularioView.OnDisappearing().
    /// Recebida por: LoadingView ou MainPage (mostra overlay).
    /// </summary>
    public class CloseFormularioMessage
    {
        public bool MostraLoading { get; }
        public CloseFormularioMessage(bool mostraLoading = true) => MostraLoading = mostraLoading;
    }

    /// <summary>
    /// ? Define qual modelo ISI Macro foi selecionado.
    /// Utilizado para pr�-preencher formul�rio com template espec�fico.
    /// Enviada por: Popup/Modal de sele��o de modelo.
    /// Recebida por: LoteFormularioViewModel (carrega template).
    /// </summary>
    public class SetModeloISIMacroMessage
    {
        public int? ModeloId { get; }
        public SetModeloISIMacroMessage(int? modeloId) => ModeloId = modeloId;
    }

    #endregion

    #region Formul�rios - Score e Avalia��es

    /// <summary>
    /// ??? Solicita rec�lculo do score total de um formul�rio ???
    /// Disparado quando par�metros, alternativas ou valores s�o alterados.
    /// Enviada por: Controles de entrada, LoteFormAvaliacaoGalpao, ParametroComAlternativas.
    /// Recebida por: LoteFormularioViewModel.UpdateTotal().
    /// </summary>
    public class UpdateScoreMessage { }

    /// <summary>
    /// ??? Recalcula totais e m�dia de avalia��es do galp�o ???
    /// Disparado quando uma resposta (quantitativa ou qualitativa) � alterada.
    /// O LoteFormularioViewModel escuta e recalcula:
    /// - Total de avalia��es respondidas
    /// - M�dia dos valores quantitativos
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
            Debug.WriteLine($"[RecalcularAvaliacaoGalpaoMessage] ? Enviada �s {Timestamp:HH:mm:ss.fff}");
        }
    }

    /// <summary>
    /// ? Notifica que o score m�dio (ISI Macro) de um lote foi recalculado.
    /// Dispara atualiza��o da UI com novo score.
    /// Enviada por: Lote.AtualizaISIMacroScoreMedio().
    /// Recebida por: LoteViewModel, DashboardViewModel (atualiza cards/gr�ficos).
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
    /// ? Notifica que um ISIMacro foi salvo com sucesso.
    /// Utilizado para atualizar dados do lote ap�s avalia��o de necropsia.
    /// Enviada por: ISIMacroViewModel.Salvar().
    /// Recebida por: LoteViewModel (recarrega score do lote).
    /// </summary>
    public class ISIMacroSalvoMessage
    {
        public int? LoteId { get; }
        public ISIMacroSalvoMessage(int? loteId) => LoteId = loteId;
    }

    /// <summary>
    /// ? Notifica que um LoteForm foi salvo com sucesso.
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

    #region Formul�rios - Avalia��es do Galp�o (Espec�fico)

    /// <summary>
    /// ? Notifica que uma avalia��o qualitativa (com foto) foi selecionada.
    /// Passa a avalia��o completa para permitir edi��o.
    /// Enviada por: LoteAvaliacaoGalpaoView (item tapped).
    /// Recebida por: Modal de edi��o de avalia��o qualitativa.
    /// </summary>
    public class SelecionouAvaliacaoQualitativaMessage
    {
        public LoteFormAvaliacaoGalpao Avaliacao { get; }
        public SelecionouAvaliacaoQualitativaMessage(LoteFormAvaliacaoGalpao avaliacao) => Avaliacao = avaliacao;
    }

    /// <summary>
    /// ? Solicita navega��o at� um registro espec�fico na lista de avalia��es.
    /// Utilizado para avalia��o quantitativa.
    /// Enviada por: VerRegistrosPopup (ap�s sele��o).
    /// Recebida por: LoteFormularioView (faz scroll at� o item).
    /// </summary>
    public class NavigateToRegistroMessage
    {
        public LoteFormAvaliacaoGalpao Registro { get; }
        public NavigateToRegistroMessage(LoteFormAvaliacaoGalpao registro) => Registro = registro;
    }

    #endregion

    #region Formul�rios - Datas e Mudan�as

    /// <summary>
    /// ? Notifica que a data de um LoteForm foi alterada.
    /// Dispara rec�lculo de idade do lote.
    /// Enviada por: LoteForm.data (setter).
    /// Recebida por: LoteFormularioView, controles que exibem idade.
    /// </summary>
    public class MudouDataLoteMessage { }

    /// <summary>
    /// ? Notifica que uma LoteVisita foi alterada.
    /// Dispara recarregamento de formul�rios relacionados.
    /// Enviada por: LoteVisitaViewModel.Salvar().
    /// Recebida por: LoteViewModel (recarrega formul�rios da visita).
    /// </summary>
    public class MudouVisitaMessage
    {
        public int? LoteId { get; }
        public MudouVisitaMessage(int? loteId) => LoteId = loteId;
    }

    #endregion

    // -------------------------------------------------------------------------------
    // SE��O 5: VALIDA��O E CONTROLE DE FORMUL�RIOS (Base)
    // -------------------------------------------------------------------------------
    // Mensagens de valida��o e controle de fluxo de formul�rios.
    // Usadas pelo BaseEditViewModel para comunica��o com a View.
    // -------------------------------------------------------------------------------

    /// <summary>
    /// ? Solicita que a View execute valida��o dos campos.
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
    /// ? Resposta da View com resultado da valida��o.
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
    /// ? Solicita que a View feche a p�gina modal.
    /// Enviada por: BaseEditViewModel.SaveAndReturn() ap�s salvar com sucesso.
    /// Recebida por: ContentPageEdit (chama Navigation.PopModalAsync()).
    /// </summary>
    public class ClosePageRequestMessage { }

    /// <summary>
    /// ? Solicita confirma��o de sa�da quando h� dados n�o salvos (para popup de 3 op��es).
    /// Enviada por: BaseEditViewModel.BackNow() quando DataSaved == false.
    /// Recebida por: ContentPageEdit (mostra PopUpThreeOptions).
    /// </summary>
    public class ConfirmExitRequestMessage { }

    /// <summary>
    /// ? A��es poss�veis ao sair de uma tela com dados n�o salvos.
    /// Usado pelo PopUpThreeOptions para determinar a a��o do usu�rio.
    /// </summary>
    public enum ExitAction
    {
        /// <summary>Salva as altera��es e fecha a p�gina</summary>
        Save,
        /// <summary>Descarta as altera��es e fecha a p�gina</summary>
        Discard,
        /// <summary>Cancela a a��o de sair e permanece na p�gina</summary>
        Cancel
    }

    /// <summary>
    /// ? Solicita confirma��o de sa�da quando h� dados n�o salvos (vers�o com 3 op��es).
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
    /// ? Notifica que o usu�rio escolheu salvar e fechar.
    /// Enviada por: ContentPageEdit ap�s confirma��o no PopUpThreeOptions.
    /// Recebida por: BaseEditViewModel (dispara SaveAndReturn).
    /// </summary>
    public class SaveAndCloseMessage { }

    /// <summary>
    /// ? Notifica que o usu�rio escolheu descartar e fechar.
    /// Enviada por: ContentPageEdit ap�s confirma��o no PopUpThreeOptions.
    /// Recebida por: BaseEditViewModel (fecha p�gina sem salvar).
    /// </summary>
    public class DiscardAndCloseMessage { }

    /// <summary>
    /// ? Notifica que o usu�rio cancelou a a��o de sair.
    /// Enviada por: ContentPageEdit quando usu�rio clica Cancelar no PopUpThreeOptions.
    /// </summary>
    public class CancelExitMessage { }

    /// <summary>
    /// ? Sinal global: destaca campos obrigat�rios vazios em vermelho.
    /// Enviada por: ViewModel ao clicar Salvar com campos obrigat�rios vazios.
    /// Recebida por: Controles customizados (Entry, ComboBox) que implementam valida��o visual.
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
    /// Solicita que todos os controles obrigat�rios limpem seu estado de erro visual.
    /// Enviada por: ContentPageEdit.OnAppearing ao reabrir a p�gina.
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

    // -------------------------------------------------------------------------------
    // SE��O 6: NAVEGA��O E FOCO
    // -------------------------------------------------------------------------------
    // Mensagens que controlam foco e navega��o entre campos.
    // ?? ACOPLAMENTO: Algumas mensagens passam objetos View (n�o ideal).
    // -------------------------------------------------------------------------------

    /// <summary>
    /// ? Solicita que o foco mova para o pr�ximo campo.
    /// ?? ACOPLAMENTO: Passa View diretamente (n�o ideal, melhor usar code-behind).
    /// Enviada por: Entry ao pressionar Enter.
    /// Recebida por: View code-behind (move foco programaticamente).
    /// </summary>
    public class VaiProProximoMessage
    {
        public View View { get; }
        public VaiProProximoMessage(View view) => View = view;
    }

    /// <summary>
    /// ? Solicita abertura de modal de sele��o de foto para ISI Macro.
    /// Passa o par�metro que precisa de foto.
    /// Enviada por: ISIMacroNota control (bot�o de foto).
    /// Recebida por: LoteFormularioView (abre modal de c�mera/galeria).
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

    // -------------------------------------------------------------------------------
    // SE��O 7: CACHE E SINCRONIZA��O
    // -------------------------------------------------------------------------------
    // Mensagens relacionadas ao gerenciamento de cache e sincroniza��o de dados.
    // -------------------------------------------------------------------------------

    /// <summary>
    /// ? Enumera��o dos tipos de cache dispon�veis.
    /// Utilizado para controlar qual se��o do cache ser� recarregada.
    /// </summary>
    public enum CacheType
    {
        /// <summary>Cache de Unidades Epidemiol�gicas</summary>
        UnidadesEpidemiologicas,
        /// <summary>Cache de Propriedades</summary>
        Propriedades,
        /// <summary>Cache de Propriet�rios</summary>
        Proprietarios,
        /// <summary>Cache de Regionais</summary>
        Regionais,
        /// <summary>Atualiza TODO o cache</summary>
        All
    }

    /// <summary>
    /// ? Solicita recarga de um setor espec�fico do cache.
    /// Utilizado ap�s opera��es de CRUD para sincronizar dados em mem�ria.
    /// Enviada por: ViewModels ap�s criar/editar/deletar entidades.
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
    /// ? Notifica que sincroniza��o (Download) completa foi finalizada.
    /// Todos os controles devem recarregar seus dados do CacheService.
    /// Utilizado por ComboBoxes e listas que dependem de dados baixados.
    /// Enviada por: SincronizacaoViewModel.BaixarDados() (ap�s sucesso).
    /// Recebida por: M�ltiplos ViewModels (recarregam combos e listas).
    /// </summary>
    public class UpdateDadosIniciaisMessage { }

    // -------------------------------------------------------------------------------
    // SE��O 8: DASHBOARD E GR�FICOS
    // -------------------------------------------------------------------------------
    // Mensagens relacionadas � Dashboard e visualiza��o de gr�ficos.
    // -------------------------------------------------------------------------------

    /// <summary>
    /// N�vel de detalhamento do gr�fico exibido (drilldown) dentro de "ISI Score Total".
    /// Foi renomeado de TipoGrafico para evitar conflito com DashboardTipoGrafico.
    /// </summary>
    public enum GraficoNivel
    {
        /// <summary>Gr�fico de SuperCategoria (agrupamento maior)</summary>
        SuperCategoria,
        /// <summary>Gr�fico de Categoria (n�vel intermedi�rio)</summary>
        Categoria,
        /// <summary>Gr�fico de Par�metro (mais detalhado)</summary>
        Parametro,
        /// <summary>Gr�fico de Dispers�o (scatter plot, fora do drilldown)</summary>
        Dispersao
    }

    /// <summary>
    /// Tipo de gr�fico principal da Dashboard (abas superiores), controla qual conjunto de visualiza��es mostrar.
    /// </summary>
    public enum DashboardTipoGrafico
    {
        /// <summary>Conjunto ISI Score Total (SuperCategoria ? Categoria ? Par�metro)</summary>
        ISIScoreTotal,
        /// <summary>Conjunto Acometimento (s�ries de linhas por SuperCategoria)</summary>
        Acometimento,
        /// <summary>Conjunto Dispers�o (Scatter plot por dia)</summary>
        ISIDispersaoScore
    }

    /// <summary>
    /// ? Solicita mudan�a para aba de gr�ficos e exibe gr�fico espec�fico.
    /// Enviada por: Bot�es/Cards em home que querem mostrar an�lise visual.
    /// Recebida por: DashboardViewModel (muda aba e renderiza gr�fico).
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
    /// Notifica mudan�a no total de altera��es pendentes de sincroniza��o.
    /// Enviada por: SincronizacaoPendentesViewModel (ap�s buscar/alterar a lista).
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
        /// Quantidade total de mudan�as pendentes para sincronizar.
        /// </summary>
        public int Total { get; }

        /// <summary>
        /// Cria a mensagem com o total de pend�ncias.
        /// </summary>
        /// <param name="total">N�mero de registros pendentes (>= 0).</param>
        public SyncPendentesTotalChangedMessage(int total)
        {
            Total = total;
        }
    }


    /// <summary>
    /// ? Solicita atualiza��o completa dos dados da Dashboard.
    /// Dispara recarregamento de gr�ficos, cards e estat�sticas.
    /// Enviada por: HomeViewModel quando dados ficam obsoletos.
    /// Recebida por: DashboardViewModel (dispara carregamento).
    /// </summary>
    public class RequestDashboardRefreshMessage { }

    // -------------------------------------------------------------------------------
    // SE��O 9: AUTENTICA��O E SESS�O
    // -------------------------------------------------------------------------------
    // Mensagens relacionadas ao fluxo de login/logout.
    // -------------------------------------------------------------------------------

    /// <summary>
    /// ? Notifica que o usu�rio fez logout com sucesso.
    /// O AppShell deve limpar navega��o e retornar ao Login.
    /// Enviada por: MinhaContaViewModel.LogOff().
    /// Recebida por: AppShell (fecha sess�o e volta ao LoginPage).
    /// </summary>
    public class LogoutSuccessMessage { }

    // -------------------------------------------------------------------------------
    // SE��O 10: MENSAGENS GEN�RICAS E UTILIT�RIAS
    // -------------------------------------------------------------------------------
    // Mensagens de prop�sito geral que n�o se encaixam em categorias espec�ficas.
    // -------------------------------------------------------------------------------

    /// <summary>
    /// ? Mensagem gen�rica para notificar mudan�a em qualquer propriedade.
    /// Utilizada para rastrear altera��es e disparar a��es reativas.
    /// Enviada por: Qualquer ViewModel/Model quando uma propriedade muda.
    /// Recebida por: Listeners interessados em rastrear mudan�as espec�ficas.
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

    // -------------------------------------------------------------------------------
    // DOCUMENTA��O DE PADR�ES DE USO
    // -------------------------------------------------------------------------------
    /*
     * PADR�O DE ENVIO:
     * ----------------
     * WeakReferenceMessenger.Default.Send(new NomeDaMensagem(parametros));
     * 
     * PADR�O DE RECEBIMENTO:
     * ----------------------
     * // No construtor ou OnAppearing:
     * WeakReferenceMessenger.Default.Register<NomeDaMensagem>(this, (recipient, message) =>
     * {
     *     // L�gica de tratamento
     * });
     * 
     * // No OnDisappearing ou Cleanup:
     * WeakReferenceMessenger.Default.Unregister<NomeDaMensagem>(this);
     * 
     * BOAS PR�TICAS:
     * --------------
     * 1. ? SEMPRE Unregister no OnDisappearing/Cleanup (evita memory leak)
     * 2. ? Use WeakReferenceMessenger (n�o mant�m refer�ncias fortes)
     * 3. ? Prefira mensagens espec�ficas a gen�ricas (ex: LoteAlteradoMessage vs PropriedadeMudouMessage)
     * 4. ? Documente QUEM envia e QUEM recebe
     * 5. ?? Evite passar objetos View em mensagens (acoplamento)
     * 6. ? Use try-catch nos handlers (previne crashes)
     * 
     * EXEMPLO COMPLETO:
     * -----------------
     * // Envio (no ViewModel ap�s salvar):
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
