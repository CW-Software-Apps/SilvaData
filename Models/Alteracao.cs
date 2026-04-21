using SilvaData.Utils;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SilvaData.Models
{
    public class UpdateResult
    {
        public int id;
        public int idApp;
    }

    public class UpdateResults
    {
        public List<UpdateResult> dados;
    }

    public class UpdateDataParametros
    {
        public string usuario; //id do usu�rio
        public string dispositivoId; //identificador do dispositivo de sess�o
        public string session; //id da sess�o (que � retornado no login)
        public string array;

        public UpdateDataParametros()
        {
            usuario = ISIWebService.Instance.LoggedUser.id;
            session = ISIWebService.Instance.LoggedUser.session;
            dispositivoId = Preferences.Get("my_id", string.Empty);
        }
    }

    public class Alteracao : ObservableObject
    {
        public string Tabela { get; set; }
        public string TabelaTexto { get; set; }
        public int Qtde { get; set; }


        public static async Task<bool> TemAlgumaAlteracao()
        {
            try
            {
                if ((await TotalAlteracoesTabela("Regional", "")).Qtde > 0) return true;
                if ((await TotalAlteracoesTabela("Atividade", "")).Qtde > 0) return true;
                if ((await TotalAlteracoesTabela("Propriedade", "")).Qtde > 0) return true;
                if ((await TotalAlteracoesTabela("UnidadeEpidemiologica", "")).Qtde > 0) return true;
                if ((await TotalAlteracoesTabela("Lote", "")).Qtde > 0) return true;
                if ((await TotalAlteracoesTabela("LoteVisita", "")).Qtde > 0) return true;
                if ((await TotalAlteracoesTabela("LoteForm", "")).Qtde > 0) return true;
            }
            catch (Exception)
            {
                return false;
                throw;
            }

            return false;
        }

        public static async Task<Alteracao> TotalAlteracoesTabela(string Tabela, string filtroAdicional)
        {
            var sql = $"select Count(*) Qtde, \"{Tabela}\" as Tabela from {Tabela} WHERE {Tabela}.temmudanca=1";

            if (!string.IsNullOrEmpty(filtroAdicional)) sql += $" {filtroAdicional}";

            var alteracao = await Db.QueryAsync<Alteracao>(sql);
            return alteracao.Count > 0 ? alteracao[0] : null;
        }

        public static string SqlNovosDados(string Tabela)
        {
            var sql = $"select * from {Tabela} WHERE {Tabela}.temmudanca=1";
            return sql;
        }

        internal static string AjustaArray(string array)
        {
            array = array.Replace("\"id\":-1,", "\"id\":\"\",");
            array = array.Replace(",\"temmudanca\":true", "");


            array = array.Replace("null", "\"\"");
            return array;
        }

        internal static string AjustaResultData(string data)
        {
            data = data.Replace(".00", "");
            data = data.Replace(".0", "");
            return data;
        }


        internal async static Task<int> GetNextId(object table)
        {
            var tabela = table.GetType().ToString();
            tabela = tabela.Replace("SilvaData.Models.", "");

            var id = await Db.FindWithQueryAsync<MaxId>($"select max(id)+1 as maxid from {tabela}");
            if (id.maxid < 50000)
                id.maxid = 50000;
            else
                id.maxid += 1;

            return id.maxid;
        }
    }

    public class MaxId
    {
        public int maxid { get; set; }
    }
}
