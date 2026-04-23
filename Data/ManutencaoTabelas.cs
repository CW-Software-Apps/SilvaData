using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

using SQLite;

namespace SilvaData.Models
{
    /// <summary>
    /// Gerencia o schema do banco de dados, incluindo criação, migração e manutenção de tabelas.
    /// </summary>
    public class ManutencaoTabelas
    {
        /// <summary>
        /// O número total de alterações locais pendentes de sincronização.
        /// </summary>
        public static int TotalAlteracoes { get; private set; }

        /// <summary>
        /// Atualiza a contagem total de alterações pendentes.
        /// </summary>
        public static async Task UpdateTotalAlteracoes()
        {
            const string sql = "SELECT sum(qtde) Qtde from ( " +
                               " select count(id) qtde, 'Proprietario' as Tabela from Proprietario prop where prop.temmudanca = 1 " +
                               " union select count(id) qtde, 'Regional' as Tabela from Regional r where r.temmudanca = 1 " +
                               " union select count(id) qtde, 'Propriedade' as Tabela from Propriedade p where p.temmudanca = 1 " +
                               " union select count(id) qtde, 'UE' as Tabela from UnidadeEpidemiologica ue where ue.temmudanca = 1 " +
                               " union select count(id) qtde, 'Lote' as Tabela from Lote l where l.temmudanca = 1 " +
                               " union select count(id) qtde, 'LoteForm' as Tabela from LoteForm lf where lf.temmudanca = 1 " +
                               " union select count(id) qtde, 'LoteVisita' as Tabela from LoteVisita lv where lv.temmudanca = 1 " +
                               ")";

            try
            {
                var db = await Database.GetInstanceAsync().ConfigureAwait(false);
                var alteracao = await db.sqlConnection.QueryAsync<Alteracao>(sql).ConfigureAwait(false);

                TotalAlteracoes = (alteracao?.Count > 0) ? alteracao[0].Qtde : 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ManutencaoTabelas] Erro ao atualizar total de alterações: {ex.Message}");
                TotalAlteracoes = 0;
            }
        }

        /// <summary>
        /// Verifica a versão do aplicativo e, se for nova, executa a criação/migração das tabelas.
        /// Índices são sempre verificados (idempotentes) para garantir consistência mesmo após falha parcial.
        /// </summary>
        public static async Task ChecaSePrecisaAtualizarTabelas()
        {
            var db = await Database.GetInstanceAsync().ConfigureAwait(false);
            var connection = db.sqlConnection;

            var version = Preferences.Get("Version", "");
            var buildNumber = Preferences.Get("BuildNumber", "");

            if (version != AppInfo.VersionString || buildNumber != AppInfo.BuildString)
            {
                await CriaOuAtualizaTabelas(connection).ConfigureAwait(false);

                Preferences.Set("Version", AppInfo.VersionString);
                Preferences.Set("BuildNumber", AppInfo.BuildString);
            }

            // Índices são idempotentes (IF NOT EXISTS) — rodam sempre para
            // garantir que existam mesmo se uma migração anterior falhou parcialmente.
            await CreateIndexesIfNeeded(connection).ConfigureAwait(false);
        }

        /// <summary>
        /// Garante que todas as tabelas do aplicativo existam no banco de dados.
        /// Executa migrações simples (Drop/Create) para tabelas que causam erro.
        /// </summary>
        public static async Task CriaOuAtualizaTabelas(SQLiteAsyncConnection? connection)
        {
            if (connection == null)
            {
                var db = await Database.GetInstanceAsync().ConfigureAwait(false);
                connection = db.sqlConnection;
            }

            // Tabelas Principais
            await connection.CreateTableAsync<ParametroCategoria>().ConfigureAwait(false);
            await connection.CreateTableAsync<Parametro>().ConfigureAwait(false);
            await connection.CreateTableAsync<ParametroAlternativas>().ConfigureAwait(false);
            await connection.CreateTableAsync<ConfigParametros>().ConfigureAwait(false);
            await connection.CreateTableAsync<ParametroDiagnosticoTratamento>().ConfigureAwait(false);
            await connection.CreateTableAsync<ParametroDiagnosticoTratamentoNomes>().ConfigureAwait(false);
            await connection.CreateTableAsync<Proprietario>().ConfigureAwait(false);
            await connection.CreateTableAsync<Propriedade>().ConfigureAwait(false);
            await connection.CreateTableAsync<Regional>().ConfigureAwait(false);
            await connection.CreateTableAsync<PropriedadeParametro>().ConfigureAwait(false);
            await connection.CreateTableAsync<UnidadeEpidemiologica>().ConfigureAwait(false);
            await connection.CreateTableAsync<UnidadeEpidemiologicaParametro>().ConfigureAwait(false);
            await connection.CreateTableAsync<TipoAtividade>().ConfigureAwait(false);

            // Lote e Formulários
            await connection.CreateTableAsync<Lote>().ConfigureAwait(false);
            await connection.CreateTableAsync<LoteParametro>().ConfigureAwait(false);
            await connection.CreateTableAsync<ModeloIsiMacro>().ConfigureAwait(false);
            await connection.CreateTableAsync<ModeloIsiMacroParametro>().ConfigureAwait(false);
            await connection.CreateTableAsync<LoteFormParametro>().ConfigureAwait(false);
            await connection.CreateTableAsync<LoteFormAvaliacaoGalpao>().ConfigureAwait(false);
            await connection.CreateTableAsync<LoteFormImagem>().ConfigureAwait(false);

            // Atividades e Notificações
            await connection.CreateTableAsync<Atividade>().ConfigureAwait(false);
            await connection.CreateTableAsync<AtividadeOutroUsuario>().ConfigureAwait(false);
            await connection.CreateTableAsync<TipoCorAtividade>().ConfigureAwait(false);

            // Tabelas com migração (Drop/Create)
            await CreateOrRecreateTableAsync<LoteVisita>(connection).ConfigureAwait(false);
            await CreateOrRecreateTableAsync<LoteForm>(connection).ConfigureAwait(false);
            await CreateOrRecreateTableAsync<Notificacao>(connection).ConfigureAwait(false);
        }

        /// <summary>
        /// Tenta criar a tabela. Se falhar (schema incompatível), recria com Drop/Create.
        /// </summary>
        private static async Task CreateOrRecreateTableAsync<T>(SQLiteAsyncConnection connection) where T : new()
        {
            try
            {
                await connection.CreateTableAsync<T>().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ManutencaoTabelas] Recriando tabela {typeof(T).Name}: {ex.Message}");
                await connection.DropTableAsync<T>().ConfigureAwait(false);
                await connection.CreateTableAsync<T>().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Cria índices que otimizam as consultas mais frequentes.
        /// Idempotente (IF NOT EXISTS) e resiliente — falha em um índice não impede os demais.
        /// </summary>
        private static async Task CreateIndexesIfNeeded(SQLiteAsyncConnection connection)
        {
            string[] indexes =
            {
                // Parâmetros e Alternativas
                "CREATE INDEX IF NOT EXISTS idx_Parametro_parametroCategoriaId ON Parametro(parametroCategoriaId)",
                "CREATE INDEX IF NOT EXISTS idx_Parametro_parametroTipo ON Parametro(parametroTipo)",
                "CREATE INDEX IF NOT EXISTS idx_Parametro_Tipo ON Parametro(Tipo)",
                "CREATE INDEX IF NOT EXISTS idx_ParametroAlternativas_idParametro ON ParametroAlternativas(idParametro)",
                "CREATE INDEX IF NOT EXISTS idx_ParametroAlternativas_id ON ParametroAlternativas(id)",
                "CREATE INDEX IF NOT EXISTS idx_ParametroAlternativas_idParametro_id ON ParametroAlternativas(idParametro, id)",

                // Diagnóstico/Tratamento
                "CREATE INDEX IF NOT EXISTS idx_ParamDiagTrat_idDiag ON ParametroDiagnosticoTratamento(idParametroDiagnostico)",
                "CREATE INDEX IF NOT EXISTS idx_ParamDiagTratNomes_idDiag ON ParametroDiagnosticoTratamentoNomes(idParametroDiagnostico)",
                "CREATE INDEX IF NOT EXISTS idx_ParamDiagTratNomes_produtoNomeId ON ParametroDiagnosticoTratamentoNomes(produtoNomeId)",

                // Proprietário/Propriedade/Regional
                "CREATE INDEX IF NOT EXISTS idx_Propriedade_proprietarioId ON Propriedade(proprietarioId)",
                "CREATE INDEX IF NOT EXISTS idx_Propriedade_regionalId ON Propriedade(regionalId)",
                "CREATE INDEX IF NOT EXISTS idx_PropriedadeParametro_parametroId ON PropriedadeParametro(parametroId)",
                "CREATE INDEX IF NOT EXISTS idx_PropriedadeParametro_propriedadeId ON PropriedadeParametro(propriedadeId)",

                // Unidade Epidemiológica
                "CREATE INDEX IF NOT EXISTS idx_UnidadeEpidemiologica_propriedadeId ON UnidadeEpidemiologica(propriedadeId)",
                "CREATE INDEX IF NOT EXISTS idx_UnidadeEpidemiologica_idApp ON UnidadeEpidemiologica(idApp)",
                "CREATE INDEX IF NOT EXISTS idx_UnidEpiParam_parametroId ON UnidadeEpidemiologicaParametro(parametroId)",
                "CREATE INDEX IF NOT EXISTS idx_UnidEpiParam_unidadeEpidemiologicaId ON UnidadeEpidemiologicaParametro(unidadeEpidemiologicaId)",

                // Lote e relações
                "CREATE INDEX IF NOT EXISTS idx_Lote_unidadeEpidemiologicaId ON Lote(unidadeEpidemiologicaId)",
                "CREATE INDEX IF NOT EXISTS idx_Lote_loteStatus ON Lote(loteStatus)",
                "CREATE INDEX IF NOT EXISTS idx_Lote_idApp ON Lote(idApp)",
                "CREATE INDEX IF NOT EXISTS idx_Lote_temmudanca ON Lote(temmudanca)",
                "CREATE INDEX IF NOT EXISTS idx_LoteParametro_parametroId ON LoteParametro(parametroId)",
                "CREATE INDEX IF NOT EXISTS idx_LoteParametro_loteId ON LoteParametro(loteId)",

                // Modelo ISI Macro
                "CREATE INDEX IF NOT EXISTS idx_ModeloIsiMacroParametro_ModeloId ON ModeloIsiMacroParametro(ModeloIsiMacroId)",
                "CREATE INDEX IF NOT EXISTS idx_ModeloIsiMacroParametro_ParametroId ON ModeloIsiMacroParametro(ParametroId)",

                // LoteForm e derivados
                "CREATE INDEX IF NOT EXISTS idx_LoteForm_loteId ON LoteForm(loteId)",
                "CREATE INDEX IF NOT EXISTS idx_LoteForm_parametroTipoId ON LoteForm(parametroTipoId)",
                "CREATE INDEX IF NOT EXISTS idx_LoteForm_loteId_parametroTipoId_fase ON LoteForm(loteId, parametroTipoId, loteFormFase)",
                "CREATE INDEX IF NOT EXISTS idx_LoteForm_loteId_parametroTipoId_item_data ON LoteForm(loteId, parametroTipoId, item, data)",
                "CREATE INDEX IF NOT EXISTS idx_LoteForm_idApp ON LoteForm(idApp)",
                "CREATE INDEX IF NOT EXISTS idx_LoteForm_temmudanca ON LoteForm(temmudanca)",
                "CREATE INDEX IF NOT EXISTS idx_LoteFormParametro_LoteFormId ON LoteFormParametro(LoteFormId)",
                "CREATE INDEX IF NOT EXISTS idx_LoteFormParametro_LoteFormId_parametroId_valor ON LoteFormParametro(LoteFormId, parametroId, valor)",
                "CREATE INDEX IF NOT EXISTS idx_LoteFormParametro_parametroId ON LoteFormParametro(parametroId)",
                "CREATE INDEX IF NOT EXISTS idx_LoteFormAvaliacaoGalpao_LoteFormId ON LoteFormAvaliacaoGalpao(LoteFormId)",
                "CREATE INDEX IF NOT EXISTS idx_LoteFormAvaliacaoGalpao_parametroId ON LoteFormAvaliacaoGalpao(parametroId)",
                "CREATE INDEX IF NOT EXISTS idx_LoteFormImagem_LoteFormId ON LoteFormImagem(LoteFormId)",
                "CREATE INDEX IF NOT EXISTS idx_LoteForm_parametroTipoId_loteId ON LoteForm(parametroTipoId, loteId)",
                "CREATE INDEX IF NOT EXISTS idx_LoteFormParametro_valor ON LoteFormParametro(valor)",
                "CREATE INDEX IF NOT EXISTS idx_LoteFormParametro_parametroId_valor ON LoteFormParametro(parametroId, valor)",

                // Atividades
                "CREATE INDEX IF NOT EXISTS idx_Atividade_unidadeEpidemiologicaId ON Atividade(unidadeEpidemiologicaId)",
                "CREATE INDEX IF NOT EXISTS idx_Atividade_usuarioResponsavelId ON Atividade(usuarioResponsavelId)",
                "CREATE INDEX IF NOT EXISTS idx_Atividade_atividadeStatus ON Atividade(atividadeStatus)",
                "CREATE INDEX IF NOT EXISTS idx_AtividadeOutroUsuario_atividadeId ON AtividadeOutroUsuario(atividadeId)",
                "CREATE INDEX IF NOT EXISTS idx_AtividadeOutroUsuario_usuarioId ON AtividadeOutroUsuario(usuarioId)",

                // LoteVisita e mudanças pendentes
                "CREATE INDEX IF NOT EXISTS idx_LoteVisita_lote ON LoteVisita(lote)",
                "CREATE INDEX IF NOT EXISTS idx_LoteVisita_temmudanca ON LoteVisita(temmudanca)",
                "CREATE INDEX IF NOT EXISTS idx_Proprietario_temmudanca ON Proprietario(temmudanca)",
                "CREATE INDEX IF NOT EXISTS idx_Regional_temmudanca ON Regional(temmudanca)",
                "CREATE INDEX IF NOT EXISTS idx_Propriedade_temmudanca ON Propriedade(temmudanca)",
                "CREATE INDEX IF NOT EXISTS idx_UnidadeEpidemiologica_temmudanca ON UnidadeEpidemiologica(temmudanca)",
                "CREATE INDEX IF NOT EXISTS idx_LoteForm_temmudanca_only ON LoteForm(temmudanca)",
                "CREATE INDEX IF NOT EXISTS idx_Notificacao_dataHora ON Notificacao(dataHora)",
            };

            foreach (var sql in indexes)
            {
                try
                {
                    await connection.ExecuteAsync(sql).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ManutencaoTabelas] Falha ao criar índice: {sql} — {ex.Message}");
                }
            }
        }

        /// <summary>
        /// (Assumindo que a classe Tabela existe) Deleta tabelas antigas.
        /// </summary>
        internal static async Task DeletaTabelasAntigas()
        {
            // Este método parece depender de outra classe 'Tabela'.
            // Se 'Tabela.GetTabelasAsync' ou 'tabela.DropTable' precisarem da conexão,
            // eles devem ser refatorados para usar 'Database.GetInstanceAsync()' internamente.

            var tabelas = await Tabela.GetTabelasAsync().ConfigureAwait(false);
            foreach (var tabela in tabelas)
            {
                await tabela.DropTable(tabela).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Força a exclusão de todas as tabelas antigas e recria o schema.
        /// </summary>
        public static async Task DeletaTabelasAntigasERecriaTudo()
        {
            await DeletaTabelasAntigas().ConfigureAwait(false);
            await CriaOuAtualizaTabelas().ConfigureAwait(false);
        }
    }
}