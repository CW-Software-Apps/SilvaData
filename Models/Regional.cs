using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

using SilvaData.Utilities;

using Newtonsoft.Json;

using SQLite;

using System.Security.AccessControl;

namespace SilvaData.Models
{
    public class UpdateDataParametrosRegional : UpdateDataParametros
    {
        public new List<RegionalFromWebService> array;
    }

    public class Regional : ObservableObject
    {
        [PrimaryKey]
        public int? id { get; set; }
        public int? idApp { get; set; }
        public string nome { get; set; }
        public int? status { get; set; }
        public DateTime dataUltimaAtualizacao { get; set; }
        public int? excluido { get; set; }
        public bool temmudanca { get; set; }

        // REMOVIDO: public static ObservableCollection<Regional> RegionalList => DadosStatic.instance.RegionalList;
        // REMOVIDO: public static bool NeedRefresh = true;

        /// <summary>
        /// MIGRADO: Retorna a lista ao inv�s de modificar est�tico
        /// </summary>
        public static async Task<List<Regional>> PegaListaRegionaisAsync()
        {
            var table = await Db.Table<Regional>();
            return await table.Where(p => (p.excluido == null || p.excluido != 1)).OrderBy(r => r.nome).ToListAsync();
        }

        public static async Task<Regional> GetItemAsync(int regionalId)
        {
            var table = await Db.Table<Regional>();
            return await table.Where(p => p.id == regionalId).FirstOrDefaultAsync();
        }

        /// <summary>
        /// MIGRADO: Envia mensagem para CacheService atualizar
        /// </summary>
        public static async Task SaveItemAsync(Regional item)
        {
            item.dataUltimaAtualizacao = DateTime.Now;
            item.temmudanca = true;
            item.status = 1;

            if (item.id != 0)
                await Db.UpdateAsync(item);
            else
            {
                item.id = await Alteracao.GetNextId(item);
                await Db.InsertAsync(item);
            }

            // MUDAN�A: Notifica o CacheService
            WeakReferenceMessenger.Default.Send(new RefreshCacheMessage(CacheType.Regionais));
        }

        internal static async Task UploadUpdates()
        {
            var sql = Alteracao.SqlNovosDados("regional");
            var Alteracoes = await Db.QueryAsync<RegionalFromWebService>(sql);

            foreach (var alteracao in Alteracoes)
            {
                alteracao.status = 1;

                if (alteracao.idApp == 0 || alteracao.idApp == null)
                {
                    alteracao.idApp = alteracao.id;
                    alteracao.id = -1;
                }
            }

            var settings = new JsonSerializerSettings { DateFormatString = "yyyy-MM-dd HH:mm:ss" };

            var UpdateDataParametros = new UpdateDataParametrosRegional
            {
                array = Alteracoes
            };

            var UpdateJson = JsonConvert.SerializeObject(UpdateDataParametros, settings);
            UpdateJson = Alteracao.AjustaArray(UpdateJson);

            var result = await ISIWebService.Instance.SendData(UpdateJson, "postRegionais");

            if (result.sucesso)
            {
                result.data = result.data.Replace("regionais", "dados");
                result.data = Alteracao.AjustaResultData(result.data);

                var resultIds = JsonConvert.DeserializeObject<UpdateResults>(result.data);

                foreach (var resultinfo in resultIds.dados)
                {
                    await Db.ExecuteAsync($"update Regional set idApp={resultinfo.idApp} where id={resultinfo.idApp}");

                    if (resultinfo.id != resultinfo.idApp)
                    {
                        await Db.ExecuteAsync($"update Regional set id={resultinfo.id} where id={resultinfo.idApp}");
                        await Db.ExecuteAsync($"update Propriedade set regionalId={resultinfo.id} where regionalId={resultinfo.idApp}");
                    }
                }

                await Db.ExecuteAsync($"update Regional set temmudanca=0 where temmudanca=1");
            }
            else
            {
                await SentryHelper.LogErrorAsync(UpdateJson, "Regional", result.mensagem);
                throw new Exception(!string.IsNullOrEmpty(result.mensagem) ? result.mensagem : "Erro desconhecido ao enviar regionais");
            }
        }

        public override string ToString() => nome;
    }

    public class RegionalFromWebService : Regional
    {
    }

    public class RegionaisFromWebService
    {
        public List<RegionalFromWebService> regionais;
    }
}
