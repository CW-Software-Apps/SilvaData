using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;
using System.Collections.ObjectModel;
using System.Text;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Linq;

namespace SilvaData.Models
{
    /// <summary>
    /// Tipos de campos que podem ser preenchidos no formulário
    /// </summary>
    public enum TipoPreenchimento
    {
        Alternativas = 1,     // ComboBox
        ValorInteiro = 2,     // Número inteiro
        CheckList = 3,        // Sim/Não
        ISIMacro = 4,         // Nota com imagem
        ValorDecimal = 5,     // Número decimal
        CampoTexto = 6,       // Texto livre
    }

    // ============================================================================
    // VALORES PERSISTIDOS
    // ============================================================================

    /// <summary>
    /// Armazena valores preenchidos em formulários
    /// </summary>
    [Table("ParametrosValores")]
    public class ParametrosValores
    {
        [Indexed]
        public int? parametroId { get; set; }

        [Indexed]
        public string valor { get; set; } = string.Empty;

        [Ignore]
        public string tipoPreenchimento { get; set; } = string.Empty;
    }

    // ============================================================================
    // CATEGORIAS
    // ============================================================================

    /// <summary>
    /// Agrupa parâmetros em categorias (ex: Saúde, Bem-estar, etc)
    /// </summary>
    [Table("ParametroCategoria")]
    public class ParametroCategoria
    {
        [PrimaryKey]
        public int? id { get; set; }

        public string nome { get; set; } = string.Empty;
        public int? ordem { get; set; }

        public static List<ParametroCategoria> ParametroCategorias;

        /// <summary>
        /// Obtém todas as categorias do banco de dados
        /// </summary>
        public static async Task<List<ParametroCategoria>> GetItemsAsync()
        {
            try
            {
                var table = await Db.Table<ParametroCategoria>();
                return await table.ToListAsync();
            }
            catch (SQLiteException ex) when (ex.Message.Contains("no such table"))
            {
                // Tabela não existe - provavelmente o app está iniciando e ainda não criou as tabelas
                Debug.WriteLine($"[ParametroCategoria] Tabela não encontrada, retornando lista vazia: {ex.Message}");
                return new List<ParametroCategoria>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ParametroCategoria] Erro ao obter categorias: {ex.Message}");
                return new List<ParametroCategoria>();
            }
        }
    }

    // ============================================================================
    // CONFIGURAÇÕES
    // ============================================================================

    /// <summary>
    /// Armazena configurações globais (IDs de parâmetros especiais, etc)
    /// </summary>
    [Table("ConfigParametros")]
    public class ConfigParametros
    {
        private static ConfigParametros _config;

        public DateTime dataUltimaAtualizacao { get; set; }
        public int parametroTratamentoNomeProdutoId { get; set; }  // ID do parâmetro de nome de produto
        public int parametroTratamentoProdutoId { get; set; }      // ID do parâmetro de produto

        [Ignore]
        public List<ParametroDiagnosticoTratamento> diagnosticosTratamentos { get; set; } = new List<ParametroDiagnosticoTratamento>();

        public static async Task<ConfigParametros> Config()
        {
            if (_config == null) await UpdateConfig();
            return _config;
        }

        public static async Task<ConfigParametros> UpdateConfig()
        {
            var table = await Db.Table<ConfigParametros>().ConfigureAwait(false);
            _config = await table.FirstOrDefaultAsync().ConfigureAwait(false);

            if (_config != null)
            {
                // Carrega dados relacionados
                var diagnostics = await (await Db.Table<ParametroDiagnosticoTratamento>()).ToListAsync().ConfigureAwait(false);
                var names = await (await Db.Table<ParametroDiagnosticoTratamentoNomes>()).ToListAsync().ConfigureAwait(false);

                _config.diagnosticosTratamentos = diagnostics;
                foreach (var diagnostic in _config.diagnosticosTratamentos)
                {
                    diagnostic.produtosNomes = names
                        .Where(n => n.idParametroDiagnostico == diagnostic.idParametroDiagnostico)
                        .ToList();
                }
            }

            return _config!;
        }
    }

    [Table("ParametroDiagnosticoTratamentoNomes")]
    public class ParametroDiagnosticoTratamentoNomesFromWebService
    {
        public int produtoNomeId { get; set; }
    }

    [Table("ParametroDiagnosticoTratamentoNomes")]
    public class ParametroDiagnosticoTratamentoNomes : ParametroDiagnosticoTratamentoNomesFromWebService
    {
        [Indexed]
        public int idParametroDiagnostico { get; set; }
    }

    [Table("ParametroDiagnosticoTratamento")]
    public class ParametroDiagnosticoTratamentoFromWebService
    {
        public DateTime dataUltimaAtualizacao { get; set; }
        public int diagnosticoEnfermidadeId { get; set; }
        public int tratamentoProdutoId { get; set; }

        [Ignore]
        public List<ParametroDiagnosticoTratamentoNomes> produtosNomes { get; set; } = new List<ParametroDiagnosticoTratamentoNomes>();
    }

    [Table("ParametroDiagnosticoTratamento")]
    public class ParametroDiagnosticoTratamento : ParametroDiagnosticoTratamentoFromWebService
    {
        [Indexed]
        public int idParametroDiagnostico { get; set; }
    }

    // ============================================================================
    // PARÂMETRO BASE
    // ============================================================================

    /// <summary>
    /// Definição de parâmetro (estrutura, não valores)
    /// </summary>
    [Table("Parametro")]
    public partial class Parametro : ObservableObject
    {
        [PrimaryKey]
        public int id { get; set; }
        public string nome { get; set; } = string.Empty;
        public TipoPreenchimento tipoPreenchimento { get; set; }
        public int exibir { get; set; }
        public int? required { get; set; }  // 1 = obrigatório, null/0 = opcional
        public string valorPadrao { get; set; } = string.Empty;
        public float? valorMinimo { get; set; }
        public float? valorMaximo { get; set; }

        [ObservableProperty]
        private int? _tipo;

        public int parametroTipo { get; set; }
        public float? peso { get; set; }
        public int ordem { get; set; }
        public int? qtdMinima { get; set; }
        public int? qtdCampos { get; set; }
        public string? campoTipo { get; set; }
        public int cliente { get; set; }
        public int? parametroCategoriaId { get; set; }
        public DateTime dataUltimaAtualizacao { get; set; }
        public int? excluido { get; set; }

        /// <summary>
        /// Nome da categoria do parâmetro
        /// </summary>
        [Ignore]
        public string Categoria => ParametroCategoria.ParametroCategorias != null
            ? ParametroCategoria.ParametroCategorias.FirstOrDefault(pc => pc.id == parametroCategoriaId)?.nome
            : string.Empty;

        [Ignore]
        public string CategoriaParaAgrupar { get; set; } = string.Empty;

        /// <summary>
        /// Obtém parâmetros por tipo (saúde, bem-estar, etc) - Mantido para compatibilidade
        /// </summary>
        public static async Task<List<Parametro>> PegaParametroAsync(int parametroTipo)
        {
            var sql = @"
            SELECT p.* 
            FROM Parametro p
            LEFT JOIN ParametroCategoria pc ON pc.id = p.parametroCategoriaId
            WHERE p.exibir = 1 
                AND (p.excluido IS NULL OR p.excluido = 0)
                AND p.parametroTipo = ?
            ORDER BY pc.ordem, p.ordem";

            var parametros = await Db.QueryAsync<Parametro>(sql, parametroTipo).ConfigureAwait(false);
            return parametros.ToList();
        }

        /// <summary>
        /// Classifica parâmetro em categoria de avaliação macro (ISI)
        /// </summary>
        [Ignore]
        public string MacroCategoria
        {
            get
            {
                switch (id)
                {
                    case 141:
                    case 243:
                    case 142:
                    case 348:
                        return "1_Locomotor/Carcaça";
                    case 149:
                    case 150:
                    case 148:
                        return "2_Respiratório";
                    case 145:
                    case 146:
                    case 147:
                    case 151:
                    case 143:
                    case 152:
                    case 155:
                    case 156:
                    case 153:
                    case 154:
                        return "3_Orgãos";
                    case 157:
                    case 176:
                    case 177:
                    case 178:
                    case 182:
                    case 349:
                    case 159:
                    case 160:
                    case 158:
                    case 179:
                    case 180:
                    case 183:
                    case 330:
                    case 189:
                    case 181:
                    case 190:
                    case 193:
                        return "4_Intestino";
                }
                return "0_";
            }
        }
    }

    // ============================================================================
    // PARÂMETRO COM ALTERNATIVAS (Principal)
    // ============================================================================

    /// <summary>
    /// Agrupador de parâmetros por categoria
    /// </summary>
    public class ParametroComAlternativasGroup : List<ParametroComAlternativas>
    {
        public ParametroComAlternativasGroup(string name, List<ParametroComAlternativas> pca) : base(pca)
        {
            Name = name;
        }

        public string Name { get; set; }
        public override string ToString() => Name;
    }

    /// <summary>
    /// Armazena resposta de parâmetro preenchido
    /// </summary>
    public class ParametroRespondidoPadrao
    {
        public string tabela { get; set; } = string.Empty;
        public string idCampoNome { get; set; } = string.Empty;
        public int? id { get; set; }
        public int? parametroId { get; set; }
        public string valor { get; set; } = string.Empty;

        /// <summary>
        /// Obtém respostas de parâmetros já preenchidas
        /// </summary>
        public static async Task<List<ParametroRespondidoPadrao>> PegaParametrosRespondidos(string tabela, string idCampoNome, int idParaFiltrar)
        {
            var sql = $"select '{tabela}' as tabela, '{idCampoNome}' as idCampoNome, {idCampoNome} as id, parametroId, valor from {tabela} p where {idCampoNome}={idParaFiltrar}";
            return await Db.QueryAsync<ParametroRespondidoPadrao>(sql);
        }
    }

    /// <summary>
    /// Parâmetro com lista de alternativas e valores preenchidos (Sim/Não, Texto, Número, etc)
    /// </summary>
    [Table("Parametro")]
    public partial class ParametroComAlternativas : Parametro
    {
        [Ignore]
        public List<ParametroAlternativas> ListaAlternativas { get; set; } = new List<ParametroAlternativas>();

        private int _selectedIndex = -1;

        [Ignore]
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (_selectedIndex == value) return;
                _selectedIndex = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AlternativaSelecionada));
                OnPropertyChanged(nameof(Score));
                OnPropertyChanged(nameof(Nota));
                OnPropertyChanged(nameof(NotaSemPeso));
                OnPropertyChanged(nameof(EstaRespondida));
            }
        }
        [Ignore]
        public string ValorString { get; set; } = string.Empty;  // Valor para CampoTexto
        [Ignore]
        public int? ValorInt { get; set; }                       // Valor para ValorInteiro
        [Ignore]
        public double? ValorDouble { get; set; }                 // Valor para ValorDecimal
        [Ignore]
        public bool? ValorSimNao { get; set; }                   // Valor para CheckList

        /// <summary>
        /// Nota com peso (para ISIMacro).
        /// Parâmetros "Isolados" (Tipo = 1) retornam 0 — eles aparecem no checklist
        /// para registro visual, mas não contribuem para o score total da ave.
        /// </summary>
        [Ignore]
        public int Nota
        {
            get
            {
                // Tipo = 1 → parâmetro "Isolado": exibido no formulário mas sem peso no score.
                if (AlternativaSelecionada == null || Tipo == 1)
                    return 0;
                return (int)(AlternativaSelecionada.score * peso);
            }
        }

        /// <summary>
        /// Nota sem peso (para ISIMacro).
        /// Parâmetros "Isolados" (Tipo = 1) também retornam 0 aqui pelo mesmo motivo.
        /// </summary>
        [Ignore]
        public int NotaSemPeso
        {
            get
            {
                // Tipo = 1 → parâmetro "Isolado": não entra no cálculo de score.
                if (AlternativaSelecionada == null || Tipo == 1)
                    return 0;
                if (AlternativaSelecionada.score != null)
                    return (int)(AlternativaSelecionada.score);
                return 0;
            }
        }

        /// <summary>
        /// Score da alternativa selecionada
        /// </summary>
        [Ignore]
        public int Score
        {
            get
            {
                if (AlternativaSelecionada?.score != null)
                    return AlternativaSelecionada == null ? 0 : (int)(AlternativaSelecionada.score);
                return 0;
            }
        }

        /// <summary>
        /// Alternativa selecionada (busca por índice)
        /// </summary>
        [Ignore]
        public ParametroAlternativas AlternativaSelecionada => SelectedIndex != -1 && SelectedIndex <= ListaAlternativas.Count - 1 ? ListaAlternativas[SelectedIndex] : null;

        /// <summary>
        /// Verifica se parâmetro foi respondido
        /// </summary>
        [Ignore]
        public bool EstaRespondida => SelectedIndex != -1 || ValorInt != null || ValorDouble != null || ValorSimNao != null || !string.IsNullOrEmpty(ValorString);

        /// <summary>
        /// Carrega alternativas e respostas anteriores para parâmetros
        /// </summary>
        public static async Task<List<ParametroComAlternativas>> PegaAlternativasERespostas(
            List<ParametroComAlternativas> parametros,
            bool estaRepondido,
            List<ParametroRespondidoPadrao> respostas,
            int _LoteFormVinculado = -1)
        {
            var lastCategoria = "";

            for (var index = 0; index < parametros.Count; index++)
            {
                var parametro = parametros[index];

                // Obtém alternativas para este parâmetro
                parametro.ListaAlternativas = _LoteFormVinculado != -1
                    ? await ParametroAlternativas.PegaAlternativas(parametro.id, _LoteFormVinculado).ConfigureAwait(false)
                    : await ParametroAlternativas.PegaAlternativas(parametro.id).ConfigureAwait(false);

                // Agrupa por categoria
                if (lastCategoria != parametro.Categoria)
                {
                    lastCategoria = parametro.Categoria ?? "";
                    parametro.CategoriaParaAgrupar = lastCategoria.Replace("\r", "").Replace("\n", "");
                }
                else
                {
                    parametro.CategoriaParaAgrupar = "";
                }

                if (!estaRepondido) continue;

                // Restaura valores anteriormente preenchidos
                var resposta = respostas?.FirstOrDefault(p => p.parametroId == parametro.id);
                if (resposta == null) continue;

                var valor = resposta.valor;

                switch (parametro.tipoPreenchimento)
                {
                    case TipoPreenchimento.Alternativas:
                    case TipoPreenchimento.ISIMacro:
                        if (int.TryParse(valor.Replace(".00", ""), out var parametroId))
                        {
                            var alt = parametro.ListaAlternativas.FirstOrDefault(a => a.id == parametroId);
                            if (alt != null)
                                parametro.SelectedIndex = parametro.ListaAlternativas.IndexOf(alt);
                        }
                        break;

                    case TipoPreenchimento.ValorInteiro:
                        if (int.TryParse(valor.Replace(".00", ""), out var valorInt))
                            parametro.ValorInt = valorInt;
                        break;

                    case TipoPreenchimento.CheckList:
                        if (int.TryParse(valor.Replace(".00", ""), out var check))
                            parametro.ValorSimNao = check == 1;
                        break;

                    case TipoPreenchimento.ValorDecimal:
                        if (double.TryParse(valor, out var valorDouble))
                            parametro.ValorDouble = valorDouble;
                        break;

                    case TipoPreenchimento.CampoTexto:
                        parametro.ValorString = valor;
                        break;
                }
            }

            return parametros;
        }

        /// <summary>
        /// Parâmetros para Unidade Epidemiológica
        /// </summary>
        public static async Task<List<ParametroComAlternativas>> UnidadeE_PegaParametrosComAlternativas(int unidadeId)
        {
            ParametroCategoria.ParametroCategorias ??= new List<ParametroCategoria>(await ParametroCategoria.GetItemsAsync());

            var sql = "select p.* from Parametro p " +
                      " left outer join ParametroCategoria pc on pc.id = p.parametroCategoriaId" +
                      " where p.exibir=1 and coalesce(p.excluido,0)=0 and p.parametroTipo=2" +
                      " order by pc.Ordem, ordem";

            var parametros = await Db.QueryAsync<ParametroComAlternativas>(sql).ConfigureAwait(false);

            List<ParametroRespondidoPadrao> respostas = null;
            if (unidadeId != -1)
                respostas = await ParametroRespondidoPadrao.PegaParametrosRespondidos(
                    nameof(UnidadeEpidemiologicaParametro), "unidadeEpidemiologicaId", unidadeId)
                    .ConfigureAwait(false);

            parametros = await PegaAlternativasERespostas(parametros, unidadeId != -1, respostas).ConfigureAwait(false);
            return parametros;
        }

        /// <summary>
        /// Parâmetros para Lote
        /// </summary>
        public static async Task<List<ParametroComAlternativas>> Lote_PegaParametrosComAlternativas(Lote lote, bool loteSimplificado = false)
        {
            try
            {
                ParametroCategoria.ParametroCategorias ??= new List<ParametroCategoria>(await ParametroCategoria.GetItemsAsync());

                var sql = "select p.* from Parametro p " +
                          " left outer join ParametroCategoria pc on pc.id = p.parametroCategoriaId" +
                          $" where p.exibir=1 and coalesce(p.excluido,0)=0 and p.parametroTipo={(loteSimplificado ? "19" : "3")}" +
                          " order by pc.Ordem, ordem";

                var parametros = await Db.QueryAsync<ParametroComAlternativas>(sql).ConfigureAwait(false);

                List<ParametroRespondidoPadrao> respostas = null;
                if (lote != null)
                    respostas = await ParametroRespondidoPadrao.PegaParametrosRespondidos(
                        nameof(LoteParametro), "loteID", (int)lote.id)
                        .ConfigureAwait(false);

                parametros = await PegaAlternativasERespostas(parametros, lote != null, respostas).ConfigureAwait(false);
                return parametros;
            }
            catch (SQLiteException ex) when (ex.Message.Contains("no such table"))
            {
                // Tabela não existe - retorna lista vazia para não quebrar o app
                Debug.WriteLine($"[Lote_PegaParametrosComAlternativas] Tabela não encontrada: {ex.Message}");
                return new List<ParametroComAlternativas>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Lote_PegaParametrosComAlternativas] Erro: {ex.Message}");
                return new List<ParametroComAlternativas>();
            }
        }

        /// <summary>
        /// Parâmetros para formulário (com filtro opcional por modelo ISI Macro) - Mantido para compatibilidade
        /// </summary>
        public static async Task<List<ParametroComAlternativas>> LoteForm_PegaListaParametros(
            int parametroTipo,
            int loteFormId = -1,
            int _LoteFormVinculado = -1,
            int? modeloFormularionIsiMacro = null)
        {
            return await GetForFormAsync(parametroTipo, loteFormId, _LoteFormVinculado, modeloFormularionIsiMacro);
        }

        /// <summary>
        /// Versão otimizada do LoteForm_PegaListaParametros
        /// </summary>
        public static async Task<List<ParametroComAlternativas>> GetForFormAsync(
            int parameterType,
            int formId = -1,
            int linkedFormId = -1,
            int? isiMacroModelId = null)
        {
            try
            {
                if (ParametroCategoria.ParametroCategorias == null)
                {
                    var categorias = await ParametroCategoria.GetItemsAsync().ConfigureAwait(false);
                    ParametroCategoria.ParametroCategorias = new List<ParametroCategoria>(categorias);
                }

                var sqlBuilder = new StringBuilder();
                sqlBuilder.AppendLine("SELECT p.* FROM Parametro p");
                sqlBuilder.AppendLine("LEFT OUTER JOIN ParametroCategoria pc ON pc.id = p.parametroCategoriaId");
                // Busca todos os parâmetros ativos do tipo solicitado.
                // IMPORTANTE: NÃO filtramos por p.Tipo aqui. Parâmetros "Isolados" (Tipo = 1)
                // devem aparecer no checklist do ISI Macro, porém sem somar ao score total
                // (a propriedade Nota retorna 0 para eles). Filtrar por Tipo = 0 aqui
                // causaria a exclusão silenciosa dos parâmetros "Isolados" do formulário.
                sqlBuilder.AppendLine("WHERE p.exibir = 1 AND COALESCE(p.excluido, 0) = 0 AND p.parametroTipo = ?");

                if (isiMacroModelId.HasValue)
                {
                    // Quando um modelo ISI Macro está selecionado, exibe apenas os parâmetros
                    // vinculados a esse modelo (tabela ModeloIsiMacroParametro).
                    sqlBuilder.AppendLine("AND p.id IN (SELECT ParametroId FROM ModeloIsiMacroParametro WHERE ModeloIsiMacroId = ?)");
                }

                sqlBuilder.AppendLine("ORDER BY pc.ordem, p.ordem");

                var parametrosConsulta = new List<object> { parameterType };
                if (isiMacroModelId.HasValue)
                    parametrosConsulta.Add(isiMacroModelId.Value);

                var sql = sqlBuilder.ToString();
                Debug.WriteLine($"[GetForFormAsync] SQL={sql.Replace(Environment.NewLine," ")} | params={string.Join(",", parametrosConsulta)}");

                var parametros = await Db.QueryAsync<ParametroComAlternativas>(
                    sql,
                    parametrosConsulta.ToArray()
                ).ConfigureAwait(false);

                Debug.WriteLine($"[GetForFormAsync] Query retornou {parametros.Count} linhas | parameterType={parameterType} | isiMacroModelId={isiMacroModelId}");

                List<ParametroRespondidoPadrao> parametrosPreenchidos = null;
                if (formId != -1)
                {
                    parametrosPreenchidos = await ParametroRespondidoPadrao.PegaParametrosRespondidos(
                        nameof(LoteFormParametro), "LoteFormId", formId)
                        .ConfigureAwait(false);
                    Debug.WriteLine($"[GetForFormAsync] PegaParametrosRespondidos retornou {parametrosPreenchidos?.Count ?? 0} respostas | formId={formId}");
                }

                parametros = await PegaAlternativasERespostas(
                    parametros, formId != -1, parametrosPreenchidos, linkedFormId)
                    .ConfigureAwait(false);

                Debug.WriteLine($"[GetForFormAsync] PegaAlternativasERespostas retornou {parametros.Count} parâmetros finais");

                if (parameterType == 15) // ISIMacro
                    parametros = parametros.OrderBy(p => p.ordem).ToList();

                return parametros;
            }
            catch (SQLiteException ex) when (ex.Message.Contains("no such table"))
            {
                // Tabela não existe - retorna lista vazia para não quebrar o app
                Debug.WriteLine($"[GetForFormAsync] Tabela não encontrada: {ex.Message}");
                return new List<ParametroComAlternativas>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GetForFormAsync] Erro: {ex.Message}");
                return new List<ParametroComAlternativas>();
            }
        }
    }

    // ============================================================================
    // ALTERNATIVAS (Opções)
    // ============================================================================

    /// <summary>
    /// Alternativa individual (ex: Excelente, Bom, Ruim para nota)
    /// </summary>
    [Table("ParametroAlternativas")]
    public partial class ParametroAlternativas : ParametroAlternativasFromWebService
        {
            [Indexed]
            public int idParametro { get; set; }
            [ObservableProperty]
            private bool _isSelected;

            /// <summary>
            /// Obtém alternativas para parâmetro - Mantido para compatibilidade
            /// </summary>
            public static async Task<List<ParametroAlternativas>> PegaAlternativas(int idParametro)
            {
                var table = await Db.Table<ParametroAlternativas>();
                var query = table
                    .Where(p => p.idParametro == idParametro && (p.excluido == null || p.excluido != 1))
                    .OrderBy(p => p.ordem);

                return await query.ToListAsync().ConfigureAwait(false);
            }

            /// <summary>
            /// Obtém alternativas filtradas (ex: produtos de tratamento para diagnóstico específico) - Mantido para compatibilidade
            /// </summary>
            public static async Task<List<ParametroAlternativas>> PegaAlternativas(int idParametro, int? loteFormVinculado)
            {
                if (idParametro == (await ConfigParametros.Config()).parametroTratamentoProdutoId)
                {
                    var sql = @"
                        SELECT * FROM ParametroAlternativas PA
                        WHERE PA.idParametro = ?
                          AND EXISTS (SELECT * FROM ParametroDiagnosticoTratamento PDT
                                      WHERE PDT.tratamentoProdutoId = PA.id
                                        AND EXISTS (SELECT * FROM LoteFormParametro LTF
                                                    WHERE LTF.LoteFormId = ?
                                                      AND LTF.parametroId = PDT.diagnosticoEnfermidadeId
                                                      AND LTF.valor = 1))
                        ORDER BY PA.ordem";

                    return await Db.QueryAsync<ParametroAlternativas>(
                        sql,
                        (await ConfigParametros.Config()).parametroTratamentoProdutoId,
                        loteFormVinculado ?? -1
                    ).ConfigureAwait(false);
                }

                return await PegaAlternativas(idParametro).ConfigureAwait(false);
            }

            /// <summary>
            /// Obtém alternativas de tratamento para diagnóstico específico
            /// </summary>
            public static async Task<List<ParametroAlternativas>> PegaAlternativasDiagnosticoTratamento(int tratamentoProdutoId)
            {
                var sql = @"
                    SELECT PA.* FROM ParametroAlternativas PA
                    WHERE EXISTS (SELECT * FROM ParametroDiagnosticoTratamento PDT
                                  WHERE PDT.tratamentoProdutoId = ?
                                    AND EXISTS (SELECT * FROM ParametroDiagnosticoTratamentoNomes PDTN
                                                WHERE PDTN.idParametroDiagnostico = PDT.idParametroDiagnostico
                                                  AND PDTN.produtoNomeId = PA.id))
                    ORDER BY PA.ordem";

                return await Db.QueryAsync<ParametroAlternativas>(sql, tratamentoProdutoId).ConfigureAwait(false);
            }

            /// <summary>
            /// Obtém todas as alternativas disponíveis
            /// </summary>
            public static async Task<List<ParametroAlternativas>> PegaTodasAlternativasDeParametros()
            {
                var table = await Db.Table<ParametroAlternativas>();
                return await table
                    .Where(p => p.excluido == null || p.excluido != 1)
                    .OrderBy(p => p.ordem)
                    .ToListAsync().ConfigureAwait(false);
            }

            private static List<ParametroAlternativas> _cacheAlternativas = new();
            private static DateTime _ultimoCache = DateTime.MinValue;

            /// <summary>
            /// Obtém todas as alternativas com cache para melhor performance
            /// </summary>
            public static async Task<List<ParametroAlternativas>> GetCachedAllAsync()
            {
                if (_cacheAlternativas.Count == 0 || _ultimoCache.AddMinutes(5) < DateTime.Now)
                {
                    _cacheAlternativas = await PegaTodasAlternativasDeParametros().ConfigureAwait(false);
                    _ultimoCache = DateTime.Now;
                }
                return _cacheAlternativas;
            }
        }

    /// <summary>
    /// Base para alternativa (dados do web service)
    /// </summary>
    public partial class ParametroAlternativasFromWebService : ObservableObject
        {
            [Indexed]
            public int id { get; set; }
            public string descricao { get; set; } = string.Empty;
            public int? ordem { get; set; }
            public float? score { get; set; }           // Nota/valor
            public int? valorPadrao { get; set; }
            public string urlImagem { get; set; } = string.Empty;

            [Ignore]
            public string urlImagemLocal => BuildLocalImagePath(urlImagem);

            [Ignore]
            public string urlImagemArquivo => NormalizeImageFileName(urlImagem);

            public static string BuildLocalImagePath(string? rawUrl)
            {
                var fileName = NormalizeImageFileName(rawUrl);
                return string.IsNullOrEmpty(fileName)
                    ? string.Empty
                    : Path.Combine(FileSystem.AppDataDirectory, fileName);
            }

            public static string NormalizeImageFileName(string? rawUrl)
            {
                if (string.IsNullOrWhiteSpace(rawUrl))
                    return string.Empty;

                try
                {
                    if (Uri.TryCreate(rawUrl, UriKind.Absolute, out var absoluteUri))
                    {
                        var localPath = Uri.UnescapeDataString(absoluteUri.LocalPath);
                        return Path.GetFileName(localPath);
                    }

                    var trimmed = rawUrl.Trim();
                    var queryIndex = trimmed.IndexOfAny(['?', '#']);
                    if (queryIndex >= 0)
                        trimmed = trimmed[..queryIndex];

                    trimmed = Uri.UnescapeDataString(trimmed).Replace('\\', '/');
                    return Path.GetFileName(trimmed);
                }
                catch
                {
                    return Path.GetFileName(rawUrl);
                }
            }



            public int? excluido { get; set; }
        }

    public class ParametroFromWebService : Parametro
        {
            public ParametroCategoria parametroCategoria { get; set; }
            public List<ParametroAlternativas> alternativas { get; set; } = new List<ParametroAlternativas>();
        }

        // ============================================================================
        // GERENCIAMENTO DE PARÂMETROS
        // ============================================================================

    /// <summary>
    /// Gerencia todos os parâmetros da aplicação
    /// </summary>
    public class Parametros
        {
            public delegate Task CheckRequiredFields();
            public delegate string PegaValorParametro(int parametroId);

            public static string UltimaCategoria = "";

            public List<ParametroFromWebService> parametros { get; set; } = new List<ParametroFromWebService>();
            public ConfigParametros configParametros { get; set; }

            /// <summary>
            /// Salva respostas de parâmetros preenchidos
            /// </summary>
            internal static async Task SalvaParametros(
                ObservableCollection<ParametroComAlternativas> ParametrosComAlternativasList,
                string tablename = "",
                string fieldnameparametrokey = "",
                string valuekey = "")
            {
                if (ParametrosComAlternativasList == null) return;

                await Db.RunInTransactionAsync(nonAsyncConnection =>
                {
                    foreach (var parametro in ParametrosComAlternativasList)
                    {
                        string valor = "";
                        bool deveSalvar = false;

                        // Determina valor a salvar baseado no tipo
                        switch (parametro.tipoPreenchimento)
                        {
                            case TipoPreenchimento.ValorInteiro:
                                if (parametro.ValorInt != null) { valor = parametro.ValorInt.ToString(); deveSalvar = true; }
                                break;
                            case TipoPreenchimento.CheckList:
                                if (parametro.ValorSimNao != null) { valor = parametro.ValorSimNao == true ? "1" : "0"; deveSalvar = true; }
                                break;
                            case TipoPreenchimento.Alternativas:
                            case TipoPreenchimento.ISIMacro:
                                if (parametro.SelectedIndex != -1 && parametro.SelectedIndex < parametro.ListaAlternativas.Count)
                                { valor = $"{parametro.ListaAlternativas[parametro.SelectedIndex].id}"; deveSalvar = true; }
                                break;
                            case TipoPreenchimento.ValorDecimal:
                                if (parametro.ValorDouble != null) { valor = parametro.ValorDouble.ToString(); deveSalvar = true; }
                                break;
                            case TipoPreenchimento.CampoTexto:
                                if (parametro.ValorString != null) { valor = parametro.ValorString; deveSalvar = true; }
                                break;
                        }

                        if (deveSalvar)
                        {
                            nonAsyncConnection.Execute($"delete from {tablename} where parametroId={parametro.id} and {fieldnameparametrokey}={valuekey}");
                            nonAsyncConnection.Execute(
                                $"insert into {tablename} (parametroId,{fieldnameparametrokey},valor) VALUES (?,?,?)",
                                parametro.id, valuekey, valor);
                        }
                    }
                });
            }

            /// <summary>
            /// Obtém todos os parâmetros disponíveis
            /// </summary>
            public static async Task<List<Parametro>> PegaTodosOsParametrosAsync()
            {
                var table = await Db.Table<Parametro>();
                return await table
                    .Where(p => p.exibir == 1 && (p.excluido == null || p.excluido != 1))
                    .ToListAsync().ConfigureAwait(false);
            }

            private static List<Parametro> _cacheParametros = new();
            private static DateTime _ultimoCacheParametros = DateTime.MinValue;

            /// <summary>
            /// Obtém todos os parâmetros com cache para melhor performance
            /// </summary>
            public static async Task<List<Parametro>> GetCachedAllAsync()
            {
                if (_cacheParametros.Count == 0 || _ultimoCacheParametros.AddMinutes(5) < DateTime.Now)
                {
                    _cacheParametros = await PegaTodosOsParametrosAsync().ConfigureAwait(false);
                    _ultimoCacheParametros = DateTime.Now;
                }
                return _cacheParametros;
            }
    }
}
