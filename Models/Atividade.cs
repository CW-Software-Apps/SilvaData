using CommunityToolkit.Mvvm.ComponentModel;

using SilvaData.Utils;
using Newtonsoft.Json;

using SQLite;

using System.Collections.ObjectModel;
using System.ComponentModel;


namespace SilvaData.Models
{

    public class UpdateDataParametrosAtividade : UpdateDataParametros
    {
        public new List<AtividadeFromWebService> array;
    }


    public partial class AtividadeOutroUsuario : ObservableObject
    {
        private int? _usuarioId;
        public int? usuarioId
        {
            get => _usuarioId;
            set => SetProperty(ref _usuarioId, value);
        }

        private int? _atividadeId;
        public int? atividadeId
        {
            get => _atividadeId;
            set => SetProperty(ref _atividadeId, value);
        }
    }


    public enum AtividadeNotificacaoTipo
    {
        Dias,
        Horas
    }

    public class AtividadeNotificacao
    {
        public int Qtde { get; set; }
        public string Tipo { get; set; }
    }


    public class Atividade : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        [PrimaryKey]
        public int? id { get; set; }

        public int? idApp { get; set; }

        public string titulo { get; set; }
        public string descricao { get; set; }

        [JsonIgnore][Ignore] public DateTime dataHoraPrazoParaCalendario => (DateTime)dataHoraInicio;



        //Define o status da atividade.
        // 0: Cancelado
        // 1: Aguardando
        // 2: Finalizado
        public int? atividadeStatus { get; set; }

        public int? atividadeTipoData { get; set; }

        public int? atividadeTipoId { get; set; }

        public int? unidadeEpidemiologicaId { get; set; }

        public int? usuarioResponsavelId { get; set; }

        public DateTime dataUltimaAtualizacao { get; set; }

        //Datas Convertidas para N�o Serem NULL
        public DateTime dataInicio { get; set; }
        public TimeSpan horaInicio { get; set; }
        public DateTime? dataHoraInicio { get; set; }
        public DateTime dataPrazo { get; set; }
        public TimeSpan horaPrazo { get; set; }

        public DateTime? dataHoraPrazo { get; set; }


        public async static Task SalvaAtividade(Atividade atividade)
        {
            atividade.dataUltimaAtualizacao = DateTime.Now;
            atividade.temmudanca = true;

            atividade.dataHoraInicio = new DateTime(atividade.dataInicio.Year, atividade.dataInicio.Month, atividade.dataInicio.Day, atividade.horaInicio.Hours, atividade.horaInicio.Minutes, 0);
            atividade.dataHoraPrazo = new DateTime(atividade.dataPrazo.Year, atividade.dataPrazo.Month, atividade.dataPrazo.Day, atividade.horaPrazo.Hours, atividade.horaPrazo.Minutes, 0);

            if (atividade.id != 0)
                await Db.UpdateAsync(atividade);
            else
            {
                atividade.id = await Alteracao.GetNextId(atividade);
                await Db.InsertAsync(atividade);
            }
        }

        public static async Task<Atividade> PegaAtividade(int idAtividade)
        {
            Console.WriteLine($"Pega Atividade - {MainThread.IsMainThread}");

            var tabela = await Db.Table<Atividade>();

            var atividade = await tabela.FirstOrDefaultAsync(p => p.id == idAtividade).ConfigureAwait(false);

            if (atividade.dataHoraPrazo == null)
            {
                atividade.dataHoraPrazo = DateTime.MinValue;
                atividade.horaPrazo = new TimeSpan(0, 0, 0);
            }

            return atividade;
        }


        public int? excluido { get; set; }

        public bool temmudanca { get; set; }

        [JsonIgnore] public bool JaVenceu => atividadeStatus == 1 && dataHoraPrazo <= DateTime.Now;
        [JsonIgnore] public bool EmAndamento => atividadeStatus == 1 && (dataHoraInicio != null && dataHoraInicio <= DateTime.Now && (dataHoraPrazo == null || dataHoraPrazo == DateTime.MinValue || dataHoraPrazo > DateTime.Now));


        [JsonIgnore]
        public string AtividadeStatus
        {
            get
            {
                switch (atividadeStatus)
                {
                    case 0: //Cancelado
                        return Traducao.AtividadeStatus_Cancelada;

                    case 1:
                        if (JaVenceu)
                        {
                            return Traducao.AtividadeStatus_Atrasada;
                        }

                        if (EmAndamento)
                        {
                            return Traducao.AtividadeStatus_EmAndamento;
                        }

                        return "Pendente";

                    case 2: //Finalizado
                        return Traducao.AtividadeStatus_Finalizada;
                }

                return "";
            }
        }

        [JsonIgnore]
        public Color AtividadeStatusColor
        {
            get
            {
                switch (atividadeStatus)
                {
                    case 0: //Cancelado
                        return Colors.DimGray;

                    case 1:
                        if (JaVenceu) //Aguardando
                        {
                            return Color.FromArgb("#FA4926");
                        }

                        if (EmAndamento)
                        {
                            return Color.FromArgb("#FEB92B");
                        }

                        return Color.FromArgb("#5FAFD9");


                    case 2: //Finalizado
                        return Color.FromArgb("#5FAFD9");
                }

                return Colors.Black;
            }
        }

        internal static async Task UploadUpdates()
        {
            var sql = Alteracao.SqlNovosDados("atividade");
            var Alteracoes = await Db.QueryAsync<AtividadeFromWebService>(sql);

            if (Alteracoes.Count <= 0)
                return;

            foreach (var alteracao in Alteracoes)
            {
                if (alteracao.idApp == 0 || alteracao.idApp == null)
                {
                    if (alteracao.atividadeStatus == null) alteracao.atividadeStatus = 1;
                    alteracao.atividadeTipoData = 1;
                    alteracao.atividadeTipoId = 1;
                    alteracao.usuarioResponsavelId = int.Parse(ISIWebService.Instance.LoggedUser.id);

                    alteracao.idApp = alteracao.id;
                    alteracao.id = -1; //Novo Registro
                }

            }

            var settings = new JsonSerializerSettings { DateFormatString = "yyyy-MM-dd" };

            //Arruma Diferan�a nos Nomes dos Campos
            var UpdateDataParametros = new UpdateDataParametrosAtividade
            {
                array = Alteracoes
            };

            //Request
            var UpdateJson = JsonConvert.SerializeObject(UpdateDataParametros, settings);
            UpdateJson = Alteracao.AjustaArray(UpdateJson);
            UpdateJson = UpdateJson.Replace("atividadeTipoId", "atividadeTipo");
            UpdateJson = UpdateJson.Replace("usuarioResponsavelId", "usuarioResponsavel");
            UpdateJson = UpdateJson.Replace("unidadeEpidemiologicaId", "unidadeEpidemiologica");

            //Envia Dados
            var result = await ISIWebService.Instance.SendData(UpdateJson, "postAtividades");

            if (result.sucesso)
            {
                //Atualiza Keys com Valores do Server
                result.data = result.data.Replace("atividades", "dados");
                result.data = Alteracao.AjustaResultData(result.data);

                var resultIds = JsonConvert.DeserializeObject<UpdateResults>(result.data);


                foreach (var resultinfo in resultIds.dados)
                {
                    await Db.ExecuteAsync($"update Atividade set idApp={resultinfo.idApp} where id={resultinfo.idApp}");


                    if (resultinfo.id != resultinfo.idApp) await Db.ExecuteAsync($"update Atividade set id={resultinfo.id} where id={resultinfo.idApp}");
                }

                //Limpa Atualiza��es
                await Db.ExecuteAsync($"update Atividade set temmudanca=0 where temmudanca=1");
            }
            else
            {
                await SentryHelper.LogErrorAsync(UpdateJson, "Atividade", result.mensagem);
                throw new Exception(!string.IsNullOrEmpty(result.mensagem) ? result.mensagem : "Erro desconhecido ao enviar atividades");
            }
        }

        [Ignore]
        [JsonIgnore]
        public ObservableCollection<AtividadeNotificacao> Notificacoes { get; set; } = new ObservableCollection<AtividadeNotificacao>();

        [JsonIgnore]
        public static int TotalPendentes = 0;
    }

    public class AtividadeUsuario
    {
        public int? usuarioId;
    }

    public class AtividadeFromWebService : Atividade
    {
        public List<AtividadeUsuario> outrosUsuarios;
    }

    public class AtividadesFromWebService
    {
        public List<AtividadeFromWebService> Atividades;
    }

    public class AtividadeComDetalhes : Atividade
    {
        public string TipoAtividadeNome { get; set; }
        public string Cor { get; set; }
        public string UnidadeEpidemiologicaNome { get; set; }

        [Ignore]
        public Color Color => Cor switch
        {
            "Amarelo" => Colors.Yellow,
            "Vermelho" => Colors.Red,
            "Roxo" => Colors.Purple,
            "Verde" => Colors.Green,
            "Rosa" => Colors.Pink,
            "Laranja" => Colors.Orange,
            "Preto" => Colors.Black,
            "Cinza" => Colors.Gray,
            "Marrom" => Colors.Maroon,
            "P�rpura" => Colors.MediumPurple,
            "Azul Escuro" => Colors.DarkBlue,
            "Verde Escuro" => Colors.DarkGreen,
            _ => Colors.Black,
        };

        [Ignore]
        [JsonIgnore]
        public string DataHoraInicioPrazo => $"{dataHoraInicio:dd/MM HH:mm} {Traducao.At�} {dataHoraPrazo:dd/MM HH:mm}";

        public string DescricaoTitulo => $"{titulo}\n{descricao}";

        public static async Task<List<AtividadeComDetalhes>> PegaAtividadeComDetalhesAsync()
        {
            var dados = (await Db.QueryAsync<AtividadeComDetalhes>(
            "select a.*,uep.nome as UnidadeEpidemiologicaNome,ta.nome as TipoAtividadeNome,tca.nome as Cor from Atividade a " +
            "left outer join UnidadeEpidemiologica uep on uep.id=a.unidadeEpidemiologicaId " +
            "left outer join TipoAtividade ta on ta.id=a.atividadeTipoId " +
            "left outer join TipoCorAtividade tca on tca.id= ta.atividadeTipoCorId " +
            "where dataHoraInicio is not null").ConfigureAwait(false)).ToList();
            TotalPendentes = dados.Count(a => a.JaVenceu || a.EmAndamento);
            return dados;
        }

    }
}
