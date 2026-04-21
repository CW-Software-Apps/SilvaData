using Newtonsoft.Json;

using SQLite;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace SilvaData.Models
{
    public class ListaModelosIsiMacroFromWeb
    {
        [JsonProperty("modeloIsiMacro")]
        public List<ModeloIsiMacroComParametros> ModelosIsiMacro { get; set; }
    }



    public class ModeloIsiMacro
    {
        [PrimaryKey]
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("nomeModelo")]
        public string NomeModelo { get; set; }

        internal static async Task<List<ModeloIsiMacroComParametros>> PegaModelosIsiMacroComParametrosAsync()
        {
            var table = await Db.Table<ModeloIsiMacro>().ConfigureAwait(false);

            // Busca todos os registros de ModeloIsiMacro no banco de dados.
            var modelos = await table.ToListAsync().ConfigureAwait(false);

            // Lista para armazenar os modelos com seus par�metros
            var listaModelosComParametros = new List<ModeloIsiMacroComParametros>();

            // Itera sobre cada modelo para buscar os par�metros associados
            foreach (var modelo in modelos)
            {
                // Consulta para obter os par�metros relacionados ao modelo atual
                var parametros = await Db.QueryAsync<Parametro>(
                    "SELECT p.* FROM Parametro p " +
                    "INNER JOIN ModeloIsiMacroParametro mp ON p.id = mp.ParametroId " +
                    "WHERE mp.ModeloIsiMacroId = ?", modelo.Id).ConfigureAwait(false);

                // Cria um novo objeto combinando o modelo e seus par�metros
                var modeloComParametros = new ModeloIsiMacroComParametros
                {
                    Id = modelo.Id,
                    NomeModelo = modelo.NomeModelo,
                    Parametros = parametros
                };

                // Adiciona o objeto � lista de resultados
                listaModelosComParametros.Add(modeloComParametros);
            }

            // Retorna a lista com os modelos e seus respectivos par�metros
            return listaModelosComParametros;
        }
    }

    // Tabela de jun��o para relacionamento many-to-many (caso seja necess�rio)
    [Table("ModeloIsiMacroParametro")]
    public class ModeloIsiMacroParametro
    {
        [Indexed]
        public int ModeloIsiMacroId { get; set; }

        [Indexed]
        public int ParametroId { get; set; }
    }

    //Clase pro JSON da Web e para Pegar a Lista com Parametros
    public class ModeloIsiMacroComParametros : ModeloIsiMacro
    {
        [JsonProperty("parametros")]
        public List<Parametro> Parametros { get; set; }
    }



}



