using SilvaData.Utilities;

using Newtonsoft.Json;

namespace SilvaData.Models
{

    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Messaging;

    using Microsoft.Maui.Storage;

    using SQLite;

    using System.Collections.ObjectModel;

    /// <summary>
    /// Formulário em andamento com dados do lote.
    /// Agregador de LoteForm + Parâmetros + Avaliações do galpão.
    /// </summary>
    public partial class LoteFormulario : ObservableObject
    {
            [ObservableProperty]
            private LoteForm? loteForm;

            [ObservableProperty]
            private ObservableCollection<ParametroComAlternativas> formulario_ParametrosComAlternativas = new();

            [ObservableProperty]
            private List<LoteFormAvaliacaoGalpao> listaAvaliacoesGalpao = new();

            [ObservableProperty]
            private ModeloIsiMacroComParametros? modeloIsiMacroSelecionado;

            [ObservableProperty]
            private Parametro? parametroSelecionado;

            /// <summary>
            /// Apaga o formulário em andamento.
            /// </summary>
            public void ApagaEmAndamento()
            {
                Preferences.Set("FormularioEmAndamento", "");
            }

            /// <summary>
            /// Calcula avaliações qualitativas realizadas.
            /// </summary>
            public int AvaliacoesRealizadasQualitativa
            {
                get
                {
                    try
                    {
                        // Optimized: ListaAvaliacoesGalpao is already a List, no need for .ToList()
                        return ListaAvaliacoesGalpao?.Count(a => a.TemAlternativaSelecionada) ?? 0;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Erro ao calcular AvaliacoesRealizadasQualitativa: {ex.Message}");
                        return 0;
                    }
                }
            }

            /// <summary>
            /// Calcula avaliações quantitativas realizadas.
            /// </summary>
            public int AvaliacoesRealizadasQuantitativa
            {
                get
                {
                    try
                    {
                        // Optimized: ListaAvaliacoesGalpao is already a List, no need for .ToList()
                        return ListaAvaliacoesGalpao?.Count(a => a.RespostaQtde.HasValue) ?? 0;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Erro ao calcular AvaliacoesRealizadasQuantitativa: {ex.Message}");
                        return 0;
                    }
                }
            }

            /// <summary>
            /// Calcula a média das avaliações quantitativas.
            /// </summary>
            public double AvaliacaoMediaQuantitativa
            {
                get
                {
                    try
                    {
                        if (ListaAvaliacoesGalpao == null || !ListaAvaliacoesGalpao.Any())
                            return 0;

                        // Optimized: Removed unnecessary .ToList() calls - already a List<T>
                        var avaliacaoComResposta = ListaAvaliacoesGalpao.Where(a => a.RespostaQtde.HasValue);

                        if (!avaliacaoComResposta.Any())
                            return 0;

                        return avaliacaoComResposta.Average(a => a.RespostaQtde.GetValueOrDefault());
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Erro ao calcular AvaliacaoMediaQuantitativa: {ex.Message}");
                        return 0;
                    }
                }
            }

            /// <summary>
            /// Notifica que os dados foram atualizados.
            /// </summary>
            public void AtualizouDados()
            {
                OnPropertyChanged(nameof(AvaliacoesRealizadasQuantitativa));
                OnPropertyChanged(nameof(AvaliacaoMediaQuantitativa));
                OnPropertyChanged(nameof(AvaliacoesRealizadasQualitativa));
            }

            /// <summary>
            /// Salva em andamento.
            /// </summary>
            public async Task SalvaEmAndamento()
            {
                if (LoteForm == null) return;

                try
                {
                    var json = Newtonsoft.Json.JsonConvert.SerializeObject(this);
                    Preferences.Set("FormularioEmAndamento", json);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erro ao salvar em andamento: {ex.Message}");
                }

                await Task.CompletedTask;
            }

        }

    /// <summary>
    /// Classe para atualizar dados de parâmetros do lote.
    /// </summary>
    public class UpdateDataParametrosLoteForm : UpdateDataParametros
    {
        public new List<LoteFormFromWebService> array;
    }

    /// <summary>
    /// Parâmetros preenchidos do formulário do lote.
    /// </summary>
    public class LoteFormParametro
    {
        [Indexed] public int? parametroId { get; set; }
        [Indexed] public int? LoteFormId { get; set; }
        public string? valor { get; set; } // ✅ MUDANÇA: nullable

        public static async Task<List<LoteFormParametro>> ParametroPreenchidoFormularioLote(int? loteFormId)
        {
            var table = await Db.Table<LoteFormParametro>();
            return await table.Where(p => p.LoteFormId == loteFormId).ToListAsync();
        }

        public static async Task<int> SaveItemAsync(LoteFormParametro lp)
        {
            return await Db.InsertOrReplaceAsync(lp);
        }

        internal static async Task<List<ParametrosValores>> GetItemsForUploadAsync(int? id)
        {
            var table = await Db.Table<LoteFormParametro>();
            var parlist = await table.Where(p => p.LoteFormId == id).ToListAsync();
            var result = new List<ParametrosValores>();

            foreach (var par in parlist)
                if (!string.IsNullOrEmpty(par.valor) && result.Count(p => p.parametroId == par.parametroId) <= 0)
                    result.Add(new ParametrosValores { parametroId = par.parametroId, valor = par.valor });

            return result;
        }
    }

    /// <summary>
    /// Diagnóstico do lote.
    /// </summary>
    public class LoteFormDiagnostico
    {
        public string? Diagnostico { get; set; } // ✅ MUDANÇA: nullable
    }

    /// <summary>
    /// Formulários preenchidos do lote (migrado para MAUI).
    /// </summary>
    public class LoteForm : ObservableObject
    {
        private DateTime _data;

        [Ignore]
        [JsonIgnore]
        public int FaseAsDays => loteFormFase switch
        {
            1 => 7,
            2 => 14,
            3 => 21,
            4 => 28,
            5 => 35,
            6 => 42,
            _ => 0
        };

        /// <summary>
        /// Calcula a idade do lote (em dias) com base na data do formulário e data de início do lote.
        /// Requer que o Lote esteja carregado.
        /// </summary>
        [Ignore]
        [JsonIgnore]
        public string IdadeLote
        {
            get
            {
                if (loteId == null) return string.Empty;

                // Busca síncrona do cache (se disponível) ou assíncrona
                var lote = Lote.GetCachedLote((int)loteId);
                if (lote == null || lote.dataInicio == null) return string.Empty;

                var totalDays = Math.Round((data - lote.dataInicio.Value).TotalDays);
                return $"{totalDays} {Traducao.Dias}";
            }
        }

        /// <summary>
        /// Versão assíncrona para obter IdadeLote (caso o lote não esteja em cache).
        /// </summary>
        public async Task<string> GetIdadeLoteAsync()
        {
            if (loteId == null) return string.Empty;

            var lote = await Lote.PegaLoteAsync((int)loteId);
            if (lote?.dataInicio == null) return string.Empty;

            var totalDays = Math.Round((data - lote.dataInicio.Value).TotalDays);
            return $"{totalDays} {Traducao.Dias}";
        }

        [PrimaryKey][AutoIncrement] public int DBId { get; set; }
        public int? id { get; set; }
        public int? idApp { get; set; }
        public int? loteFormVinculado { get; set; }
        public int? item { get; set; }

        public DateTime data
        {
            get => _data;
            set
            {
                if (value.Equals(_data)) return;
                _data = value;
                OnPropertyChanged();
            }
        }

        public DateTime? dataInicioPreenchimento { get; set; }
        public DateTime? dataTerminoPreenchimento { get; set; }
        public double latitude { get; set; }
        public double longitute { get; set; } 
        public int? parametroTipoId { get; set; }
        public int? loteFormFase { get; set; }
        public int? loteVisita { get; set; }
        public int? loteId { get; set; }
        public DateTime dataUltimaAtualizacao { get; set; }
        private string? _observacoes;
        public string? observacoes 
        { 
            get => _observacoes;
            set
            {
                if (_observacoes == value) return;
                _observacoes = value;
                OnPropertyChanged();
            }
        }
        public bool temmudanca { get; set; }
        public int? modeloisimacro { get; set; }
        public int? excluido { get; set; }

        /// <summary>
        /// Busca um formulário específico do lote.
        /// </summary>
        public static async Task<LoteForm?> PegaFormulariosLote(int loteId, int parametroTipo, int? loteFormFase, int? item = null)
        {
            var table = await Db.Table<LoteForm>();

            return item != null
                ? await table.FirstOrDefaultAsync(lf => lf.loteId == loteId
                    && lf.parametroTipoId == parametroTipo
                    && lf.loteFormFase == loteFormFase
                    && lf.item == item)
                : await table.FirstOrDefaultAsync(lf => lf.loteId == loteId
                    && lf.parametroTipoId == parametroTipo
                    && lf.loteFormFase == loteFormFase);
        }

        /// <summary>
        /// Busca lista de formulários do lote.
        /// </summary>
        public static async Task<List<LoteForm>> PegaListaFormulariosLoteList(int loteId, int parametroTipo, int? loteFormFase = null, bool filtrarFaseNula = false)
        {
            try
            {
                var table = await Db.Table<LoteForm>().ConfigureAwait(false);

                var query = table.Where(lf => lf.loteId == loteId && lf.parametroTipoId == parametroTipo);

                if (loteFormFase.HasValue)
                {
                    query = query.Where(lf => lf.loteFormFase == loteFormFase);
                }
                else if (filtrarFaseNula)
                {
                    query = query.Where(lf => lf.loteFormFase == null);
                }

                return await query.OrderByDescending(lf => lf.item).ThenByDescending(lf => lf.data).ToListAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoteForm] PegaListaFormulariosLoteList falhou | lote={loteId} | tipo={parametroTipo} | fase={(loteFormFase?.ToString() ?? "null")} | erro={ex}");
                return new List<LoteForm>();
            }
        }

        /// <summary>
        /// Pega diagnóstico do lote.
        /// </summary>
        public static async Task<LoteFormDiagnostico?> PegaDiagnosticoLoteForm(int? loteFormID)
        {
            if (loteFormID == null) return null;

            const string sql = @"
                SELECT LF.*, group_concat(p.nome, ' - ') AS Diagnostico 
                FROM LoteForm LF
                LEFT OUTER JOIN LoteFormParametro lfp ON lfp.LoteFormId = LF.id AND lfp.valor = 1
                LEFT OUTER JOIN Parametro p ON p.id = lfp.parametroId
                WHERE LF.id = ? AND LF.parametroTipoId = 13
                GROUP BY LF.id";

            return (await Db.QueryAsync<LoteFormDiagnostico>(sql, loteFormID)).FirstOrDefault();
        }

        /// <summary>
        /// Pega formulários de uma visita.
        /// </summary>
        public static async Task<List<LoteForm>> GetItemsFromVisitaAsync(int loteId, int loteVisita)
        {
            var table = await Db.Table<LoteForm>();
            return await table
                .Where(lf => lf.loteId == loteId && lf.loteVisita == loteVisita)
                .ToListAsync();
        }

        /// <summary>
        /// Pega formulário por ID.
        /// </summary>
        public static async Task<LoteForm?> PegaFormulariosLotePorLoteFormId(int loteFormId)
        {
            var table = await Db.Table<LoteForm>();
            return await table.FirstOrDefaultAsync(lf => lf.id == loteFormId);
        }

        /// <summary>
        /// Conta formulários vinculados.
        /// </summary>
        public static async Task<int> TotalVinculados(int? loteFormId)
        {
            var table = await Db.Table<LoteForm>();
            return await table.Where(lf => lf.loteFormVinculado == loteFormId).CountAsync();
        }

        /// <summary>
        /// Salva formulário do lote.
        /// </summary>
        public static async Task<int> SalvaLoteFormularioAsync(LoteForm loteform)
        {
            if (loteform == null) return 0;

            loteform.dataUltimaAtualizacao = DateTime.Now;
            loteform.temmudanca = true;

            if (loteform.id > 0)
                return await Db.UpdateAsync(loteform);

            loteform.id = await Alteracao.GetNextId(loteform);
            return await Db.InsertAsync(loteform);
        }

        /// <summary>
        /// Faz upload dos formulários atualizados (lógica mantida do original).
        /// </summary>
        internal static async Task FazUploadLoteFormsAtualizados()
        {
            const string sql = @"
                select * from LoteForm l 
                WHERE l.temmudanca=1 
                and not exists (select * from loteVisita lv where lv.lote=l.loteId and lv.loteVisitaStatus=1)";

            var listalotesFormsComAlteracoes = await Db.QueryAsync<LoteFormFromWebService>(sql);

            if (listalotesFormsComAlteracoes.Count <= 0) return;

            foreach (var alteracao in listalotesFormsComAlteracoes)
            {
                try
                {
                    // Pega parâmetros preenchidos
                    var parametros = await LoteFormParametro.GetItemsForUploadAsync(alteracao.id);
                    alteracao.parametros = parametros;

                    // ===== AVALIAÇÕES DO GALPÃO =====
                    var avaliacoes = await LoteFormAvaliacaoGalpao.RespostasAvaliacaoGalpaoPorLote(alteracao.id);
                    var ehAvaliacaoDeGalpao = avaliacoes.Count > 0;

                    if (ehAvaliacaoDeGalpao)
                    {
                        alteracao.loteFormParametroGalpao = new List<LoteFormParametroGalpaoDoWebService>();
                        var primeiraAvaliacao = avaliacoes[0];
                        var tipoPreenchimento = primeiraAvaliacao.RespostaQtde.HasValue ? "8" : "7";

                        alteracao.parametros = new List<ParametrosValores>
                        {
                            new() { parametroId = primeiraAvaliacao.parametroId, tipoPreenchimento = tipoPreenchimento }
                        };

                        foreach (var avaliacao in avaliacoes)
                        {
                            if (avaliacao.RespostaQtde.HasValue)
                            {
                                alteracao.loteFormParametroGalpao.Add(new LoteFormParametroGalpaoDoWebService
                                {
                                    tipoPreenchimento = "8",
                                    numeroResposta = avaliacao.NumeroResposta,
                                    respostaQtde = avaliacao.RespostaQtde,
                                    alternativaId = avaliacao.AlternativaIds.Count > 0 ? avaliacao.AlternativaIds[0] : 0
                                });
                            }
                            else
                            {
                                foreach (var alternativaId in avaliacao.AlternativaIds)
                                {
                                    alteracao.loteFormParametroGalpao.Add(new LoteFormParametroGalpaoDoWebService
                                    {
                                        tipoPreenchimento = "7",
                                        numeroResposta = avaliacao.NumeroResposta,
                                        respostaQtde = 0,
                                        alternativaId = alternativaId
                                    });
                                }
                            }
                        }
                    }

                    // ===== AJUSTE DE IDs =====
                    alteracao.loteFormFaseId = alteracao.loteFormFase;
                    alteracao.loteVisita = 50000;

                    if ((alteracao.idApp == 0 || alteracao.idApp == null) && alteracao.id >= 5000 || alteracao.idApp == alteracao.id)
                    {
                        alteracao.idApp = alteracao.id;
                        alteracao.id = -1;
                    }

                    alteracao.idApp ??= alteracao.id;

                    // ===== PREPARAR JSON =====
                    var updateDataParametros = new UpdateDataParametrosLoteForm
                    {
                        array = new List<LoteFormFromWebService> { alteracao }
                    };

                    var updateJson = JsonConvert.SerializeObject(updateDataParametros);
                    updateJson = Alteracao.AjustaArray(updateJson);
                    updateJson = updateJson.Replace("loteId", "lote")
                        .Replace("longitude", "longitude") // ✅ MUDANÇA: já está correto
                        .Replace("parametroId", "id")
                        .Replace("parametroTipoId", "parametroTipo")
                        .Replace(@"""loteFormVinculado"":-1,", @"""loteFormVinculado"":"""",");

                    // ===== ENVIAR =====
                    var result = await ISIWebService.Instance.SendData(updateJson,
                        ehAvaliacaoDeGalpao ? "postLoteFormGalpao" : "postLoteForms");

                    if (result.sucesso)
                    {
                        result.data = result.data.Replace("loteForms", "dados");
                        result.data = Alteracao.AjustaResultData(result.data);
                        var resultIds = JsonConvert.DeserializeObject<UpdateResults>(result.data);

                        if (resultIds?.dados != null)
                        {
                            foreach (var resultinfo in resultIds.dados)
                            {
                                await Db.ExecuteAsync($"update LoteForm set idApp={resultinfo.idApp} where id={resultinfo.idApp}");

                                if (resultinfo.id != resultinfo.idApp)
                                {
                                    await Db.ExecuteAsync($"update LoteForm set id={resultinfo.id} where id={resultinfo.idApp}");
                                    await Db.ExecuteAsync($"update LoteForm set loteFormVinculado={resultinfo.id} where loteFormVinculado={resultinfo.idApp}");
                                    await Db.ExecuteAsync($"update LoteFormParametro set LoteFormId={resultinfo.id} where LoteFormId={resultinfo.idApp}");
                                    await Db.ExecuteAsync($"update LoteFormAvaliacaoGalpao set LoteFormId={resultinfo.id} where LoteFormId={resultinfo.idApp}");
                                    await Db.ExecuteAsync($"update LoteFormImagem set LoteFormId={resultinfo.id} where LoteFormId={resultinfo.idApp}");
                                }

                                await Db.ExecuteAsync($"update LoteForm set temmudanca=0 where id={resultinfo.id}");
                            }
                        }
                    }
                    else
                    {
                        if (result.mensagem == "Erro ao salvar lote forms sincronizado")
                            await Db.ExecuteAsync($"update LoteForm set temmudanca=0 where id={alteracao.idApp}");
                        else
                        {
                            await SentryHelper.LogErrorAsync(updateJson, "Formulários do Lote", $"Mensagem: {result.mensagem} | Data: {result.data}");
                            throw new Exception(!string.IsNullOrEmpty(result.mensagem) ? result.mensagem : (!string.IsNullOrEmpty(result.data) ? result.data : "Erro desconhecido ao enviar formulários"));
                        }
                    }
                }
                catch (Exception ex)
                {
                    await SentryHelper.LogErrorAsync(ex.Message, "FazUploadLoteFormsAtualizados", ex.StackTrace);
                    throw;
                }
            }
        }
    }

    /// <summary>
    /// Imagem do formulário do lote para web service.
    /// </summary>
    public class LoteFormImageWebService
    {
        public string? url { get; set; } // ✅ MUDANÇA: nullable
    }

    /// <summary>
    /// Dados do formulário do lote para web service.
    /// </summary>
    public class LoteFormFromWebService : LoteForm
    {
        public List<ParametrosValores>? parametros { get; set; } // ✅ MUDANÇA: nullable
        public List<LoteFormImageWebService>? imagens { get; set; } // ✅ MUDANÇA: nullable
        public List<LoteFormParametroGalpaoDoWebService>? loteFormParametroGalpao { get; set; } // ✅ MUDANÇA: nullable
        public int? loteFormFaseId { get; set; }
    }

    /// <summary>
    /// Avaliações do galpão do web service.
    /// </summary>
    public class AvalicoesGalpaoFromWebService : LoteForm
    {
        public List<ParametrosValores>? parametros { get; set; } // ✅ MUDANÇA: nullable
        public List<ParametrosGalpaoDoWebService>? parametrosGalpao { get; set; } // ✅ MUDANÇA: nullable
        public List<LoteFormImageWebService>? imagens { get; set; } // ✅ MUDANÇA: nullable
        public int? loteFormFaseId { get; set; }
    }

    /// <summary>
    /// Lista de formulários do web service.
    /// </summary>
    public class LotesFormFromWebService
    {
        public List<LoteFormFromWebService>? loteForms { get; set; } // ✅ MUDANÇA: nullable
    }

    /// <summary>
    /// Dados de atualização de imagens do formulário do lote.
    /// </summary>
    public class UpdateDataParametrosLoteFormImagem : UpdateDataParametros
    {
        public int id { get; set; }
        public int idApp { get; set; }
        public int qtdeImagens { get; set; }
    }

    /// <summary>
    /// Lista de IDs do formulário do lote.
    /// </summary>
    public class LoteFormIdList
    {
        public int id { get; set; }
        public int idApp { get; set; }
    }

    /// <summary>
    /// Imagem do formulário do lote.
    /// </summary>
    public class LoteFormImagem : ObservableObject
    {
        [Ignore]
        public string urlImagemLocal => $"{FileSystem.AppDataDirectory}/{url}";

        public int? LoteFormId { get; set; }
        public string? url { get; set; } // ✅ MUDANÇA: nullable
        public bool temmudanca { get; set; }

        /// <summary>
        /// Pega imagens do formulário.
        /// </summary>
        public static async Task<List<LoteFormImagem>> PegaImagens(int loteFormId)
        {
            var table = await Db.Table<LoteFormImagem>();
            return await table.Where(l => l.LoteFormId == loteFormId).ToListAsync();
        }

        /// <summary>
        /// Adiciona imagem.
        /// </summary>
        public static async Task AdicionaImagem(LoteFormImagem loteFormImagem)
        {
            loteFormImagem.temmudanca = true;
            await Db.InsertAsync(loteFormImagem);
        }

        /// <summary>
        /// Deleta imagens do formulário.
        /// </summary>
        public static async Task DeletaImagens(LoteForm loteForm)
        {
            if (loteForm == null || loteForm.id == null) return;
            await Db.ExecuteAsync($"delete from LoteFormImagem where LoteFormId={loteForm.id}");
        }

        /// <summary>
        /// Faz upload das imagens.
        /// </summary>
        internal static async Task UploadUpdates()
        {
            const string sql = @"
                select lf.id, lf.idApp, count(*) total
                from LoteFormImagem lfi
                inner join LoteForm lf on lf.id=lfi.LoteFormId
                where lfi.temmudanca=1
                and lf.idApp is not null and lf.idApp > 0
                and lf.id != lf.idApp
                and not exists (select * from loteVisita lv where lv.lote=lf.loteId and lv.loteVisitaStatus=1)
                group by lf.id, lf.idApp";

            var alteracoes = await Db.QueryAsync<LoteFormIdList>(sql);
            foreach (var alteracao in alteracoes)
                await UploadImages(alteracao.id, alteracao.idApp);
        }

        /// <summary>
        /// Lista imagens para backup.
        /// </summary>
        internal static async Task<List<string>> ListaImagensParaBackup()
        {
            var imagens = new List<string>();
            const string sql = "select * from LoteFormImagem WHERE temmudanca=1";
            var alteracoes = await Db.QueryAsync<LoteFormImagem>(sql);

            foreach (var alteracao in alteracoes)
                imagens.Add(alteracao.urlImagemLocal);

            return imagens;
        }

        /// <summary>
        /// Faz upload de imagens específicas.
        /// </summary>
        internal static async Task UploadImages(int loteFormId, int loteFormIdApp)
        {
            var sql = $"select * from LoteFormImagem WHERE LoteFormId={loteFormId} and temmudanca=1";
            var alteracoes = await Db.QueryAsync<LoteFormImagem>(sql);

            if (alteracoes.Count <= 0) return;

            var settings = new JsonSerializerSettings { DateFormatString = "yyyy-MM-dd HH:mm:ss" };

            var updateDataParametros = new UpdateDataParametrosLoteFormImagem
            {
                id = loteFormId,
                idApp = loteFormIdApp,
                qtdeImagens = alteracoes.Count
            };

            var updateJson = JsonConvert.SerializeObject(updateDataParametros, settings);
            updateJson = Alteracao.AjustaArray(updateJson);

            var image1 = alteracoes.Count >= 1 ? alteracoes[0].urlImagemLocal : "";
            var image2 = alteracoes.Count >= 2 ? alteracoes[1].urlImagemLocal : "";
            var image3 = alteracoes.Count >= 3 ? alteracoes[2].urlImagemLocal : "";

            var result = await ISIWebService.Instance.SendDataWithImages(
                updateDataParametros, "postLoteFormImagens", image1, image2, image3);

            if (result.sucesso)
                await Db.ExecuteAsync($"update LoteFormImagem set temmudanca=0 where LoteFormId={loteFormId} and temmudanca=1");
            else
            {
                var img1Exists = !string.IsNullOrEmpty(image1) && File.Exists(image1);
                var img2Exists = !string.IsNullOrEmpty(image2) && File.Exists(image2);
                var img3Exists = !string.IsNullOrEmpty(image3) && File.Exists(image3);
                var errorDetail = $"Mensagem: {result.mensagem} | Data: {result.data} | " +
                                  $"Img1: {image1} (existe={img1Exists}) | " +
                                  $"Img2: {image2} (existe={img2Exists}) | " +
                                  $"Img3: {image3} (existe={img3Exists})";
                await SentryHelper.LogErrorAsync(updateJson, "Formulários do Lote - Imagens", errorDetail);
            }
        }
    }
}
