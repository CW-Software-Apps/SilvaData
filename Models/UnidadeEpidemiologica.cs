using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

using SilvaData.Utilities;

using Newtonsoft.Json;

using SQLite;

namespace SilvaData.Models
{
    public class UpdateDataParametrosUnidadeEpidemiologica : UpdateDataParametros
    {
        public new List<UnidadeEpidemiologicaFromWebService>? array { get; set; }
    }

    public partial class UnidadeEpidemiologicaParametro : ParametrosValores
    {
        public int? unidadeEpidemiologicaId { get; set; }

        public static async Task<List<UnidadeEpidemiologicaParametro>> GetItemsAsync(int id)
        {
            var table = await Db.Table<UnidadeEpidemiologicaParametro>();
            return await table.Where(p => p.unidadeEpidemiologicaId == id).ToListAsync();
        }

        public static Task<int> SaveItemAsync(UnidadeEpidemiologicaParametro uep)
        {
            return Db.InsertOrReplaceAsync(uep);
        }

        internal static async Task<List<ParametrosValores>> GetItemsForUploadAsync(int? id)
        {
            var table = await Db.Table<UnidadeEpidemiologicaParametro>();
            var parlist = await table.Where(p => p.unidadeEpidemiologicaId == id).ToListAsync();

            var result = new List<ParametrosValores>();
            foreach (var par in parlist)
            {
                result.Add(new ParametrosValores { parametroId = par.parametroId, valor = par.valor });
            }
            return result;
        }
    }

    public partial class UnidadeEpidemiologica : ObservableObject
    {
        // REMOVIDO: public static List<UnidadeEpidemiologica> UnidadeEpidemiologicasList

        private int _id;
        [PrimaryKey]
        public int id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private int? _idApp;
        public int? idApp
        {
            get => _idApp;
            set => SetProperty(ref _idApp, value);
        }

        private string? _nome;
        public string? nome
        {
            get => _nome;
            set => SetProperty(ref _nome, value);
        }

        private double _latitude;
        public double latitude
        {
            get => _latitude;
            set => SetProperty(ref _latitude, value);
        }

        private double _longitude;
        public double longitude
        {
            get => _longitude;
            set => SetProperty(ref _longitude, value);
        }

        private int _status;
        public int status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        private int? _propriedadeId;
        [JsonProperty("propriedade")]
        public int? propriedadeId
        {
            get => _propriedadeId;
            set => SetProperty(ref _propriedadeId, value);
        }

        private DateTime _dataUltimaAtualizacao;
        public DateTime dataUltimaAtualizacao
        {
            get => _dataUltimaAtualizacao;
            set => SetProperty(ref _dataUltimaAtualizacao, value);
        }

        private int? _excluido;
        public int? excluido
        {
            get => _excluido;
            set => SetProperty(ref _excluido, value);
        }

        private bool _temmudanca;
        public bool temmudanca
        {
            get => _temmudanca;
            set => SetProperty(ref _temmudanca, value);
        }

        // REMOVIDO: Propriedades computadas que dependiam de DadosStatic
        // Agora ser�o resolvidas no ViewModel ou CacheService

        /// <summary>
        /// MIGRADO: Retorna a lista ao inv�s de modificar est�tico
        /// </summary>
        public static async Task<List<UnidadeEpidemiologica>> PegaListaUE()
        {
            var table = await Db.Table<UnidadeEpidemiologica>().ConfigureAwait(false);

            return await table
                .Where(p => p.status == 1 && (p.excluido == null || p.excluido != 1))
                .OrderBy(u => u.nome)
                .ToListAsync().ConfigureAwait(false);
        }

        public static async Task<UnidadeEpidemiologica> GetItemAsync(int id)
        {
            var table = await Db.Table<UnidadeEpidemiologica>().ConfigureAwait(false);
            return await table.Where(i => i.id == id).FirstOrDefaultAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// MIGRADO: Envia mensagem para CacheService atualizar
        /// </summary>
        public static async Task SalvaUEAsync(UnidadeEpidemiologica item)
        {
            item.dataUltimaAtualizacao = DateTime.Now;
            item.temmudanca = true;
            item.status = 1;

            if (item.id != 0)
            {
                await Db.UpdateAsync(item).ConfigureAwait(false);
            }
            else
            {
                item.id = await Alteracao.GetNextId(item).ConfigureAwait(false);
                await Db.InsertAsync(item).ConfigureAwait(false);
            }

            // MUDAN�A: Notifica o CacheService para atualizar
            WeakReferenceMessenger.Default.Send(new RefreshCacheMessage(CacheType.UnidadesEpidemiologicas));
        }

        public Task<int> DeleteItemAsync(UnidadeEpidemiologica item)
        {
            return Db.DeleteAsync(item);
        }

        internal static async Task UploadUpdates()
        {
            string updateJson = string.Empty;

            try
            {
                var sql = Alteracao.SqlNovosDados("unidadeepidemiologica");
                var alteracoes = await Db.QueryAsync<UnidadeEpidemiologicaFromWebService>(sql).ConfigureAwait(false);

                foreach (var alteracao in alteracoes)
                {
                    alteracao.parametros = await UnidadeEpidemiologicaParametro.GetItemsForUploadAsync(alteracao.id).ConfigureAwait(false);

                    if (alteracao.idApp == 0 || alteracao.idApp == null)
                    {
                        alteracao.idApp = alteracao.id;
                        alteracao.id = -1;
                    }
                }

                var updateData = new UpdateDataParametrosUnidadeEpidemiologica { array = alteracoes };
                updateJson = JsonConvert.SerializeObject(updateData);
                updateJson = Alteracao.AjustaArray(updateJson);
                updateJson = updateJson.Replace("parametroId", "id");

                var result = await ISIWebService.Instance.SendData(updateJson, "postUnidadesEpidemiologicas").ConfigureAwait(false);

                if (result.sucesso)
                {
                    result.data = result.data.Replace("unidadesEpidemiologicas", "dados");
                    result.data = Alteracao.AjustaResultData(result.data);
                    var resultIds = JsonConvert.DeserializeObject<UpdateResults>(result.data);

                    if (resultIds?.dados == null) return;

                    foreach (var resultinfo in resultIds.dados)
                    {
                        await Db.ExecuteAsync($"update UnidadeEpidemiologica set idApp={resultinfo.idApp} where id={resultinfo.idApp}").ConfigureAwait(false);

                        if (resultinfo.id != resultinfo.idApp)
                        {
                            await Db.ExecuteAsync($"update UnidadeEpidemiologica set id={resultinfo.id} where id={resultinfo.idApp}").ConfigureAwait(false);
                            await Db.ExecuteAsync($"update UnidadeEpidemiologicaParametro set unidadeEpidemiologicaId={resultinfo.id} where unidadeEpidemiologicaId={resultinfo.idApp}").ConfigureAwait(false);
                            await Db.ExecuteAsync($"update Lote set unidadeEpidemiologicaId={resultinfo.id} where unidadeEpidemiologicaId={resultinfo.idApp}").ConfigureAwait(false);
                        }
                    }

                    await Db.ExecuteAsync($"update UnidadeEpidemiologica set temmudanca=0 where temmudanca=1").ConfigureAwait(false);
                }
                else
                {
                    await SentryHelper.LogErrorAsync("Unidades Epidemiol�gicas", "UnidadeEpidemiologica", result.mensagem).ConfigureAwait(false);
                    throw new Exception(!string.IsNullOrEmpty(result.mensagem) ? result.mensagem : "Erro desconhecido ao enviar unidades epidemiol�gicas");
                }
            }
            catch (Exception ex)
            {
                await SentryHelper.LogErrorAsync("UploadUpdates UnidadeEpidemiologica", "UnidadeEpidemiologica", ex.ToString()).ConfigureAwait(false);
                throw;
            }
        }
    }

    public partial class UnidadeEpidemiologicaComDetalhes : UnidadeEpidemiologica
    {
        public string? RegionalNome { get; set; }
        public string? PropriedadeNome { get; set; }

        /// <summary>
        /// MIGRADO: Retorna a lista ao inv�s de modificar est�tico
        /// </summary>
        public static async Task<List<UnidadeEpidemiologicaComDetalhes>> PegaListaUnidadesComDetalhesAsync()
        {
            const string sql = @"
                SELECT ue.*, p.nome AS PropriedadeNome, r.nome AS RegionalNome 
                FROM UnidadeEpidemiologica ue
                LEFT OUTER JOIN Propriedade p ON p.id = ue.propriedadeId
                LEFT OUTER JOIN Regional r ON r.id = p.regionalId
                WHERE COALESCE(ue.excluido, 0) = 0 AND ue.status = 1
                ORDER BY ue.nome, r.nome, p.nome";

            return await Db.QueryAsync<UnidadeEpidemiologicaComDetalhes>(sql).ConfigureAwait(false);
        }
    }

    public class UnidadeEpidemiologicaFromWebService : UnidadeEpidemiologica
    {
        [JsonProperty("parametros")]
        public List<ParametrosValores>? parametros { get; set; }
    }

    public class UnidadesEpidemiolgicasFromWebService
    {
        [JsonProperty("unidadesEpidemiologicas")]
        public List<UnidadeEpidemiologicaFromWebService>? unidadesEpidemiologicas { get; set; }
    }
}
