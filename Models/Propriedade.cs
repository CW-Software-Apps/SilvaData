using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

using SilvaData.Utilities;

using Newtonsoft.Json;

using SQLite;

using System.Security.AccessControl;

namespace SilvaData.Models
{
    public class UpdateDataParametrosPropriedade : UpdateDataParametros
    {
        public new List<PropriedadeFromWebService> array;
    }

    public class PropriedadeParametro : ParametrosValores
    {
        public int? propriedadeId { get; set; }

        public static Task<int> SaveItemAsync(PropriedadeParametro p)
        {
            return Db.InsertOrReplaceAsync(p);
        }

        public static async Task<List<PropriedadeParametro>> GetItemsAsync(int id)
        {
            var table = await Db.Table<PropriedadeParametro>();
            return await table.Where(p => p.propriedadeId == id).ToListAsync();
        }

        internal static async Task<List<ParametrosValores>> GetItemsForUploadAsync(int? id)
        {
            var table = await Db.Table<PropriedadeParametro>();
            var parlist = await table.Where(p => p.propriedadeId == id).ToListAsync();
            var result = new List<ParametrosValores>();
            foreach (var par in parlist) result.Add(new ParametrosValores { parametroId = par.parametroId, valor = par.valor });
            return result;
        }
    }

    public class Propriedade : ObservableObject
    {
        [PrimaryKey]
        public int? id { get; set; }
        public int? idApp { get; set; }
        public string nome { get; set; }
        public int? regionalId { get; set; }
        public int? proprietarioId { get; set; }
        public int? status { get; set; }
        public DateTime dataUltimaAtualizacao { get; set; }
        public int? excluido { get; set; }
        public bool temmudanca { get; set; }

        // REMOVIDO: [Ignore] [JsonIgnore] public string RegionalNome => ...
        // REMOVIDO: public static ObservableCollection<Propriedade> PropriedadeList => DadosStatic.instance.PropriedadeList;
        // REMOVIDO: public static bool NeedRefresh = true;

        /// <summary>
        /// MIGRADO: Retorna a lista ao inv�s de modificar est�tico
        /// </summary>
        public static async Task<List<Propriedade>> PegaListaPropriedadesAsync()
        {
            var table = await Db.Table<Propriedade>();

            return await table
                .Where(p => (p.excluido == null || p.excluido != 1))
                .OrderBy(p => p.nome)
                .ToListAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// MIGRADO: Envia mensagem para CacheService atualizar
        /// </summary>
        public static async Task SalvaPropriedadeAsync(Propriedade item)
        {
            item.dataUltimaAtualizacao = DateTime.Now;
            item.temmudanca = true;
            item.status = 1;

            if (item.id != 0)
            {
                await Db.UpdateAsync(item);
            }
            else
            {
                item.id = await Alteracao.GetNextId(item);
                await Db.InsertAsync(item);
            }

            // MUDAN�A: Notifica o CacheService
            WeakReferenceMessenger.Default.Send(new RefreshCacheMessage(CacheType.Propriedades));
        }

        internal async static Task UploadUpdates()
        {
            var sql = Alteracao.SqlNovosDados("propriedade");
            var Alteracoes = await Db.QueryAsync<PropriedadeFromWebService>(sql);

            foreach (var alteracao in Alteracoes)
            {
                List<ParametrosValores> parametros = await PropriedadeParametro.GetItemsForUploadAsync(alteracao.id);
                alteracao.parametros = parametros;

                if (alteracao.idApp == 0 || alteracao.idApp == null)
                {
                    alteracao.idApp = alteracao.id;
                    alteracao.id = -1;
                }
                alteracao.status = 1;
            }

            var settings = new JsonSerializerSettings { DateFormatString = "yyyy-MM-dd HH:mm:ss" };

            var UpdateDataParametros = new UpdateDataParametrosPropriedade
            {
                array = Alteracoes
            };

            var UpdateJson = JsonConvert.SerializeObject(UpdateDataParametros, settings);
            UpdateJson = UpdateJson.Replace("propriedadeId", "propriedade");
            UpdateJson = UpdateJson.Replace("proprietarioId", "proprietario");
            UpdateJson = UpdateJson.Replace("regionalId", "regional");
            UpdateJson = Alteracao.AjustaArray(UpdateJson);

            var result = await ISIWebService.Instance.SendData(UpdateJson, "postPropriedades");

            if (result.sucesso)
            {
                result.data = result.data.Replace("propriedades", "dados");
                result.data = Alteracao.AjustaResultData(result.data);
                var resultIds = JsonConvert.DeserializeObject<UpdateResults>(result.data);

                foreach (var resultinfo in resultIds.dados)
                {
                    await Db.ExecuteAsync($"update Propriedade set idApp={resultinfo.idApp} where id={resultinfo.idApp}");

                    if (resultinfo.id != resultinfo.idApp)
                    {
                        await Db.ExecuteAsync($"update Propriedade set id={resultinfo.id} where id={resultinfo.idApp}");
                        await Db.ExecuteAsync($"update PropriedadeParametro set propriedadeId={resultinfo.id} where propriedadeId={resultinfo.idApp}");
                        await Db.ExecuteAsync($"update UnidadeEpidemiologica set propriedadeId={resultinfo.id} where propriedadeId={resultinfo.idApp}");
                    }
                }

                await Db.ExecuteAsync($"update Propriedade set temmudanca=0 where temmudanca=1");
            }
            else
            {
                await SentryHelper.LogErrorAsync(UpdateJson, "Propriedade", result.mensagem);
                throw new Exception(!string.IsNullOrEmpty(result.mensagem) ? result.mensagem : "Erro desconhecido ao enviar propriedades");
            }
        }
    }

    public class PropriedadeFromWebService : Propriedade
    {
        public List<ParametrosValores> parametros;
    }

    public class PropriedadesFromWebService
    {
        public List<PropriedadeFromWebService> propriedades;
    }
}
