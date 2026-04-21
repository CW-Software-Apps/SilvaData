using CommunityToolkit.Mvvm.ComponentModel;

using Newtonsoft.Json;

namespace SilvaData.Models
{
    /// <summary>
    /// DTO para os par�metros de requisi��o dos gr�ficos.
    /// </summary>
    public class pegaGraficosParametros
    {
        public string? usuario { get; set; }
        public string? dispositivoId { get; set; }
        public string? session { get; set; }
    }

    /// <summary>
    /// Modelo base para dados de SuperCategoria agrupados.
    /// </summary>
    public partial class GraficoSuperCategoriaAgrupado : ObservableObject
    {
        public string? SuperCategoriaId { get; set; }
        public string? SuperCategoria { get; set; }
        public DateTime dataLote { get; set; }
        public double MediaScore { get; set; }
        public double ScoreTotal { get; set; }
        public double Quantidade { get; set; }
        public double PorcentagemAcometimento { get; set; }
    }

    /// <summary>
    /// Modelo para dados de SuperCategoria agrupados por UE.
    /// MIGRADO: Propriedade computada removida (ser� resolvida no ViewModel).
    /// </summary>
    public partial class GraficoSuperCategoriaAgrupadoPorUE : GraficoSuperCategoriaAgrupado
    {
        public int UnidadeEpidemiologicaId { get; set; }

        // REMOVIDO: [JsonIgnore] public string? UnidadeEpidemiologicaNome => ...
        // Agora ser� resolvido no ViewModel usando CacheService quando necess�rio:
        // var nomeDaUE = _cacheService.UEList.FirstOrDefault(ue => ue.id == item.UnidadeEpidemiologicaId)?.nome;
    }

    /// <summary>
    /// Modelo base para dados de Categoria agrupados.
    /// </summary>
    public partial class GraficoCategoriaAgrupado : ObservableObject
    {
        public string? Categoria { get; set; }
        public string? CategoriaId { get; set; }
        public DateTime dataLote { get; set; }
        public double MediaScore { get; set; }
        public double ScoreTotal { get; set; }
        public double Quantidade { get; set; }
    }

    /// <summary>
    /// Modelo para dados de Categoria agrupados por UE.
    /// MIGRADO: Propriedade computada removida (ser� resolvida no ViewModel).
    /// </summary>
    public partial class GraficoCategoriaAgrupadoPorUE : GraficoCategoriaAgrupado
    {
        public int UnidadeEpidemiologicaId { get; set; }

        // REMOVIDO: [JsonIgnore] public string? UnidadeEpidemiologicaNome => ...
    }

    /// <summary>
    /// Modelo base para dados de Par�metro agrupados.
    /// </summary>
    public partial class GraficoParametroAgrupado : ObservableObject
    {
        public string? Parametro { get; set; }
        public string? ParametroId { get; set; }
        public DateTime dataLote { get; set; }
        public double MediaScore { get; set; }
        public double ScoreTotal { get; set; }
        public double Quantidade { get; set; }
    }

    /// <summary>
    /// Modelo para dados de Par�metro agrupados por UE.
    /// MIGRADO: Propriedade computada removida (ser� resolvida no ViewModel).
    /// </summary>
    public partial class GraficoParametroAgrupadoPorUE : GraficoParametroAgrupado
    {
        public int UnidadeEpidemiologicaId { get; set; }

        // REMOVIDO: [JsonIgnore] public string? UnidadeEpidemiologicaNome => ...
    }

    /// <summary>
    /// Modelo de dados brutos (DTO) vindo da API para SuperCategoria.
    /// MIGRADO: Propriedades computadas removidas (ser�o resolvidas no ViewModel).
    /// </summary>
    public partial class GraficoSuperCategoria : ObservableObject
    {
        public DateTime dataLote { get; set; }
        public string? SuperCategoriaId { get; set; }
        public string? SuperCategoriaNome { get; set; }
        public string? ParametroCategoriaId { get; set; }
        public string? ParametroCategoriaNome { get; set; }
        public int ParametroId { get; set; }
        public string? ParametroNome { get; set; }
        public int RegionalId { get; set; }
        public int UnidadeEpidemiologicaId { get; set; }

        [JsonIgnore] public string? RegionalNome { get; set; }
        [JsonIgnore] public string? UnidadeEpidemiologicaNome { get; set; }

        public double Score { get; set; }
        public float Peso { get; set; }
        public float Quantidade { get; set; }
        public float ScoreTotal => (float)Score * Peso;
        public float ScoreMedio => (Quantidade > 0) ? ScoreTotal / Quantidade : 0;
    }

    /// <summary>
    /// Modelo de dados brutos (DTO) vindo da API para Dispers�o.
    /// MIGRADO: Modelo simplificado (sem depend�ncias externas).
    /// </summary>
    public partial class GraficoDispersao : ObservableObject
    {
        public DateTime dataLote { get; set; }
        public int Id { get; set; }
        public int Item { get; set; }
        public string? LoteNumero { get; set; }
        public string? PropriedadeNome { get; set; }
        public string? RegionalNome { get; set; }
        public string? UnidadeEpidemiologicaNome { get; set; }
        public double Score { get; set; }
        public int QtdeAgrupado { get; set; }
    }
}
