using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

using SilvaData.Utilities;

using Newtonsoft.Json;

using SQLite;

using System.Security.AccessControl;

namespace SilvaData.Models
{
    public class ProprietarioFromWebServiceParametro
    {
        public int parametroId { get; set; }
        public string valor { get; set; }
    }

    public class Proprietario : ObservableObject
    {
        [PrimaryKey]
        public int? id { get; set; }
        public int? idApp { get; set; }
        public string nome { get; set; }
        public int? status { get; set; }
        public DateTime dataUltimaAtualizacao { get; set; }
        public int? excluido { get; set; }
        public int temmudanca { get; set; }

        public async static Task<List<Proprietario>> PegaListaProprietarios()
        {
            var table = await Db.Table<Proprietario>();
            return await table.Where(p => (p.excluido == null || p.excluido != 1)).OrderBy(p => p.nome).ToListAsync().ConfigureAwait(false);
        }

        public async static Task<Proprietario> GetProprietarioAsync(int proprietarioId)
        {
            var table = await Db.Table<Proprietario>();
            return await table.Where(p => ((p.excluido == null || p.excluido != 1) && (p.id == proprietarioId))).FirstOrDefaultAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// MIGRADO: Retorna a lista ao inv�s de modificar est�tico
        /// </summary>
        public static async Task<List<Proprietario>> PegaListaProprietarioAsync()
        {
            var table = await Db.Table<Proprietario>();
            return await table.Where(p => (p.excluido == null || p.excluido != 1)).OrderBy(r => r.nome).ToListAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// MIGRADO: Envia mensagem para CacheService atualizar
        /// </summary>
        public async static Task SaveItemAsync(Proprietario item)
        {
            item.dataUltimaAtualizacao = DateTime.Now;
            item.temmudanca = 1;
            item.status = 1;

            if (item.id != 0)
                await Db.UpdateAsync(item).ConfigureAwait(false);
            else
            {
                item.id = await Alteracao.GetNextId(item).ConfigureAwait(false);
                await Db.InsertAsync(item).ConfigureAwait(false);
            }

            // MUDAN�A: Notifica o CacheService
            WeakReferenceMessenger.Default.Send(new RefreshCacheMessage(CacheType.Proprietarios));
        }

        internal static async Task UploadUpdates()
        {
            var sql = Alteracao.SqlNovosDados("proprietario");
            var Alteracoes = await Db.QueryAsync<RegionalFromWebService>(sql);

            foreach (var alteracao in Alteracoes)
            {
                alteracao.status = 1;

                if ((alteracao.temmudanca) && (alteracao.idApp == 0 || alteracao.idApp == null))
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

            var result = await ISIWebService.Instance.SendData(UpdateJson, "postProprietarios");

            if (result.sucesso)
            {
                result.data = result.data.Replace("proprietarios", "dados");
                result.data = Alteracao.AjustaResultData(result.data);

                var resultIds = JsonConvert.DeserializeObject<UpdateResults>(result.data);

                foreach (var resultinfo in resultIds.dados)
                {
                    await Db.ExecuteAsync($"update Proprietario set idApp={resultinfo.idApp} where id={resultinfo.idApp}");

                    if (resultinfo.id != resultinfo.idApp)
                    {
                        await Db.ExecuteAsync($"update Proprietario set id={resultinfo.id} where id={resultinfo.idApp}");
                        await Db.ExecuteAsync($"update Propriedade set proprietarioId={resultinfo.id} where proprietarioId={resultinfo.idApp}");
                    }
                }

                await Db.ExecuteAsync($"update Proprietario set temmudanca=0 where temmudanca=1");
            }
            else
            {
                await SentryHelper.LogErrorAsync(UpdateJson, "Proprietario", result.mensagem);
                throw new Exception(!string.IsNullOrEmpty(result.mensagem) ? result.mensagem : "Erro desconhecido ao enviar propriet�rios");
            }
        }

        public override string ToString() => nome;
    }

    public class ProprietarioFromWebService : UnidadeEpidemiologica
    {
        public List<ProprietarioFromWebServiceParametro> parametros;
    }

    public class ProprietariosFromWebService
    {
        public List<ProprietarioFromWebService> proprietarios;
    }
}
