using SilvaData.Utils;
using Newtonsoft.Json;
using Plugin.LocalNotification;
using SQLite;
using System.Diagnostics;
using System.Threading;
using Microsoft.Maui.ApplicationModel;
using Plugin.LocalNotification.Core.Models; // Para MainThread

namespace SilvaData.Models
{
    public class Notificacao
    {
        [PrimaryKey]
        [AutoIncrement] public int idBD { get; set; }
        public int id { get; set; }
        public int idApp { get; set; }
        public string titulo { get; set; }
        public string descricao { get; set; }
        public DateTime? dataHora { get; set; }
        public DateTime? dataHoraArquivado { get; set; }
        public DateTime dataUltimaAtualizacao { get; set; }
        public bool temmudanca { get; set; } = false;

        public static async Task<int> SalvaNotificacao(Notificacao notificacao)
        {
            notificacao.dataUltimaAtualizacao = DateTime.Now;
            notificacao.temmudanca = true;
            if (notificacao.id != 0)
                return await Db.UpdateAsync(notificacao);
            notificacao.id = await Alteracao.GetNextId(notificacao);
            return await Db.InsertAsync(notificacao);
        }

        internal static async Task<List<Notificacao>> PegaNotificacoesAtivas()
        {
            var table = await Db.Table<Notificacao>();
            var notificacoes = await table.Where(n => n.dataHoraArquivado == null).ToListAsync().ConfigureAwait(false);
            TotalPendentes = notificacoes.Count(n => n.dataHora <= DateTime.Now);
            return notificacoes;
        }

        internal static async Task UploadUpdates()
        {
            var sql = Alteracao.SqlNovosDados("notificacao");
            var alteracoes = await Db.QueryAsync<Notificacao>(sql);

            if (alteracoes.Count <= 0)
                return;

            foreach (var notificacao in alteracoes)
            {
                if (notificacao.idApp == 0)
                {
                    notificacao.idApp = notificacao.id;
                    notificacao.id = -1; // novo registro
                }
            }
            var settings = new JsonSerializerSettings { DateFormatString = "yyyy-MM-dd HH:mm:ss" };
            var updateData = new UpdateDataParametrosNotificacao { array = alteracoes };
            var updateJson = JsonConvert.SerializeObject(updateData, settings);
            updateJson = Alteracao.AjustaArray(updateJson);
            var result = await ISIWebService.Instance.SendData(updateJson, "postNotificacoes");
            if (result.sucesso)
            {
                result.data = result.data.Replace("notificacoes", "dados");
                result.data = Alteracao.AjustaResultData(result.data);
                var resultIds = JsonConvert.DeserializeObject<UpdateResults>(result.data);
                foreach (var info in resultIds.dados)
                    await Db.ExecuteAsync($"update Notificacao set idApp={info.idApp} where id={info.idApp}");
                await Db.ExecuteAsync("update Notificacao set temmudanca=0 where temmudanca=1");
            }
            else
            {
                await SentryHelper.LogErrorAsync(updateJson, "Notificacao", result.mensagem);
                throw new Exception(!string.IsNullOrEmpty(result.mensagem) ? result.mensagem : "Erro desconhecido ao enviar notifica��es");
            }
        }

        /// <summary>Cria notifica��es locais para os itens agendados.</summary>
        public static async Task CreateNotificationsAsync()
        {
            try
            {
                LocalNotificationCenter.Current.CancelAll();
                var temPermissao = await EnsureNotificationPermissionAsync();
                if (!temPermissao)
                {
                    Debug.WriteLine("Notifica��es: permiss�o negada. Agendamento cancelado.");
                    return;
                }
                var notificacoes = await PegaNotificacoesAtivas();
                var futuras = notificacoes.Where(n => n.dataHora.HasValue && n.dataHora > DateTime.Now).ToList();
                Debug.WriteLine($"Agendando {futuras.Count} notifica��es");
                int idx = 0;
                foreach (var n in futuras)
                {
                    try
                    {
                        await AgendarNotificacao(n);
                        if (++idx % 25 == 0)
                            await Task.Yield();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Erro ao agendar notifica��o (idBD:{n.idBD}): {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao criar notifica��es: {ex.Message}");
            }
        }

        private static async Task<bool> EnsureNotificationPermissionAsync()
        {
            try
            {
                var habilitado = await LocalNotificationCenter.Current.AreNotificationsEnabled();
                if (habilitado) return true;
                habilitado = await MainThread.InvokeOnMainThreadAsync(() => LocalNotificationCenter.Current.RequestNotificationPermission());
                return habilitado;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Permiss�o de notifica��o falhou: {ex.Message}");
                return false;
            }
        }

        private static Task AgendarNotificacao(Notificacao notificacao)
        {
            if (!notificacao.dataHora.HasValue)
            {
                Debug.WriteLine($"ERRO: Notifica��o (idBD:{notificacao.idBD}) sem dataHora v�lida.");
                return Task.CompletedTask;
            }
            var notification = new NotificationRequest
            {
                NotificationId = notificacao.idBD,
                Title = notificacao.titulo,
                Description = notificacao.descricao,
                Schedule = new NotificationRequestSchedule { NotifyTime = notificacao.dataHora.Value }
            };
            return MainThread.InvokeOnMainThreadAsync(() => LocalNotificationCenter.Current.Show(notification));
        }

        [JsonIgnore] public static int TotalPendentes = 0;
    }

    public class NotificacaoListFromWebService { public List<Notificacao> notificacoes; }
    public class UpdateDataParametrosNotificacao : UpdateDataParametros { public new List<Notificacao> array; }
}
