namespace SilvaData.Models
{
    public class ISIMacroResumoFormulario
    {
        public int LoteFormId { get; set; }
        public double ScoreTotal { get; set; }
    }

    public class ISIMacro
    {
        public const double AnimalSaudavel = 21;
        public const double AnimalEmAlerta = 40;

        public int parametroCategoriaId { get; set; }
        public string? nome { get; set; } = string.Empty;
        public int scoreTotal { get; set; }


        /// <summary>
        /// Obt�m os itens com detalhes do loteFormId especificado. 
        /// </summary>
        /// <param name="loteFormId"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static async Task<List<ISIMacro>> GetItemsComDetalhesAsync(int? loteFormId)
        {
            // Verifica se o loteFormId � nulo
            if (loteFormId == null)
            {
                throw new ArgumentNullException(nameof(loteFormId), "O ID do lote n�o pode ser nulo.");
            }

            // Executa a consulta SQL para obter os itens com detalhes
            // IMPORTANTE: O filtro "p.Tipo = 0" � INTENCIONAL aqui.
            // Esta query calcula o score total por categoria para exibi��o no resumo do ISI Macro.
            // Par�metros "Isolados" (Tipo = 1) s�o registrados no formul�rio mas N�O entram
            // no c�lculo do score � por isso o JOIN exclui Tipo != 0.
            // No formul�rio de preenchimento, os par�metros Isolados S�O exibidos (ver
            // GetForFormAsync em Parametros.cs), mas a propriedade Nota deles retorna 0.
            return await Db.QueryAsync<ISIMacro>(
                "SELECT p.parametroCategoriaId, pc.nome, SUM(pa.score * p.peso) AS scoreTotal " +
                "FROM LoteFormParametro lfp " +
                "INNER JOIN Parametro p ON p.id = lfp.parametroId AND p.Tipo = 0 " + // Tipo = 0 ? exclui "Isolados" do score
                "INNER JOIN ParametroCategoria pc ON pc.id = p.parametroCategoriaId " +
                "INNER JOIN ParametroAlternativas pa ON pa.idParametro = p.id AND pa.id = lfp.valor " +
                "WHERE lfp.LoteFormId = ? AND COALESCE(p.excluido, 0) = 0 " +
                "GROUP BY p.parametroCategoriaId, pc.nome " +
                "ORDER BY pc.ordem",
                loteFormId
            );
        }

        public static async Task<Dictionary<int, double>> GetScoresPorFormularioDoLoteAsync(int loteId)
        {
            const string sql = @"
                SELECT
                    lf.id AS LoteFormId,
                    COALESCE(SUM(pa.score * p.peso), 0) AS ScoreTotal
                FROM LoteForm lf
                LEFT JOIN LoteFormParametro lfp ON lfp.LoteFormId = lf.id
                LEFT JOIN Parametro p ON p.id = lfp.parametroId
                    AND p.Tipo = 0
                    AND COALESCE(p.excluido, 0) = 0
                LEFT JOIN ParametroAlternativas pa ON pa.idParametro = p.id AND pa.id = lfp.valor
                WHERE lf.loteId = ?
                    AND lf.parametroTipoId = 15
                    AND lf.loteFormFase IS NULL
                GROUP BY lf.id";

            var resumo = await Db.QueryAsync<ISIMacroResumoFormulario>(sql, loteId);
            return resumo.ToDictionary(item => item.LoteFormId, item => item.ScoreTotal);
        }

        public static Color StatusColor(double score)
        {
            return score <= ISIMacro.AnimalSaudavel ? Color.FromArgb("#48ba00") : score <= ISIMacro.AnimalEmAlerta ? Color.FromArgb("#ffba00") : Color.FromArgb("#fc4c17");
        }

        public static string StatusText(double score)
        {
            return score <= ISIMacro.AnimalSaudavel ? Traducao.Bom : score <= ISIMacro.AnimalEmAlerta ? Traducao.Regular : Traducao.Ruim;
        }

        public static Color StatusColorBackground(double score)
        {
            return score <= ISIMacro.AnimalSaudavel ? Color.FromArgb("#c9efba") : score <= ISIMacro.AnimalEmAlerta ? Color.FromArgb("#fdf1d2") : Color.FromArgb("#f7d4c9");
        }
    }
}
