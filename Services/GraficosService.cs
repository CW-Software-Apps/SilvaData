using SilvaData.Models;
using SilvaData.Utilities;
using SilvaData.ViewModels;

using Newtonsoft.Json;

using System.Collections.ObjectModel;
using System.Diagnostics;

namespace SilvaData.Services
{
    /// <summary>
    /// Servi�o respons�vel por baixar, processar e cachear dados de gr�ficos.
    /// </summary>
    public class GraficosService
    {
        public Graficos Graficos { get; private set; } = new();

        public HomeViewModel HomeViewModel;

        public GraficosService(HomeViewModel homeViewModel)
        {
            HomeViewModel = homeViewModel;

            CarregaGraficosDoCache();
        }

        public async Task<string> AtualizaDadosGraficos(bool showError = true)
        {
            var result = await Graficos.DownloadDadosGraficos().ConfigureAwait(false);

            if (!result.Equals("ok"))
            {
                if (showError)
                {
                    return $"{Traducao.NaoFoiPossivelAtualizarDadosGraficos} - {result}";
                }

                Graficos.ListaSuperCategorias ??= new List<ObservableCollection<GraficoSuperCategoriaAgrupado>>();
                Graficos.DadosGraficosDispersao ??= new ObservableCollection<GraficoDispersao>();
            }
            else
            {
                SalvaGraficosNoCache();
                _ = Task.Run(HomeViewModel.AtualizaMedia);
            }
            return string.Empty;
        }

        private void CarregaGraficosDoCache()
        {
            var ListaSuperCategoriasJSON = Preferences.Get("ListaSuperCategoriasJSON", "");
            var DadosGraficosDispersaoJSON = Preferences.Get("DadosGraficosDispersaoJSON", "");

            try
            {
                if (!string.IsNullOrEmpty(ListaSuperCategoriasJSON))
                    Graficos.ListaSuperCategorias = JsonConvert.DeserializeObject<List<ObservableCollection<GraficoSuperCategoriaAgrupado>>>(ListaSuperCategoriasJSON) ?? new();
                else
                    Graficos.ListaSuperCategorias = new List<ObservableCollection<GraficoSuperCategoriaAgrupado>>();

                if (!string.IsNullOrEmpty(DadosGraficosDispersaoJSON))
                    Graficos.DadosGraficosDispersao = JsonConvert.DeserializeObject<ObservableCollection<GraficoDispersao>>(DadosGraficosDispersaoJSON) ?? new();
                else
                    Graficos.DadosGraficosDispersao = new ObservableCollection<GraficoDispersao>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Falha ao carregar gr�ficos do cache: {ex.Message}");
                Graficos.ListaSuperCategorias = new List<ObservableCollection<GraficoSuperCategoriaAgrupado>>();
                Graficos.DadosGraficosDispersao = new ObservableCollection<GraficoDispersao>();
            }
        }

        private void SalvaGraficosNoCache()
        {
            try
            {
                string listaSuperCategoriasJSON = JsonConvert.SerializeObject(Graficos.ListaSuperCategorias);
                string dadosGraficosDispersaoJSON = JsonConvert.SerializeObject(Graficos.DadosGraficosDispersao);

                Preferences.Set("ListaSuperCategoriasJSON", listaSuperCategoriasJSON);
                Preferences.Set("DadosGraficosDispersaoJSON", dadosGraficosDispersaoJSON);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Falha ao salvar gr�ficos no cache: {ex.Message}");
            }
        }
    }
}
