using SilvaData.Models;
using System.Diagnostics;
using Newtonsoft.Json;

namespace SilvaData.Services
{
    /// <summary>
    /// Servi�o respons�vel por buscar e armazenar em cache os dados
    /// dos cart�es de m�dia do Dashboard (HomeViewModel).
    /// </summary>
    public class HomeService
    {
        private readonly ISIWebService _webService;

        public HomeService(ISIWebService webService)
        {
            _webService = webService;
        }

        /// <summary>
        /// Busca os dados de m�dia dos cart�es do dashboard,
        /// primeiramente do cache, ou (se for�ado) da web.
        /// </summary>
        /// <param name="forceWebFetch">Se true, ignora o cache e busca na web.</param>
        public async Task<DashboardMedia> AtualizaDadosMediaAsync(bool forceWebFetch = false)
        {
            // 1. Tenta carregar do cache primeiro
            if (!forceWebFetch)
            {
                var dadosDashboardJson = Preferences.Get("DadosDashboard", "");
                if (!string.IsNullOrEmpty(dadosDashboardJson))
                {
                    try
                    {
                        var cachedData = JsonConvert.DeserializeObject<DashboardMedia>(dadosDashboardJson);
                        if (cachedData != null)
                            return cachedData;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Falha ao ler cache do DashboardMedia: {ex.Message}");
                    }
                }
            }

            // 2. Se o cache falhar, for for�ado, ou estiver vazio, busca na Web
            if (_webService.LoggedUser == null)
            {
                Debug.WriteLine("HomeService: Usu�rio n�o logado, retornando dados vazios.");
                return new DashboardMedia(); // Retorna vazio se n�o estiver logado
            }

            try
            {
                var getDashboardMediaParametros = new pegaGraficosParametros
                {
                    dispositivoId = Preferences.Get("my_id", string.Empty),
                    session = _webService.LoggedUser.session,
                    usuario = _webService.LoggedUser.id
                };

                var jsonParams = JsonConvert.SerializeObject(getDashboardMediaParametros);
                using var requestBody = new StringContent(EncryptDecrypt.Encrypt(jsonParams));

                // Esta � a l�gica que estava no DashboardMedia.AtualizaDados()
                ISIWebServiceResult result = await _webService.ExecutePostAndWaitResult(requestBody, "getDashboardMedia", "dashboard")
                    .ConfigureAwait(false);

                if (!result.sucesso || string.IsNullOrEmpty(result.data))
                {
                    return new DashboardMedia(); // Retorna vazio em caso de falha
                }

                Preferences.Set("DadosDashboard", result.data); // Salva o novo cache
                return JsonConvert.DeserializeObject<DashboardMedia>(result.data) ?? new DashboardMedia();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Falha ao buscar DashboardMedia: {ex.Message}");
                SentryHelper.CaptureExceptionWithUser(ex, ISIWebService.Instance.LoggedUser.nome, "AtualizaDadosMediaAsync");
                return new DashboardMedia(); // Retorna vazio em caso de erro
            }
        }
    }
}
