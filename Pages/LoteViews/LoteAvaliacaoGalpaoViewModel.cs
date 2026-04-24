using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using SilvaData.Models;
using SilvaData.Pages.PopUps;
using SilvaData.Utils;

using System.Collections.ObjectModel;
using System.Diagnostics;

namespace SilvaData.ViewModels;

public class AvaliacaoGalpaoResultado
{
    public string? Parametro { get; set; }
    public double Porcentagem { get; set; }
}

public partial class AvaliacaoGalpaoButton : ObservableObject
{
    [ObservableProperty] private int? numero;
    [ObservableProperty] private Parametro? parametro;
    [ObservableProperty] private DateTime data;
    [ObservableProperty] private int idade;
    [ObservableProperty] private int qtdeAvaliada;
    [ObservableProperty] private double media;
    [ObservableProperty] private LoteForm? loteForm;
    [ObservableProperty] private string? campoTipo;
    [ObservableProperty] private ObservableCollection<AvaliacaoGalpaoResultado> resultados = new();

    public IRelayCommand? EditaAvaliacaoCommand { get; set; }
}

public partial class LoteAvaliacaoGalpaoViewModel : ViewModelBase
{
    private readonly AvaliacaoAlternativasViewModel _avaliacaoAlternativasViewModel;

    [ObservableProperty] private Lote? lote;
    [ObservableProperty] private List<Parametro> parametrosAvaliacaoGalpaoList = new();

    // ★ OBSERVA MUDANÇAS: OnPropertyChanged chama CarregaAvaliacoes automaticamente
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TemParametroSelecionado))]
    private Parametro? parametroSelecionado;

    [ObservableProperty] private ObservableCollection<AvaliacaoGalpaoButton> avaliacaoGalpaoList = new();

    [ObservableProperty]
    private string quantidadeMinimaFormatada = "";

    [ObservableProperty]
    private string quantidadeMaximaFormatada = "";

    /// <summary>
    /// Parâmetro inicial a pré-selecionar (passado do resumo)
    /// </summary>
    public Parametro? ParametroInicial { get; set; }

    private List<LoteForm> AvaliacaoGalpaoListForm = new();
    private bool CarregandoFormulariosPreenchidos;
    private CancellationTokenSource? _carregamentoTokenSource;
    private bool _isInitializing; // ★ Flag para evitar recarregamento durante inicialização

    /// <summary>
    /// Propriedade auxiliar para binding de visibilidade
    /// </summary>
    public bool TemParametroSelecionado => ParametroSelecionado != null;

    public LoteAvaliacaoGalpaoViewModel()
    {
        _avaliacaoAlternativasViewModel = ServiceHelper.GetRequiredService<AvaliacaoAlternativasViewModel>();

        // Message registration moved to View for better lifecycle management (_needsReload approach)
    }

    /// <summary>
    /// ★ OBSERVA MUDANÇAS: Chamado automaticamente quando ParametroSelecionado muda
    /// </summary>
    partial void OnParametroSelecionadoChanged(Parametro? oldValue, Parametro? newValue)
    {
        Debug.WriteLine($"[LoteAvaliacaoGalpaoViewModel] ParametroSelecionado mudou: " +
                       $"Antigo={(oldValue?.nome ?? "NULL")}, " +
                       $"Novo={(newValue?.nome ?? "NULL")}, " +
                       $"IsInitializing={_isInitializing}");

        // Atualiza propriedades de exibição de mínimo e máximo
        if (newValue != null)
        {
            QuantidadeMinimaFormatada = newValue.qtdMinima?.ToString() ?? "0";
            QuantidadeMaximaFormatada = newValue.qtdCampos?.ToString() ?? "0";
        }

        // ★ Só recarrega se NÃO estiver inicializando
        if (!_isInitializing && newValue != null)
        {
            Debug.WriteLine($"[LoteAvaliacaoGalpaoViewModel] → Disparando CarregaAvaliacoes automaticamente");
            _ = CarregaAvaliacoes();
        }
    }

    public async Task CarregaDados(Lote lote)
    {
        if (lote == null) return;

        try
        {
            IsBusy = true;
            _isInitializing = true; // ★ Marca que está inicializando

            Lote = lote;
            Lote.EnsureNames();
            AvaliacaoGalpaoList.Clear();

            // Carrega formulários preenchidos
            await CarregaFormulariosPreenchidos();

            // Carrega lista de parâmetros
            ParametrosAvaliacaoGalpaoList = await LoteFormAvaliacaoGalpao.ListaParametrosAvalicaoGalpao();

            // ★ SELECIONA PARÂMETRO (ParametroInicial se fornecido, senão o primeiro da lista)
            if (ParametroInicial != null && ParametrosAvaliacaoGalpaoList?.Any(p => p.id == ParametroInicial.id) == true)
            {
                Debug.WriteLine($"[LoteAvaliacaoGalpaoViewModel] Selecionando parâmetro inicial: {ParametroInicial.nome}");
                ParametroSelecionado = ParametroInicial;
            }
            else if (ParametrosAvaliacaoGalpaoList?.Count > 0)
            {
                Debug.WriteLine($"[LoteAvaliacaoGalpaoViewModel] Selecionando parâmetro padrão: {ParametrosAvaliacaoGalpaoList[0].nome}");
                ParametroSelecionado = ParametrosAvaliacaoGalpaoList[0];
            }
            else
            {
                Debug.WriteLine($"[LoteAvaliacaoGalpaoViewModel] AVISO: Nenhum parâmetro disponível!");
            }

            _isInitializing = false; // ★ Marca que terminou inicialização

            // ★ Carrega avaliações do parâmetro padrão
            if (ParametroSelecionado != null)
            {
                await CarregaAvaliacoes();
            }
        }
        catch (Exception ex)
        {
            await PopUpOK.ShowAsync(Traducao.Erro, $"Erro ao carregar avaliações: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            _isInitializing = false; // Garante que sempre desmarca
        }
    }

    public async Task CarregaFormulariosPreenchidos()
    {
        if (CarregandoFormulariosPreenchidos || Lote == null) return;

        try
        {
            CarregandoFormulariosPreenchidos = true;
            var countAntes = AvaliacaoGalpaoListForm.Count;
            Debug.WriteLine($"[CarregaFormulariosPreenchidos] Iniciando - Count Antes: {countAntes}");

            if (Lote?.id == null) return;

            // Query direta sem filtro loteFormFase — garante que todos os formulários type 20 apareçam
            var table = await Db.Table<LoteForm>();
            AvaliacaoGalpaoListForm = await table
                .Where(lf => lf.loteId == (int)Lote.id
                          && lf.parametroTipoId == 20
                          && (lf.excluido == null || lf.excluido != 1))
                .ToListAsync();

            Debug.WriteLine($"[CarregaFormulariosPreenchidos] ✓ Carregou {AvaliacaoGalpaoListForm.Count} Formulários Preenchidos (Antes: {countAntes})");
        }
        finally
        {
            CarregandoFormulariosPreenchidos = false;
        }
    }

    [RelayCommand]
    public async Task CarregaAvaliacoes()
    {
        Debug.WriteLine($"[LoteAvaliacaoGalpaoViewModel] ═══ CarregaAvaliacoes INICIADO ═══");

        // ★ Cancela carregamento anterior se estiver em progresso
        _carregamentoTokenSource?.Cancel();
        _carregamentoTokenSource = new CancellationTokenSource();
        var token = _carregamentoTokenSource.Token;

        try
        {
            LoteFormAvaliacaoGalpao.IsLoadingData = true;
            IsBusy = true;
            var countAntes = AvaliacaoGalpaoList.Count;
            await MainThread.InvokeOnMainThreadAsync(() => AvaliacaoGalpaoList.Clear());

            if (ParametroSelecionado == null)
            {
                Debug.WriteLine($"[LoteAvaliacaoGalpaoViewModel] ParametroSelecionado é NULL - abortando");
                return;
            }

            Debug.WriteLine($"[LoteAvaliacaoGalpaoViewModel] Carregando avaliações para: {ParametroSelecionado.nome}");
            Debug.WriteLine($"  Formulários disponíveis: {AvaliacaoGalpaoListForm.Count}");

            if (token.IsCancellationRequested) return;

            var alternativasParametroSelecionado = await ParametroAlternativas.PegaAlternativas(ParametroSelecionado.id);
            Debug.WriteLine($"[LoteAvaliacaoGalpaoViewModel] Alternativas encontradas: {alternativasParametroSelecionado?.Count ?? 0}");

            // Constrói todos os itens em background antes de tocar a UI
            var novosItens = new List<AvaliacaoGalpaoButton>();

            for (var index = AvaliacaoGalpaoListForm.Count - 1; index >= 0; index--)
            {
                if (token.IsCancellationRequested)
                {
                    Debug.WriteLine($"[LoteAvaliacaoGalpaoViewModel] Carregamento CANCELADO");
                    return;
                }

                var loteForm = AvaliacaoGalpaoListForm[index];
                Debug.WriteLine($"  Processando formulário {index}: id={loteForm.id}, data={loteForm.data:dd/MM}");

                var respostas = await LoteFormAvaliacaoGalpao.RespostasAvaliacaoGalpaoPorLote(loteForm.id);
                if (respostas == null || ParametroSelecionado == null) continue;

                respostas = respostas.Where(t => t.parametroId == ParametroSelecionado.id).ToList();
                Debug.WriteLine($"    Respostas filtradas: {respostas.Count}");

                if (respostas.Count <= 0) continue;

                var resultados = new List<AvaliacaoGalpaoResultado>();
                if (alternativasParametroSelecionado != null)
                {
                    foreach (var alternativa in alternativasParametroSelecionado)
                    {
                        var qtde = respostas.Count(t => t.AlternativaIds.Contains(alternativa.id));
                        var porcentagem = Math.Round((double)qtde / respostas.Count * 100, 2);
                        resultados.Add(new AvaliacaoGalpaoResultado
                        {
                            Parametro = alternativa.descricao,
                            Porcentagem = porcentagem
                        });
                    }
                }

                novosItens.Add(new AvaliacaoGalpaoButton
                {
                    Numero = loteForm.item,
                    Idade = (int)(loteForm.data - (Lote?.dataInicio ?? DateTime.Now)).TotalDays,
                    QtdeAvaliada = respostas.Count,
                    Data = loteForm.data,
                    LoteForm = loteForm,
                    Media = respostas.Count == 0 ? 0 : respostas.Average(t => t.RespostaQtde ?? 0),
                    CampoTipo = ParametroSelecionado.campoTipo,
                    Resultados = new ObservableCollection<AvaliacaoGalpaoResultado>(resultados),
                    EditaAvaliacaoCommand = new AsyncRelayCommand<AvaliacaoGalpaoButton?>(EditaAvaliacao)
                });
            }

            if (token.IsCancellationRequested) return;

            // Atualiza a UI em um único dispatch — evita travar a thread principal N vezes
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                AvaliacaoGalpaoList.Clear();
                foreach (var item in novosItens)
                    AvaliacaoGalpaoList.Add(item);
            });

            Debug.WriteLine($"[LoteAvaliacaoGalpaoViewModel] Avaliações carregadas: {AvaliacaoGalpaoList.Count}");
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine($"[LoteAvaliacaoGalpaoViewModel] Carregamento CANCELADO (OperationCanceledException)");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LoteAvaliacaoGalpaoViewModel] ERRO ao carregar avaliações: {ex.Message}");
            await PopUpOK.ShowAsync(Traducao.Erro, $"Erro ao carregar avaliações: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            LoteFormAvaliacaoGalpao.IsLoadingData = false;
            Debug.WriteLine($"[LoteAvaliacaoGalpaoViewModel] ═══ CarregaAvaliacoes FINALIZADO ═══");
        }
    }

    [RelayCommand]
    private async Task NovaAvaliacao()
    {
        if (ParametroSelecionado == null)
        {
            await PopUpOK.ShowAsync(Traducao.Erro, Traducao.SelecioneParametroAvaliacao);
            return;
        }

        HapticFeedback.Default.Perform(HapticFeedbackType.Click);

        try
        {
            IsBusy = true;
            var item = AvaliacaoGalpaoList.Count == 0 ? 1 : AvaliacaoGalpaoList.Max(t => t.Numero ?? 0) + 1;
            IsBusy = false;

            await NavigationUtils.OpenLoteFormularioAsync(
                lote: Lote!,
                loteFormId: -1,
                parametroTipoId: 20,
                fase: null,
                isReadOnly: false,
                podeEditar: true,
                item: item,
                modeloIsiMacroSelecionado: null,
                limpaFormularioAtual: true,
                parametroSelecionado: ParametroSelecionado);
        }
        catch (Exception ex)
        {
            await PopUpOK.ShowAsync(Traducao.Erro, $"Erro ao criar avaliação: {ex.Message}");
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task EditaAvaliacao(AvaliacaoGalpaoButton? avaliacao)
    {
        if (avaliacao?.LoteForm == null || ParametroSelecionado == null) return;

        try
        {
            IsBusy = true;
            IsBusy = false;

            await NavigationUtils.OpenLoteFormularioAsync(
                lote: Lote!,
                loteFormId: avaliacao.LoteForm.id ?? -1,
                parametroTipoId: 20,
                fase: avaliacao.LoteForm.loteFormFase,
                isReadOnly: false,
                podeEditar: true,
                item: avaliacao.LoteForm.item,
                modeloIsiMacroSelecionado: null,
                limpaFormularioAtual: false,
                parametroSelecionado: ParametroSelecionado);
        }
        catch (Exception ex)
        {
            await PopUpOK.ShowAsync(Traducao.Erro, $"Erro ao editar avaliação: {ex.Message}");
            IsBusy = false;
        }
    }

    public override async Task Voltar()
    {
        Cleanup();
        await base.Voltar();
    }

    public override void Cleanup()
    {
        base.Cleanup();
        _carregamentoTokenSource?.Cancel();
        _carregamentoTokenSource?.Dispose();
        _carregamentoTokenSource = null;
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }
}
