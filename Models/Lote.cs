using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

using SilvaData.Utilities;

using Newtonsoft.Json;

using SQLite;

using System.Collections.ObjectModel;

using static SilvaData.Models.Lote;

namespace SilvaData.Models
{
    public class LoteParametro
    {
        [Indexed] public int? parametroId { get; set; }
        [Indexed] public int? loteId { get; set; }
        public string valor { get; set; }

        public static async Task<List<LoteParametro>> PegaLoteParametroAsync(int id)
        {
            var table = await Db.Table<LoteParametro>();
            return await table.Where(p => p.loteId == id).ToListAsync().ConfigureAwait(false);
        }

        public static async Task<int> SaveItemAsync(LoteParametro lp)
        {
            return await Db.InsertOrReplaceAsync(lp);
        }

        internal static async Task<List<ParametrosValores>> GetItemsForUploadAsync(int? id)
        {
            var table = await Db.Table<LoteParametro>();

            var parlist = await table
                .Where(p => p.loteId == id)
                .ToListAsync();

            return parlist.Select(par => new ParametrosValores
            {
                parametroId = par.parametroId,
                valor = par.valor
            }).ToList();
        }
    }

    public class UpdateDataParametrosLote : UpdateDataParametros
    {
        public new List<LoteFromWebService> array;
    }

    public partial class Lote : ObservableObject
    {
        public static bool NeedRefresh;
        public static DateTime LastCheckPrecisaSincronizar;

        public DateTime dataUltimaAtualizacao;

        [PrimaryKey] public int? id { get; set; }
        public int? idApp { get; set; }
        public int? unidadeEpidemiologicaId { get; set; }
        public int? loteStatus { get; set; }
        public int? pesoInicial { get; set; }
        public DateTime? dataInicio { get; set; }

        [Ignore]
        [JsonIgnore]
        public string dataInicioText => dataInicio != null
            ? ((DateTime)dataInicio).ToShortDateString()
            : "";

        public string numero { get; set; }

        [Ignore]
        [JsonIgnore]
        public string numeroAsInt => new string(numero.Where(char.IsDigit).ToArray());

        public DateTime? dataAbate { get; set; }

        [Ignore]
        [JsonIgnore]
        public string dataAbateText => dataAbate != null
            ? ((DateTime)dataAbate).ToString("dd/MM/yyyy")
            : "";

        public float? pesoFinal { get; set; }
        public float? conversaoAlimentarReal { get; set; }
        public float? mortalidade { get; set; }
        public bool temmudanca { get; set; }
        public int? excluido { get; set; }

        [JsonIgnore][Ignore] public string UnidadeEpidemiologicaNome { get; set; }
        [JsonIgnore][Ignore] public string RegionalNome { get; set; }
        [JsonIgnore][Ignore] public string PropriedadeNome { get; set; }

        [JsonIgnore][Ignore] public bool EstaFechado => loteStatus == 2;

        [JsonIgnore][Ignore] public Color StatusColor => ISIMacro.StatusColor(ISIMacroScoreMedio);

        /// <summary>
        /// Copia propriedades que no vm do banco de dados (Ignore) de outro objeto.
        /// Evita que nomes de Propriedade/Regional desapaream em updates parciais.
        /// </summary>
        public void TransferMetadataFrom(Lote source)
        {
            if (source == null) return;
            UnidadeEpidemiologicaNome = source.UnidadeEpidemiologicaNome;
            RegionalNome = source.RegionalNome;
            PropriedadeNome = source.PropriedadeNome;
            // Preserva tambm o score mdio se o novo objeto no tiver (calculado localmente)
            if (ISIMacroScoreMedio == 0 && source.ISIMacroScoreMedio != 0)
                ISIMacroScoreMedio = source.ISIMacroScoreMedio;
        }

        [JsonIgnore]
        [Ignore]
        public string Status => loteStatus is null or 1 ? Traducao.Aberto : Traducao.Fechado;

        [JsonIgnore]
        [Ignore]
        public Color BackgroundColor => EstaFechado ? Color.FromArgb("#F5F5F5") : Colors.White;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StatusColor))]
        [NotifyPropertyChangedFor(nameof(ISIMacroScoreMedioStatusText))]
        [NotifyPropertyChangedFor(nameof(ISIMacroScoreMedioFormatted))]
        private double iSIMacroScoreMedio = 0;

        [JsonIgnore]
        [Ignore]
        public string ISIMacroScoreMedioStatusText => ISIMacro.StatusText(ISIMacroScoreMedio);

        [JsonIgnore]
        [Ignore]
        public string ISIMacroScoreMedioFormatted => ISIMacroScoreMedio.ToString("N1");

        [JsonIgnore] public DateTime? DataInicioVisita { get; set; }
        [JsonIgnore] public int? VisitaAbertaId { get; set; }
        [JsonIgnore] public int? TipoVisita { get; set; }

        [JsonIgnore]
        [Ignore]
        public string DescricaoCompleta
        {
            get
            {
                // Fallback: garante nomes preenchidos caso não tenham sido via JOIN ou ViewModel
                if (string.IsNullOrWhiteSpace(UnidadeEpidemiologicaNome) && unidadeEpidemiologicaId.HasValue)
                {
                    this.EnsureNames();
                }

                return $"{(Permissoes.TratamentoEmVezDeLote ? Traducao.Tratamento : Traducao.Lote)} {numero} / {RegionalNome} / {PropriedadeNome} / {UnidadeEpidemiologicaNome}";
            }
        }

        /// <summary>
        /// MIGRADO: Retorna a lista ao invés de armazenar em estático
        /// </summary>
        public static async Task<List<Lote>> PegaListaLotesAsync(bool maisNovoPrimeiro = true)
        {
            var orderClause = maisNovoPrimeiro ? "order by lote.id" : "order by lote.id DESC";

            var sql = $@"Select lote.*, lv.dataInicio DataInicioVisita, lv.loteVisitaTipo TipoVisita, lv.id VisitaAbertaId,
                         ue.nome as UnidadeEpidemiologicaNome,
                         r.nome as RegionalNome,
                         p.nome as PropriedadeNome
                         from lote 
                         left outer join loteVisita lv on lv.lote=lote.id and lv.loteVisitaStatus=1 
                         inner join UnidadeEpidemiologica ue on lote.unidadeEpidemiologicaId = ue.id
                         left outer join Propriedade p on ue.propriedadeId = p.id
                         left outer join Regional r on p.regionalId = r.id
                         where coalesce(lote.excluido,0)!=1 and ue.nome is not null {orderClause}";

            return await Db.QueryAsync<Lote>(sql).ConfigureAwait(false);
        }

        /// <summary>
        /// Retorna lotes em alerta COM nomes populados via CacheService
        /// </summary>
        public static async Task<IEnumerable<Lote>> PegaListaLotesEmAlertaAsync()
        {
            // ★★★ Query SIMPLES (sem JOIN) ★★★
            var sql = $@"
                SELECT lote.*
                FROM lote 
                WHERE 
                    COALESCE(lote.excluido, 0) != 1 
                    AND lote.ISIMacroScoreMedio > {ISIMacro.AnimalSaudavel}
                    AND lote.unidadeEpidemiologicaId IS NOT NULL
                ORDER BY CAST(lote.numero AS INTEGER)";

            var lotes = await Db.QueryAsync<Lote>(sql).ConfigureAwait(false);

            // ★★★ Preenche nomes usando CacheService ★★★
            lotes.EnsureNames();

            // ★ Debug
            System.Diagnostics.Debug.WriteLine($"[Lote] Lotes em alerta: {lotes.Count()}");
            foreach (var lote in lotes.Take(3))
            {
                System.Diagnostics.Debug.WriteLine($"  {lote.numero}: UE={lote.UnidadeEpidemiologicaNome}, Prop={lote.PropriedadeNome}, Reg={lote.RegionalNome}");
            }

            return lotes;
        }

        private static readonly Dictionary<int, Lote> _loteCache = new();
        private static readonly SemaphoreSlim _cacheLock = new(1, 1);

        /// <summary>
        /// Busca um lote do cache local (síncrono). Retorna null se não estiver em cache.
        /// </summary>
        public static Lote? GetCachedLote(int loteId)
        {
            return _loteCache.TryGetValue(loteId, out var lote) ? lote : null;
        }

        public static async Task<Lote> PegaLoteAsync(int loteId, bool forceRefresh = false)
        {
            // Verifica cache primeiro
            if (!forceRefresh && _loteCache.TryGetValue(loteId, out var cachedLote))
                return cachedLote;

            var sql = $@"Select lote.*, lv.dataInicio DataInicioVisita, lv.loteVisitaTipo TipoVisita, lv.id VisitaAbertaId,
                         ue.nome as UnidadeEpidemiologicaNome,
                         r.nome as RegionalNome,
                         p.nome as PropriedadeNome
                         from lote 
                         left outer join loteVisita lv on lv.lote=lote.id and lv.loteVisitaStatus=1 
                         inner join UnidadeEpidemiologica ue on lote.unidadeEpidemiologicaId = ue.id
                         left outer join Propriedade p on ue.propriedadeId = p.id
                         left outer join Regional r on p.regionalId = r.id
                         where coalesce(lote.excluido,0)!=1 and lote.id={loteId} 
                         order by Cast(numero as integer)";

            var lote = await Db.FindWithQueryAsync<Lote>(sql);

            // Adiciona ao cache
            if (lote?.id != null)
            {
                await _cacheLock.WaitAsync();
                try
                {
                    _loteCache[(int)lote.id] = lote;
                    
                    // Limita cache a 100 itens
                    if (_loteCache.Count > 100)
                    {
                        var oldestKey = _loteCache.Keys.First();
                        _loteCache.Remove(oldestKey);
                    }
                }
                finally
                {
                    _cacheLock.Release();
                }
            }

            return lote;
        }

        /// <summary>
        /// Atualiza todas as médias do ISI Macro Score para todos os lotes.
        /// </summary>
        public static async Task AtualizaTodasMediasISIMacro()
        {
            var lotesaatualizar = await Lote.PegaListaLotesAsync();

            foreach (var lote in lotesaatualizar.Where(lote => lote.id != null))
                await Lote.AtualizaISIMacroScoreMedio((int)lote.id, true);

            Preferences.Set("UltimaAtualizacaoTodasMediasISIMacro", DateTime.Now);
        }

        /// <summary>
        /// Atualiza a média do ISI Macro Score para o lote especificado.
        /// </summary>
        public static async Task AtualizaISIMacroScoreMedio(int loteId, bool refresh = false)
        {
            var sqlFinal = @$"
                update lote set ISIMacroScoreMedio = (
                    select avg(ISIMacroScore) from (
                        select LoteFormid, 
                            sum(pa.score * p.peso) as ISIMacroScore 
                        from LoteFormParametro lfp 
                        inner join LoteForm lf on lf.id = lfp.LoteFormId 
                        inner join Parametro p on p.id = lfp.parametroId and p.Tipo = 0  -- Tipo = 0: exclui parâmetros Isolados do cálculo de score médio
                        inner join ParametroCategoria pc on pc.id = p.parametroCategoriaId
                        inner join ParametroAlternativas pa on pa.idParametro = p.id and pa.id = lfp.valor
                        where lf.parametroTipoId = 15 and lf.loteId = {loteId}
                        group by LoteFormId
                    ) as ISIMacroScores
                ) where lote.Id = {loteId}";

            await Db.ExecuteAsync(sqlFinal);

            if (refresh)
            {
                var lote = await Lote.PegaLoteAsync(loteId, forceRefresh: true).ConfigureAwait(false);
                var novoIsiMacroScore = lote?.ISIMacroScoreMedio ?? 0;

                WeakReferenceMessenger.Default.Send(new ISIMacroScoreMedioAtualizadoMessage(loteId, novoIsiMacroScore));
            }
        }

        /// <summary>
        /// MIGRADO: Notifica o sistema após salvar com transação robusta
        /// </summary>
        public static async Task<int> SaveLote(Lote lote)
        {
            NeedRefresh = true;

            lote.dataUltimaAtualizacao = DateTime.Now;
            lote.temmudanca = true;
            lote.loteStatus ??= 1;

            int result = 0;

            // Para novo lote: obtém o ID antes de entrar na transação,
            // evitando deadlock pois GetNextId usa o mesmo Db
            if (lote.id == 0)
            {
                lote.id = await Alteracao.GetNextId(lote).ConfigureAwait(false);
            }

            await Db.RunInTransactionAsync(connection =>
            {
                if (lote.id != 0 && connection.Find<Lote>(lote.id) != null)
                {
                    result = connection.Update(lote);
                }
                else
                {
                    result = connection.Insert(lote);
                }
            }).ConfigureAwait(false);

            WeakReferenceMessenger.Default.Send(new LoteAlteradoMessage(lote));
            return result;
        }

        public static async Task ApagaLotesFechadosQueJaFizeramUploadEEstaoFechados()
        {
            await Db.ExecuteAsync("delete from Lote where loteStatus=2");
        }

        internal static async Task UploadUpdates(bool todosDeUmaVez = true)
        {
            var sql = "select * from Lote l WHERE l.temmudanca=1 and not exists (select * from loteVisita lv where lv.lote=l.id and lv.loteVisitaStatus=1)";
            var alteracoes = await Db.QueryAsync<LoteFromWebService>(sql);

            if (alteracoes.Count <= 0)
                return;

            foreach (var alteracao in alteracoes)
            {
                var parametros = await LoteParametro.GetItemsForUploadAsync(alteracao.id);
                alteracao.parametros = parametros;
                if (alteracao.idApp == 0 || alteracao.idApp == null)
                {
                    alteracao.idApp = alteracao.id;
                    alteracao.id = -1;
                }
                if (alteracao.id >= 5000) alteracao.id = null;
                if (alteracao.id > 0) alteracao.idApp = alteracao.id;
                if (alteracao.dataUltimaAtualizacao == DateTime.MinValue)
                    alteracao.dataUltimaAtualizacao = DateTime.Now;
            }

            if (todosDeUmaVez)
            {
                await EnviarLotes(alteracoes);
            }
            else
            {
                foreach (var lote in alteracoes)
                {
                    await EnviarLotes(new List<LoteFromWebService> { lote });
                }
            }
        }

        private static async Task EnviarLotes(List<LoteFromWebService> lotes)
        {
            var updateDataParametros = new UpdateDataParametrosLote
            {
                array = lotes
            };

            var updateJson = JsonConvert.SerializeObject(updateDataParametros);

            updateJson = Alteracao.AjustaArray(updateJson);
            updateJson = updateJson.Replace("unidadeEpidemiologicaId", "unidadeEpidemiologica");
            updateJson = updateJson.Replace("parametroId", "id");

            var result = await ISIWebService.Instance.SendData(updateJson, "postLotes");

            if (result.sucesso)
            {
                result.data = result.data.Replace("lotes", "dados");
                result.data = Alteracao.AjustaResultData(result.data);
                var resultIds = JsonConvert.DeserializeObject<UpdateResults>(result.data);

                foreach (var resultinfo in resultIds.dados)
                {
                    await Db.ExecuteAsync($"update Lote set idApp={resultinfo.idApp} where id={resultinfo.idApp}");
                    if (resultinfo.id == resultinfo.idApp) continue;
                    await Db.ExecuteAsync($"update Lote set id={resultinfo.id} where id={resultinfo.idApp}");
                    await Db.ExecuteAsync($"update LoteParametro set loteId={resultinfo.id} where loteId={resultinfo.idApp}");
                    await Db.ExecuteAsync($"update LoteForm set loteId={resultinfo.id} where loteId={resultinfo.idApp}");
                    await Db.ExecuteAsync($"update LoteVisita set lote={resultinfo.id} where lote={resultinfo.idApp}");
                }

                var loteIds = lotes.Select(l => l.idApp).ToList();
                string idList = string.Join(",", loteIds);
                await Db.ExecuteAsync($"update Lote set temmudanca=0 where idApp IN ({idList})");
            }
            else
            {
                await SentryHelper.LogErrorAsync(updateJson, "Lote", result.mensagem);
                throw new Exception(!string.IsNullOrEmpty(result.mensagem) ? result.mensagem : "Erro desconhecido ao enviar lotes");
            }
        }

        public static async Task ChecaPrecisaSincronizar()
        {
            if (LastCheckPrecisaSincronizar.AddMinutes(5) > DateTime.Now) return;
            LastCheckPrecisaSincronizar = DateTime.Now;

            // Implementação futura de verificação de sincronização
        }

        public class LoteFromWebService : Lote
        {
            public List<ParametrosValores> parametros;
        }

        public class LotesFromWebService
        {
            public List<LoteFromWebService> lotes;
        }

        public class LoteRepository
        {
            public LoteRepository()
            {
                loteCollection = new ObservableCollection<Lote>();
                GenerateOrders();
            }

            public ObservableCollection<Lote> loteCollection { get; set; }

            private void GenerateOrders()
            {
                loteCollection.Add(new Lote
                {
                    numero = "1",
                    dataInicio = DateTime.Now,
                    loteStatus = 1
                });

                loteCollection.Add(new Lote
                {
                    numero = "2",
                    dataInicio = DateTime.Now,
                    loteStatus = 2
                });

                loteCollection.Add(new Lote
                {
                    numero = "3",
                    dataInicio = DateTime.Now,
                    loteStatus = 1
                });

                loteCollection.Add(new Lote
                {
                    numero = "4",
                    dataInicio = DateTime.Now,
                    loteStatus = 2
                });
            }
        }

        public class TotalResult
        {
            public int Total { get; set; }
        }
    }

    /// <summary>
    /// Classe que contém as informações do fechamento de lote.
    /// </summary>
    public class LoteFechamentoInfo
    {
        public DateTime DataFechamento { get; set; }
        public string Observacoes { get; set; } = string.Empty;
        public bool Confirmado { get; set; }

        public static LoteFechamentoInfo Default() => new()
        {
            DataFechamento = DateTime.Now,
            Observacoes = string.Empty,
            Confirmado = false
        };
    }

}
