using CommunityToolkit.Mvvm.ComponentModel;

using SilvaData.Infrastructure;

using Newtonsoft.Json;

using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.Maui.Controls; // for Brush, SolidColorBrush
using Microsoft.Maui.Graphics;

namespace SilvaData.Models
{
    /// <summary>
    /// Classe de L�gica de Neg�cios para buscar e processar dados de gr�ficos.
    /// MIGRADO: Injecta CacheService e ISIWebService.
    /// </summary>
    public partial class Graficos : ObservableObject
    {
        private readonly ISIWebService _webService;
        private readonly CacheService _cacheService; // ADICIONADO

        // Propriedades de dados brutos
        public List<GraficoSuperCategoria> DadosSuperCategoriaSemFiltros { get; set; } = new();
        public List<GraficoDispersao> DadosDispersaoSemFiltros { get; set; } = new();

        // Propriedades de dados filtrados/processados
        public ObservableCollection<GraficoSuperCategoria> DadosGraficosSuperCategoria { get; set; } = new();
        public ObservableCollection<string> ListaRegionais { get; set; } = new();
        public ObservableCollection<string> ListaUE { get; set; } = new();
        public List<ObservableCollection<GraficoSuperCategoriaAgrupado>> ListaAcometimentos { get; set; } = new();
        public List<ObservableCollection<GraficoSuperCategoriaAgrupado>> ListaSuperCategorias { get; set; } = new();
        public List<ObservableCollection<GraficoCategoriaAgrupado>> ListaCategorias { get; set; } = new();
        public List<ObservableCollection<GraficoParametroAgrupado>> ListaParametros { get; set; } = new();
        public ObservableCollection<GraficoDispersao> DadosGraficosDispersao { get; set; } = new();

        [ObservableProperty]
        private ObservableCollection<Brush> dadosGraficosDispersaoCores = new();

        /// <summary>
        /// MIGRADO: Construtor agora injeta CacheService
        /// </summary>
        public Graficos(ISIWebService webService, CacheService cacheService)
        {
            _webService = webService;
            _cacheService = cacheService; // ADICIONADO
        }

        /// <summary>
        /// Construtor padr�o com fallback (para desserializa��o, se necess�rio).
        /// </summary>
        public Graficos()
        {
            _webService = ISIWebService.Instance;
            _cacheService = ServiceHelper.GetRequiredService<CacheService>(); // ADICIONADO
        }

        /// <summary>
        /// Download de dados dos gr�ficos da API.
        /// </summary>
        public async Task<string> DownloadDadosGraficos()
        {
            try
            {
                if (_webService.LoggedUser == null)
                    return "Usu�rio n�o logado.";

                var pegaGraficosParametros = new pegaGraficosParametros
                {
                    dispositivoId = Preferences.Get("my_id", string.Empty),
                    session = _webService.LoggedUser.session,
                    usuario = _webService.LoggedUser.id
                };

                var jsonParametros = JsonConvert.SerializeObject(pegaGraficosParametros);
                using var requestBody = new StringContent(EncryptDecrypt.Encrypt(jsonParametros));

                ISIWebServiceResult result = await _webService.ExecutePostAndWaitResult(requestBody, "getGraficoSuperCategoriateste", "parametros")
                    .ConfigureAwait(false);

                if (!result.sucesso) return result.mensagem ?? "Erro desconhecido";

                DadosSuperCategoriaSemFiltros = JsonConvert.DeserializeObject<List<GraficoSuperCategoria>>(result.data) ?? new();

                var listaParametros = await Parametro.PegaParametroAsync(15).ConfigureAwait(false);
                foreach (var item in DadosSuperCategoriaSemFiltros)
                {
                    var parametro = listaParametros.FirstOrDefault(p => p.id == item.ParametroId);
                    if (parametro != null)
                        item.Peso = (float)parametro.peso;
                }

                #region ListaRegionais
                ListaRegionais.Clear();
                ListaRegionais.Add("Todas");
                var regionais = DadosSuperCategoriaSemFiltros
                    .Select(regional => regional.RegionalNome)
                    .Where(nome => !string.IsNullOrEmpty(nome))
                    .Distinct();
                foreach (var regional in regionais)
                    ListaRegionais.Add(regional!);
                #endregion

                #region ListaUE
                ListaUE.Clear();
                ListaUE.Add("Todas");
                var uelist = DadosSuperCategoriaSemFiltros
                    .Select(ue => ue.UnidadeEpidemiologicaNome)
                    .Where(nome => !string.IsNullOrEmpty(nome))
                    .Distinct();
                foreach (var ue in uelist)
                    ListaUE.Add(ue!);
                #endregion

                using var requestBody2 = new StringContent(EncryptDecrypt.Encrypt(jsonParametros));
                result = await _webService.ExecutePostAndWaitResult(requestBody2, "getGraficoDispersao", "parametros")
                    .ConfigureAwait(false);

                if (!result.sucesso) return result.mensagem ?? "Erro ao buscar dispers�o";

                DadosDispersaoSemFiltros = JsonConvert.DeserializeObject<List<GraficoDispersao>>(result.data) ?? new();

                GeraGraficoDispersao("", "");
                GeraGraficoSuperCategoria("", "");
                GeraGraficoAcometimento("", "");

                // Salva Dados Baixados no Cache
                var DadosGraficosDispersaoJSON = JsonConvert.SerializeObject(DadosGraficosDispersao);
                var ListaSuperCategoriasJSON = JsonConvert.SerializeObject(ListaSuperCategorias);

                Preferences.Set("ListaSuperCategoriasJSON", ListaSuperCategoriasJSON);
                Preferences.Set("DadosGraficosDispersaoJSON", DadosGraficosDispersaoJSON);

                return "ok";
            }
            catch (Exception e)
            {
                Debug.WriteLine($"[Graficos] Erro ao download: {e.Message}");
                return e.Message;
            }
        }

        /// <summary>
        /// Gera gr�fico de dispers�o filtrado.
        /// </summary>
        public void GeraGraficoDispersao(string RegionalSelecionada, string UESelecionada)
        {
            if (DadosDispersaoSemFiltros == null)
            {
                DadosGraficosDispersao = new ObservableCollection<GraficoDispersao>();
                return;
            }

            DadosGraficosDispersao = new ObservableCollection<GraficoDispersao>(DadosDispersaoSemFiltros
                .Where(d => (d.RegionalNome == RegionalSelecionada || string.IsNullOrEmpty(RegionalSelecionada)) &&
                            (d.UnidadeEpidemiologicaNome == UESelecionada || string.IsNullOrEmpty(UESelecionada))).ToList());

            var dadosAgrupados = (from dado in DadosGraficosDispersao
                                  group dado by new { dado.dataLote, dado.LoteNumero, dado.PropriedadeNome, dado.RegionalNome, dado.UnidadeEpidemiologicaNome }
                                  into dadoAgrupado
                                  select new GraficoDispersao()
                                  {
                                      dataLote = dadoAgrupado.Key.dataLote,
                                      LoteNumero = dadoAgrupado.Key.LoteNumero,
                                      Score = dadoAgrupado.Average(x => x.Score),
                                      PropriedadeNome = dadoAgrupado.Key.PropriedadeNome,
                                      RegionalNome = dadoAgrupado.Key.RegionalNome,
                                      UnidadeEpidemiologicaNome = dadoAgrupado.Key.UnidadeEpidemiologicaNome,
                                      QtdeAgrupado = dadoAgrupado.Count()
                                  }).ToList();

            DadosGraficosDispersao = new ObservableCollection<GraficoDispersao>(dadosAgrupados);
        }

        /// <summary>
        /// Calcula a porcentagem de acometimento.
        /// </summary>
        public double CalculaAcometimento(double media, double scoremaximo)
        {
            return (scoremaximo > 0) ? media / scoremaximo : 0;
        }

        /// <summary>
        /// Gera gr�fico de acometimento filtrado.
        /// </summary>
        public void GeraGraficoAcometimento(string RegionalSelecionada, string UESelecionada)
        {
            try
            {
                if (DadosSuperCategoriaSemFiltros == null)
                {
                    ListaAcometimentos = new List<ObservableCollection<GraficoSuperCategoriaAgrupado>>();
                    return;
                }

                DadosGraficosSuperCategoria = new ObservableCollection<GraficoSuperCategoria>(DadosSuperCategoriaSemFiltros
                    .Where(d => (d.RegionalNome == RegionalSelecionada || string.IsNullOrEmpty(RegionalSelecionada)) &&
                                (d.UnidadeEpidemiologicaNome == UESelecionada || string.IsNullOrEmpty(UESelecionada))).ToList());

                var categoriaAtual = new ObservableCollection<GraficoSuperCategoriaAgrupado>();
                ListaAcometimentos = new List<ObservableCollection<GraficoSuperCategoriaAgrupado>>();

                var ListaGraficoSuperCategoriaAgrupadaPorUE = DadosGraficosSuperCategoria
                .GroupBy(d => new { d.dataLote, d.SuperCategoriaNome, d.SuperCategoriaId, d.UnidadeEpidemiologicaId })
                .Select(dadoAgrupado => new GraficoSuperCategoriaAgrupadoPorUE()
                {
                    dataLote = dadoAgrupado.Key.dataLote,
                    SuperCategoria = dadoAgrupado.Key.SuperCategoriaNome,
                    SuperCategoriaId = dadoAgrupado.Key.SuperCategoriaId,
                    MediaScore = 0,
                    ScoreTotal = dadoAgrupado.Sum(x => x.ScoreTotal),
                    Quantidade = dadoAgrupado.Min(x => x.Quantidade),
                    PorcentagemAcometimento = 0,
                    UnidadeEpidemiologicaId = dadoAgrupado.Key.UnidadeEpidemiologicaId
                })
                .ToList();

                foreach (var item in ListaGraficoSuperCategoriaAgrupadaPorUE)
                {
                    item.Quantidade = CalcularQuantidadeAvesPorDiaUE(item.dataLote, item.UnidadeEpidemiologicaId);
                    item.MediaScore = (item.Quantidade > 0) ? Math.Round(item.ScoreTotal / item.Quantidade, 2) : 0;
                    item.PorcentagemAcometimento = Math.Round(item.MediaScore * 100f / PegaScoreMaximoSuperCategoria(item.SuperCategoriaId, item.SuperCategoria), 2);
                }

                var ListaAcometimentosAgrupado = (from item in ListaGraficoSuperCategoriaAgrupadaPorUE
                                                  group item by new { item.dataLote, item.SuperCategoria, item.SuperCategoriaId }
                into itemSuperCategoria
                                                  select new GraficoSuperCategoriaAgrupado()
                                                  {
                                                      dataLote = itemSuperCategoria.Key.dataLote,
                                                      SuperCategoria = itemSuperCategoria.Key.SuperCategoria,
                                                      SuperCategoriaId = itemSuperCategoria.Key.SuperCategoriaId,
                                                      ScoreTotal = Math.Round(itemSuperCategoria.Sum(x => x.MediaScore), 2),
                                                      Quantidade = itemSuperCategoria.Count(),
                                                      MediaScore = Math.Round(itemSuperCategoria.Average(x => x.MediaScore), 2),
                                                      PorcentagemAcometimento = Math.Round(itemSuperCategoria.Average(x => x.PorcentagemAcometimento), 2)
                                                  }).ToList();

                var SuperCategoria = "";
                foreach (var item in ListaAcometimentosAgrupado.Where(i => i.PorcentagemAcometimento > 0).OrderBy(p => p.SuperCategoria).ThenBy(p => p.dataLote))
                {
                    if (!string.IsNullOrEmpty(SuperCategoria) && SuperCategoria != item.SuperCategoria)
                    {
                        ListaAcometimentos.Add(categoriaAtual);
                        categoriaAtual = new ObservableCollection<GraficoSuperCategoriaAgrupado>();
                    }
                    categoriaAtual.Add(item);
                    SuperCategoria = item.SuperCategoria;
                }
                if (categoriaAtual.Any()) ListaAcometimentos.Add(categoriaAtual);

                var listaDatas = ListaAcometimentosAgrupado.GroupBy(p => p.dataLote).Select(p => p.First().dataLote);
                for (int i = 0; i < ListaAcometimentos.Count; i++)
                {
                    var item = ListaAcometimentos[i];
                    ListaAcometimentos[i] = new ObservableCollection<GraficoSuperCategoriaAgrupado>(item.OrderBy(p => p.dataLote));
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"[Graficos] Erro em GeraGraficoAcometimento: {e.Message}");
            }
        }

        public class QuantidadeAgrupada
        {
            public float QtdeAgrupada;
        }

        /// <summary>
        /// Calcula a quantidade de aves por dia.
        /// </summary>
        public float CalcularQuantidadeAvesPorDia(DateTime date)
        {
            if (DadosGraficosSuperCategoria == null) return 0;

            var dadosGraficosSuperCategoriaPorRegional = from item in DadosGraficosSuperCategoria
                                                         where item.dataLote == date
                                                         group item by new { item.dataLote, item.RegionalId }
                                                         into qtdeRegional
                                                         select new QuantidadeAgrupada
                                                         {
                                                             QtdeAgrupada = qtdeRegional.First().Quantidade
                                                         };
            // Optimized: Sum works on IEnumerable, no need for .ToList()
            return dadosGraficosSuperCategoriaPorRegional.Sum(d => d.QtdeAgrupada);
        }

        /// <summary>
        /// Calcula a quantidade de aves por dia para uma UE espec�fica.
        /// </summary>
        public float CalcularQuantidadeAvesPorDiaUE(DateTime date, int unidadeEpidemiologicaId)
        {
            if (DadosGraficosSuperCategoria == null) return 0;

            var dadosGraficosSuperCategoriaPorRegional = DadosGraficosSuperCategoria
                .Where(item => item.dataLote == date && item.UnidadeEpidemiologicaId == unidadeEpidemiologicaId)
                .GroupBy(item => new { item.dataLote, item.RegionalId })
                .Select(qtdeRegional => new QuantidadeAgrupada
                {
                    QtdeAgrupada = qtdeRegional.First().Quantidade
                });
            // Optimized: Sum works on IEnumerable, no need for .ToList()
            return dadosGraficosSuperCategoriaPorRegional.Sum(d => d.QtdeAgrupada);
        }

        /// <summary>
        /// Gera gr�fico de SuperCategoria filtrado.
        /// </summary>
        public void GeraGraficoSuperCategoria(string RegionalSelecionada, string UESelecionada)
        {
            if (DadosSuperCategoriaSemFiltros == null) return;

            DadosGraficosSuperCategoria = new ObservableCollection<GraficoSuperCategoria>(
                DadosSuperCategoriaSemFiltros.Where(d =>
                    (d.RegionalNome == RegionalSelecionada || string.IsNullOrEmpty(RegionalSelecionada)) &&
                    (d.UnidadeEpidemiologicaNome == UESelecionada || string.IsNullOrEmpty(UESelecionada))
                ).ToList()
            );

            var categoriaAtual = new ObservableCollection<GraficoSuperCategoriaAgrupado>();
            ListaSuperCategorias = new List<ObservableCollection<GraficoSuperCategoriaAgrupado>>();

            var ListaGraficoSuperCategoriaAgrupadaPorUE = DadosGraficosSuperCategoria
            .GroupBy(d => new { d.dataLote, d.SuperCategoriaNome, d.SuperCategoriaId, d.UnidadeEpidemiologicaId })
            .Select(dadoAgrupado => new GraficoSuperCategoriaAgrupadoPorUE()
            {
                dataLote = dadoAgrupado.Key.dataLote,
                SuperCategoria = dadoAgrupado.Key.SuperCategoriaNome,
                SuperCategoriaId = dadoAgrupado.Key.SuperCategoriaId,
                MediaScore = 0,
                ScoreTotal = dadoAgrupado.Sum(x => x.ScoreTotal),
                Quantidade = dadoAgrupado.Min(x => x.Quantidade),
                PorcentagemAcometimento = 0,
                UnidadeEpidemiologicaId = dadoAgrupado.Key.UnidadeEpidemiologicaId
            })
            .ToList();

            foreach (var item in ListaGraficoSuperCategoriaAgrupadaPorUE)
            {
                item.Quantidade = CalcularQuantidadeAvesPorDiaUE(item.dataLote, item.UnidadeEpidemiologicaId);
                item.MediaScore = (item.Quantidade > 0) ? Math.Round(item.ScoreTotal / item.Quantidade, 2) : 0;
                item.PorcentagemAcometimento = Math.Round(item.MediaScore * 100f / PegaScoreMaximoSuperCategoria(item.SuperCategoriaId, item.SuperCategoria), 2);
            }

            var ListaGraficoSuperCategoriaAgrupada = (from item in ListaGraficoSuperCategoriaAgrupadaPorUE
                                                      group item by new { item.dataLote, item.SuperCategoria, item.SuperCategoriaId }
            into itemSuperCategoria
                                                      select new GraficoSuperCategoriaAgrupado()
                                                      {
                                                          dataLote = itemSuperCategoria.Key.dataLote,
                                                          SuperCategoria = itemSuperCategoria.Key.SuperCategoria,
                                                          SuperCategoriaId = itemSuperCategoria.Key.SuperCategoriaId,
                                                          ScoreTotal = Math.Round(itemSuperCategoria.Sum(x => x.MediaScore), 2),
                                                          Quantidade = itemSuperCategoria.Count(),
                                                          MediaScore = Math.Round(itemSuperCategoria.Average(x => x.MediaScore), 2),
                                                          PorcentagemAcometimento = Math.Round(itemSuperCategoria.Average(x => x.PorcentagemAcometimento), 2)
                                                      }).ToList();

            var SuperCategoria = "";
            foreach (var item in ListaGraficoSuperCategoriaAgrupada.OrderBy(p => p.SuperCategoria)
                                     .ThenBy(p => p.dataLote))
            {
                if (!string.IsNullOrEmpty(SuperCategoria) && SuperCategoria != item.SuperCategoria)
                {
                    ListaSuperCategorias.Add(categoriaAtual);
                    categoriaAtual = new ObservableCollection<GraficoSuperCategoriaAgrupado>();
                }
                categoriaAtual.Add(item);
                SuperCategoria = item.SuperCategoria;
            }
            if (categoriaAtual.Any()) ListaSuperCategorias.Add(categoriaAtual);

            #region SuperCategoriaDatas
            var listaDatas = ListaGraficoSuperCategoriaAgrupada.GroupBy(p => p.dataLote)
                .Select(p => p.First().dataLote);
            for (var i = 0; i < ListaSuperCategorias.Count; i++)
            {
                ObservableCollection<GraficoSuperCategoriaAgrupado> item = ListaSuperCategorias[i];
                if (!item.Any()) continue;
                foreach (var data in listaDatas)
                {
                    if (item.Count(p => p.dataLote == data) <= 0)
                    {
                        item.Add(new GraficoSuperCategoriaAgrupado
                        {
                            dataLote = data,
                            MediaScore = 0,
                            SuperCategoria = item.First().SuperCategoria,
                            SuperCategoriaId = item.First().SuperCategoriaId
                        });
                    }
                }
                ListaSuperCategorias[i] = new ObservableCollection<GraficoSuperCategoriaAgrupado>(item.OrderBy(p => p.dataLote));
            }
            #endregion
        }

        /// <summary>
        /// Gera gr�fico de Categoria com m�dias.
        /// </summary>
        public IEnumerable<GraficoCategoriaAgrupado> GeraGraficoCategoriaComMedias(string superCategoria)
        {
            if (DadosGraficosSuperCategoria == null) return new List<GraficoCategoriaAgrupado>();

            var ListaGraficoCategoriaAgrupadaPorUE = DadosGraficosSuperCategoria
                .Where(item => item.SuperCategoriaNome == superCategoria)
                .GroupBy(item => new { item.dataLote, item.ParametroCategoriaNome, item.ParametroCategoriaId, item.UnidadeEpidemiologicaId })
                .Select(itemCategoria => new GraficoCategoriaAgrupadoPorUE()
                {
                    dataLote = itemCategoria.Key.dataLote,
                    Categoria = itemCategoria.Key.ParametroCategoriaNome,
                    CategoriaId = itemCategoria.Key.ParametroCategoriaId,
                    ScoreTotal = Math.Round(itemCategoria.Sum(c => c.ScoreTotal), 2),
                    UnidadeEpidemiologicaId = itemCategoria.Key.UnidadeEpidemiologicaId
                }).ToList();

            foreach (var itemParametro in ListaGraficoCategoriaAgrupadaPorUE)
            {
                itemParametro.Quantidade = CalcularQuantidadeAvesPorDiaUE(itemParametro.dataLote, itemParametro.UnidadeEpidemiologicaId);
                itemParametro.MediaScore = (itemParametro.Quantidade > 0) ? Math.Round(itemParametro.ScoreTotal / itemParametro.Quantidade, 2) : 0;
            }

            var ListaGraficoCategoriaAgrupada = ListaGraficoCategoriaAgrupadaPorUE
              .GroupBy(item => new { item.dataLote, item.Categoria, item.CategoriaId })
              .Select(itemCategoria => new GraficoCategoriaAgrupado()
              {
                  dataLote = itemCategoria.Key.dataLote,
                  Categoria = itemCategoria.Key.Categoria,
                  CategoriaId = itemCategoria.Key.CategoriaId,
                  ScoreTotal = Math.Round(itemCategoria.Average(c => c.ScoreTotal), 2),
                  Quantidade = itemCategoria.Count(),
                  MediaScore = Math.Round(itemCategoria.Average(c => c.MediaScore), 2)
              }).ToList();

            return ListaGraficoCategoriaAgrupada;
        }

        /// <summary>
        /// Atualiza o gr�fico de Categoria.
        /// </summary>
        internal void AtualizaGraficoCategoria(string superCategoria)
        {
            var categoriaAtual = new ObservableCollection<GraficoCategoriaAgrupado>();
            ListaCategorias = new List<ObservableCollection<GraficoCategoriaAgrupado>>();

            var ListaGraficoCategoriaAgrupada = GeraGraficoCategoriaComMedias(superCategoria);

            var Categoria = "";
            foreach (var item in ListaGraficoCategoriaAgrupada.OrderBy(p => p.Categoria).ThenBy(p => p.dataLote))
            {
                if (!string.IsNullOrEmpty(Categoria) && Categoria != item.Categoria)
                {
                    ListaCategorias.Add(categoriaAtual);
                    categoriaAtual = new ObservableCollection<GraficoCategoriaAgrupado>();
                }
                categoriaAtual.Add(item);
                Categoria = item.Categoria;
            }
            if (categoriaAtual.Any()) ListaCategorias.Add(categoriaAtual);

            #region CategoriaDatas
            var listaDatas = ListaGraficoCategoriaAgrupada.GroupBy(p => p.dataLote).Select(p => p.First().dataLote);
            for (int i = 0; i < ListaCategorias.Count; i++)
            {
                ObservableCollection<GraficoCategoriaAgrupado> item = ListaCategorias[i];
                if (!item.Any()) continue;
                foreach (var data in listaDatas)
                {
                    if (item.Count(p => p.dataLote == data) <= 0)
                    {
                        item.Add(new GraficoCategoriaAgrupado
                        {
                            dataLote = data,
                            MediaScore = 0,
                            Categoria = item.First().Categoria
                        });
                    }
                }
                ListaCategorias[i] = new ObservableCollection<GraficoCategoriaAgrupado>(item.OrderBy(p => p.dataLote));
            }
            #endregion
        }

        /// <summary>
        /// Atualiza o gr�fico de Par�metro.
        /// </summary>
        internal void AtualizaGraficoParametro(string parametroCategoria)
        {
            if (DadosGraficosSuperCategoria == null) return;

            var parametroAtual = new ObservableCollection<GraficoParametroAgrupado>();
            ListaParametros = new List<ObservableCollection<GraficoParametroAgrupado>>();

            var ListaGraficoParametroAgrupadoPorUE = DadosGraficosSuperCategoria
                .Where(item => item.ParametroCategoriaNome == parametroCategoria)
                .GroupBy(item => new { item.dataLote, item.ParametroNome, item.ParametroId, item.UnidadeEpidemiologicaId })
                .Select(itemParametro => new GraficoParametroAgrupadoPorUE()
                {
                    dataLote = itemParametro.Key.dataLote,
                    Parametro = itemParametro.Key.ParametroNome,
                    ParametroId = itemParametro.Key.ParametroId.ToString(),
                    ScoreTotal = Math.Round(itemParametro.Sum(x => x.ScoreTotal), 2),
                    UnidadeEpidemiologicaId = itemParametro.Key.UnidadeEpidemiologicaId
                }).ToList();

            foreach (var item in ListaGraficoParametroAgrupadoPorUE)
            {
                item.Quantidade = CalcularQuantidadeAvesPorDiaUE(item.dataLote, item.UnidadeEpidemiologicaId);
                item.MediaScore = (item.Quantidade > 0) ? Math.Round(item.ScoreTotal / item.Quantidade, 2) : 0;
            }

            var ListaGraficoParametroAgrupado = ListaGraficoParametroAgrupadoPorUE
                .GroupBy(item => new { item.dataLote, item.Parametro, item.ParametroId })
                .Select(itemSuperCategoria => new GraficoParametroAgrupado()
                {
                    dataLote = itemSuperCategoria.Key.dataLote,
                    Parametro = itemSuperCategoria.Key.Parametro,
                    ParametroId = itemSuperCategoria.Key.ParametroId.ToString(),
                    ScoreTotal = Math.Round(itemSuperCategoria.Average(x => x.ScoreTotal), 2),
                    Quantidade = itemSuperCategoria.Count(),
                    MediaScore = Math.Round(itemSuperCategoria.Average(x => x.MediaScore), 2),
                }).ToList();

            var Parametro = "";
            foreach (var item in ListaGraficoParametroAgrupado.OrderBy(p => p.Parametro).ThenBy(p => p.dataLote))
            {
                if (!string.IsNullOrEmpty(Parametro) && Parametro != item.Parametro)
                {
                    ListaParametros.Add(parametroAtual);
                    parametroAtual = new ObservableCollection<GraficoParametroAgrupado>();
                }
                parametroAtual.Add(item);
                Parametro = item.Parametro;
            }
            if (parametroAtual.Any()) ListaParametros.Add(parametroAtual);

            #region Par�metro Datas
            var listaDatas = ListaGraficoParametroAgrupado.GroupBy(p => p.dataLote).Select(p => p.First().dataLote);
            for (int i = 0; i < ListaParametros.Count; i++)
            {
                ObservableCollection<GraficoParametroAgrupado> item = ListaParametros[i];
                if (!item.Any()) continue;
                foreach (var data in listaDatas)
                {
                    if (item.Count(p => p.dataLote == data) <= 0)
                    {
                        item.Add(new GraficoParametroAgrupado
                        {
                            dataLote = data,
                            MediaScore = 0,
                            Parametro = item.First().Parametro
                        });
                    }
                }
                ListaParametros[i] = new ObservableCollection<GraficoParametroAgrupado>(item.OrderBy(p => p.dataLote));
            }
            #endregion
        }

        /// <summary>
        /// Limpa todos os dados de gr�ficos salvos em Preferences.
        /// </summary>
        public static void ZeraDadosGraficos()
        {
            Preferences.Set("ListaSuperCategoriasJSON", "");
            Preferences.Set("DadosGraficosDispersaoJSON", "");
            Preferences.Set("lastsyncdashboard", DateTime.MinValue);
        }

        /// <summary>
        /// Dictionary com scores m�ximos por SuperCategoria.
        /// </summary>
        private readonly Dictionary<string, double> ScoreMaximoSuperCategoria = new Dictionary<string, double>
        {
            {"2", 15},   // Respirat�rio
            {"5", 30},   // Coccidiose
            {"4", 54},   // Intestino            
            {"1", 12},   // Locomotor
            {"3", 51},   // Outros �rg�os
        };

        /// <summary>
        /// Obt�m o score m�ximo para uma SuperCategoria.
        /// </summary>
        public double PegaScoreMaximoSuperCategoria(string id, string name)
        {
            if (!string.IsNullOrEmpty(id) && ScoreMaximoSuperCategoria.ContainsKey(id))
                return ScoreMaximoSuperCategoria[id];

            // Fallback caso o ID n�o seja encontrado (evita crash)
            Debug.WriteLine($"[AVISO] ScoreMaximoSuperCategoria n�o encontrado para ID: {id} (Nome: {name})");
            return 100; // Retorna um padr�o seguro
        }
    }
}
