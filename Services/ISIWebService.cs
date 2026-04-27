using CommunityToolkit.Mvvm.ComponentModel;

using SilvaData.Infrastructure; // Para ServiceHelper
using SilvaData.Models; // Para os DTOs
using SilvaData.Pages.PopUps; // Para HtmlErrorPopup
using SilvaData.Utilities; // Para EncryptDecrypt, IResizeImageCommand

using Newtonsoft.Json;

using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Storage;

namespace SilvaData.Utils
{
    #region Exceptions & Enums

    public class WebServiceHtmlErrorException : Exception
    {
        public string HtmlContent { get; }
        public string MethodName { get; }

        public WebServiceHtmlErrorException(string message, string htmlContent, string methodName) : base(message)
        {
            HtmlContent = htmlContent;
            MethodName = methodName;
        }
    }

    public enum LoginResult
    {
        Ok,
        Wrong,
        FailedWebService
    }

    public enum TipoEstadoSessao
    {
        TemSessaoValida,
        NaoTemSessaoValida,
        Desconhecida
    }

    #endregion

    /// <summary>
    /// Serviço central para comunicação com o ISI Web Service.
    /// </summary>
    public partial class ISIWebService : ObservableObject
    {
        #region Constants & Fields

        public const string UrlAcesso = "https://webapp.silvateam.com.br/wssilva/app/request.cfc";
        private const string AccessKey = "21479726A36B71FCAD673DB9CD495AB32E2E65DAEE6BCA7EE9EBC5F0D43BED88";
        private const int DefaultTimeoutSeconds = 30;
        private const int UploadTimeoutSeconds = 120;

        private static ISIWebService? _instance;
        private readonly HttpClient _client;
        private readonly IResizeImageCommand _resizeImageService;
        private readonly CircuitBreaker _circuitBreaker;

        #endregion

        #region Properties

        [ObservableProperty]
        private dataLoginResult? _loggedUser;

        /// <summary>
        /// Singleton via ServiceHelper para compatibilidade com chamadas estáticas, mas preferencialmente via DI.
        /// </summary>
        public static ISIWebService Instance => _instance ??= ServiceHelper.GetRequiredService<ISIWebService>();

        #endregion

        #region Constructor
        public ISIWebService(IResizeImageCommand resizeImageService)
        {
            _instance = this;
            _resizeImageService = resizeImageService;

            // Carrega o usuário logado (se existir) dos Preferences logo no início
            CheckLoggedUser();

            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            };

            _client = new HttpClient(handler);

            // --- CORREÇÃO CRÍTICA PARA ERRO 500 EM UPLOAD ---
            // Evita que o cliente espere um "100-Continue" do servidor antes de enviar o corpo.
            // Servidores antigos costumam travar ou dar timeout com isso.
            _client.DefaultRequestHeaders.ExpectContinue = false;
            // ------------------------------------------------

            _client.DefaultRequestHeaders.TryAddWithoutValidation("accessKey", AccessKey);
            _client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "RestSharp/106.11.7.0");
            _client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "*/*"); // Aceita tudo (JSON, HTML, Imagens)
            _client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate");

            // Inicializa Circuit Breaker para ISI Web Service
            _circuitBreaker = CircuitBreakerManager.GetCircuitBreaker("ISIWebService", failureThreshold: 5, openTimeout: TimeSpan.FromMinutes(2));
        }

        #endregion

        #region Auth Methods (Login, Logout, Session)

        public async Task<LoginResult> Login(string username, string password)
        {
            var id = Preferences.Get("my_id", string.Empty);
            if (string.IsNullOrWhiteSpace(id))
            {
                id = Guid.NewGuid().ToString();
                Preferences.Set("my_id", id);
            }

            var dataLogin = new dataLoginParameters
            {
                email = username,
                senha = password,
                dispositivoId = id,
                dispositivoDescricao = DeviceInfo.Model
            };

            // Prepara corpo criptografado
            using var requestBody = new StringContent(EncryptDecrypt.Encrypt(JsonConvert.SerializeObject(dataLogin)));

            var result = await ExecutePostAndWaitResult(requestBody, "login", "usuario").ConfigureAwait(false);

            if (result.data == "erro") return LoginResult.FailedWebService;
            if (!result.sucesso) return LoginResult.Wrong;

            try
            {
                var userinfo = JsonConvert.DeserializeObject<dataLoginResult>(result.data);
                if (userinfo == null) return LoginResult.FailedWebService;

                Preferences.Set("user", result.data); // Salva o JSON bruto desencriptado
                LoggedUser = userinfo;

                if (!string.IsNullOrEmpty(LoggedUser.urlImagem))
                {
                    Debug.WriteLine($"Downloading {LoggedUser.urlImagem}");
                    // Download em background, sem bloquear o retorno do login
                    _ = DownloadImage(0, LoggedUser.urlImagem, null, CancellationToken.None, "logo.jpg");
                }

                return LoginResult.Ok;
            }
            catch (Exception ex)
            {
                SentryHelper.CaptureExceptionWithUser(ex, "Login_Deserialize");
                return LoginResult.FailedWebService;
            }
        }

        public async Task<checkSessionAtivaResult?> checkSessionAtiva(string username, string password)
        {
            var dataLogin = new checkSessionAtivaParameters { email = username, senha = password };
            using var requestBody = new StringContent(EncryptDecrypt.Encrypt(JsonConvert.SerializeObject(dataLogin)));

            var result = await ExecutePostAndWaitResult(requestBody, "checkSessionAtiva", "session").ConfigureAwait(false);

            if (!result.sucesso) return null;
            return JsonConvert.DeserializeObject<checkSessionAtivaResult>(result.data);
        }

        public void CheckLoggedUser()
        {
            var userJson = Preferences.Get("user", null);
            LoggedUser = string.IsNullOrEmpty(userJson)
                ? null
                : JsonConvert.DeserializeObject<dataLoginResult>(userJson);
        }

        public async Task<bool> LogOut(bool limpaTudo = true)
        {
            try
            {
                if (LoggedUser != null)
                {
                    var dataLogin = new checkUserSessionAtivaParameters
                    {
                        usuario = LoggedUser.id,
                        dispositivoId = LoggedUser.dispositivoId,
                        session = LoggedUser.session
                    };
                    using var requestBody = new StringContent(EncryptDecrypt.Encrypt(JsonConvert.SerializeObject(dataLogin)));
                    // Best-effort logout
                    await ExecutePostAndWaitResult(requestBody, "logout").ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Logout] Erro no servidor: {ex.Message}");
            }

            LoggedUser = null;

            if (limpaTudo)
            {
                await LimpaDadosLocaisAsync().ConfigureAwait(false);
            }
            else
            {
                Preferences.Set("user", "");
                Preferences.Set("lastsyncdatetime", DateTime.MinValue);
            }
            return true;
        }

        internal async Task<TipoEstadoSessao> CheckSessionAindaAtiva()
        {
            if (LoggedUser == null) return TipoEstadoSessao.NaoTemSessaoValida;
            try
            {
                var parameters = new checkUserSessionAtivaParameters
                {
                    usuario = LoggedUser.id,
                    dispositivoId = LoggedUser.dispositivoId,
                    session = LoggedUser.session
                };

                using var requestBody = new StringContent(EncryptDecrypt.Encrypt(JsonConvert.SerializeObject(parameters)));
                var result = await ExecutePostAndWaitResult(requestBody, "checkSession", "session").ConfigureAwait(false);

                if (!result.sucesso) return TipoEstadoSessao.Desconhecida;

                var sessionInfo = JsonConvert.DeserializeObject<checkSessionAtivaResult>(result.data);
                return sessionInfo?.status == 1
                    ? TipoEstadoSessao.TemSessaoValida
                    : TipoEstadoSessao.NaoTemSessaoValida;
            }
            catch
            {
                return TipoEstadoSessao.Desconhecida;
            }
        }

        #endregion

        #region Data Transmission (Generic & Multipart)

        /// <summary>
        /// Executa chamada POST padrão (StringContent criptografado).
        /// </summary>
        public async Task<ISIWebServiceResult> ExecutePostAndWaitResult(StringContent requestBody, string methodname, string rootnametoremove = "", bool mostraErro = false, Action<long, long>? progress = null)
        {
            var requestId = Guid.NewGuid().ToString()[..8];
            string requestUrl = $"{UrlAcesso}?method={methodname}";

            int maxRetries = 3;
            int currentRetry = 0;

            // Lemos o conteúdo original caso precisemos recriar o StringContent no retry
            string bodyContent = await requestBody.ReadAsStringAsync().ConfigureAwait(false);

            while (true)
            {
                currentRetry = 0;
                while (currentRetry < maxRetries)
                {
                    try
                    {
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(DefaultTimeoutSeconds));

                        // Log
                        Debug.WriteLine($"[REQ:{requestId}] POST {methodname} (Tentativa {currentRetry + 1}/{maxRetries})");

                        // Precisamos de um novo HttpContent a cada tentativa, pois o HttpClient pode dispor o conteúdo consumido
                        using var contentToPost = new StringContent(bodyContent);
                        contentToPost.Headers.ContentType = requestBody.Headers.ContentType;

                        var response = await _client.PostAsync(new Uri(requestUrl), contentToPost, cts.Token).ConfigureAwait(false);

                        // Reportar progresso se necessário (baseado no ContentLength da resposta)
                        if (progress != null && response.Content.Headers.ContentLength.HasValue)
                        {
                            progress.Invoke(response.Content.Headers.ContentLength.Value, response.Content.Headers.ContentLength.Value);
                        }

                        // Processamento centralizado
                        var result = await ProcessResponseAndDecryptAsync(response, methodname, requestId, rootnametoremove).ConfigureAwait(false);

                        return result;
                    }
                    catch (Exception ex)
                    {
                        bool isTransient = ex is System.Net.Sockets.SocketException ||
                                           ex is System.IO.IOException ||
                                           ex is HttpRequestException ||
                                           ex is TimeoutException ||
                                           ex is OperationCanceledException;
                        if (isTransient && currentRetry < maxRetries - 1)
                        {
                            currentRetry++;
                            Debug.WriteLine($"[REQ:{requestId}] Erro de rede ( {ex.Message} ). Agendando tentativa {currentRetry + 1}...");
                            await Task.Delay(TimeSpan.FromSeconds(2 * currentRetry)).ConfigureAwait(false);
                            continue;
                        }

                        var exceptionResult = await HandleRequestExceptionAsync(ex, methodname, requestId, mostraErro);
                        if (exceptionResult.RetryRequested)
                        {
                            // Se o usuário pediu retry manual, reiniciamos o loop principal
                            Debug.WriteLine($"[REQ:{requestId}] Usuário solicitou RE-TENTATIVA manual.");
                            break; // Sai do loop de retries automáticos
                        }
                        return exceptionResult;
                    }
                }
                // Se saiu do loop de retries automáticos por causa do RetryRequested, o while(true) continua
            }
        }

        /// <summary>
        /// Envia JSON genérico (wrapper para tratamento de strings específicas antes de criptografar).
        /// </summary>
        public async Task<ISIWebServiceResult> SendData(string jsonparameters, string methodname)
        {
            // Correções de JSON legado
            jsonparameters = jsonparameters.Replace("\\", "")
                                           .Replace("array\":\"", "array\":")
                                           .Replace("}]\"", "}]");

            var encryptedjsonparameters = EncryptDecrypt.Encrypt(jsonparameters);

            try
            {
                using var requestBody = new StringContent(encryptedjsonparameters);
                // Reutiliza a lógica central
                return await ExecutePostAndWaitResult(requestBody, methodname).ConfigureAwait(false);
            }
            catch (JsonException)
            {
                // Tratamento específico para erro de JSON solicitado no código original
                await HandleJsonSerializationError(encryptedjsonparameters).ConfigureAwait(false);
                return new ISIWebServiceResult { sucesso = false, data = "Erro de serialização JSON", mensagem = "Erro de serialização JSON" };
            }
            catch (Exception e)
            {
                SentryHelper.CaptureExceptionWithUser(e);
                return new ISIWebServiceResult { sucesso = false, data = e.Message, mensagem = e.Message };
            }
        }

        /// <summary>
        /// Envia dados complexos com múltiplas imagens via MultipartFormData.
        /// </summary>
        public async Task<ISIWebServiceResult> SendDataWithImages(
            UpdateDataParametrosLoteFormImagem jsonparameters,
            string methodname,
            string image1,
            string image2,
            string image3)
        {
            var requestId = Guid.NewGuid().ToString()[..8];
            var url = $"{UrlAcesso}?method={methodname}";

            try
            {
                Debug.WriteLine($"[REQ:{requestId}] Iniciando upload de imagens ({jsonparameters.qtdeImagens}) para {methodname}");

                using var formData = new MultipartFormDataContent();

                // Adiciona campos criptografados
                AddEncryptedField(formData, "usuario", jsonparameters.usuario);
                AddEncryptedField(formData, "dispositivoId", jsonparameters.dispositivoId);
                AddEncryptedField(formData, "session", jsonparameters.session);
                AddEncryptedField(formData, "id", jsonparameters.id.ToString());
                AddEncryptedField(formData, "idApp", jsonparameters.idApp.ToString());
                AddEncryptedField(formData, "qtdImagens", jsonparameters.qtdeImagens.ToString());

                // Processa Imagens
                var imagePaths = new[] {
                    (path: image1, name: "fileImagem_1"),
                    (path: image2, name: "fileImagem_2"),
                    (path: image3, name: "fileImagem_3")
                };

                foreach (var img in imagePaths)
                {
                    await AddImageToFormAsync(formData, img.path, img.name, requestId, methodname).ConfigureAwait(false);
                }

                // Executa Requisição
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(UploadTimeoutSeconds));
                var stopwatch = Stopwatch.StartNew();

                try
                {
                    Debug.WriteLine("============= INSPEÇÃO DO PACOTE MULTIPART =============");
                    // Imprime Headers Globais
                    Debug.WriteLine($"URL: {url}");
                    foreach (var h in _client.DefaultRequestHeaders)
                        Debug.WriteLine($"Header Global: {h.Key} = {string.Join(", ", h.Value)}");

                    // Imprime Partes do Corpo
                    foreach (var content in formData)
                    {
                        Debug.WriteLine("--- PARTE ---");
                        foreach (var h in content.Headers)
                        {
                            Debug.WriteLine($"{h.Key}: {string.Join(", ", h.Value)}");
                        }

                        // Tenta ler o conteúdo se for texto (cuidado com binário)
                        if (content.Headers.ContentType == null || content.Headers.ContentType.MediaType == "text/plain")
                        {
                            var str = await content.ReadAsStringAsync();
                            Debug.WriteLine($"VALOR: {str}");
                        }
                        else
                        {
                            Debug.WriteLine($"VALOR: [DADOS BINÁRIOS/IMAGEM]");
                        }
                    }
                    Debug.WriteLine("========================================================");
                }
                catch (Exception logEx) { Debug.WriteLine($"Erro no log: {logEx.Message}"); }

                var response = await _client.PostAsync(url, formData, cts.Token).ConfigureAwait(false);

                stopwatch.Stop();
                Debug.WriteLine($"[REQ:{requestId}] Upload {methodname} finalizado em {stopwatch.ElapsedMilliseconds}ms - Status: {response.StatusCode}");

                var result = await ProcessResponseAndDecryptAsync(response, methodname, requestId).ConfigureAwait(false);

                // Processa Resposta (usa o mesmo pipeline do ExecutePost)
                if (!result.sucesso)
                {
                    Debug.WriteLine($"[REQ:{requestId}] Erro no upload {methodname}: {result.mensagem} {result.data}");
                    await PopUpOK.ShowAsync("Erro de Upload", $"Ocorreu um erro ao enviar as imagens. Erro : {result.mensagem}");
                }

                return result;

            }
            catch (Exception ex)
            {
                return await HandleRequestExceptionAsync(ex, methodname, requestId, false);
            }
        }

        public async Task SalvaNotaNPS(int nota)
        {
            if (LoggedUser == null) return;

            var dataPostNPS = new postNpsParameters
            {
                usuario = LoggedUser.id,
                session = LoggedUser.session,
                dispositivoId = LoggedUser.dispositivoId,
                npsData = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                nps = $"{nota}"
            };

            if (!string.IsNullOrEmpty(LoggedUser.npsData))
                dataPostNPS.npsData = LoggedUser.npsData;

            var json = JsonConvert.SerializeObject(dataPostNPS);

            if (Connectivity.NetworkAccess == NetworkAccess.Internet)
            {
                using var requestBody = new StringContent(EncryptDecrypt.Encrypt(json));
                var result = await ExecutePostAndWaitResult(requestBody, "postNps").ConfigureAwait(false);
                LoggedUser.npsPrecisaEnviar = !result.sucesso;
            }
            else
            {
                LoggedUser.npsPrecisaEnviar = true;
            }

            LoggedUser.nps = dataPostNPS.nps;
            LoggedUser.npsData = dataPostNPS.npsData;
            Preferences.Set("user", JsonConvert.SerializeObject(LoggedUser));
        }

        public async Task SalvaTermosAceitePrivacidade()
        {
            if (LoggedUser == null) return;

            var dataPostTermos = new postTermosParameters
            {
                usuario = LoggedUser.id,
                dispositivoId = LoggedUser.dispositivoId,
                session = LoggedUser.session,
                dataAceite = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                dispositivoDescricao = $"{DeviceInfo.Manufacturer} {DeviceInfo.Name} - {DeviceInfo.Model}"
            };

            var json = JsonConvert.SerializeObject(dataPostTermos);
            using var requestBody = new StringContent(EncryptDecrypt.Encrypt(json));

            var result = await ExecutePostAndWaitResult(requestBody, "postTermos").ConfigureAwait(false);

            if (result.sucesso)
            {
                LoggedUser.dataAceiteApp = dataPostTermos.dataAceite;
                LoggedUser.dispositivoDescricaoAceite = dataPostTermos.dispositivoDescricao;
                LoggedUser.dispositivoIdAceite = dataPostTermos.dispositivoId;
                Preferences.Set("user", JsonConvert.SerializeObject(LoggedUser));
            }
        }

        #endregion

        #region File & Image Methods

        public async Task<bool> DownloadImage(int taskNumber, string url, IProgress<float>? progress, CancellationToken cancellationToken, string customFileName = "")
        {
            var documentsPath = FileSystem.AppDataDirectory;
            var localFilename = string.IsNullOrEmpty(customFileName) ? Path.GetFileName(new Uri(url).LocalPath) : customFileName;
            var localPath = Path.Combine(documentsPath, localFilename);

            // Evita re-download de imagens que já existem no disco (cache simples por nome de arquivo).
            if (File.Exists(localPath) && new FileInfo(localPath).Length > 0)
            {
                Debug.WriteLine($"[{taskNumber}] Imagem já existe: {localFilename}");
                progress?.Report(100);
                return true;
            }

            int maxRetries = 3;
            int currentRetry = 0;

            while (currentRetry < maxRetries)
            {
                // Guid único por tentativa evita conflito de nome entre downloads paralelos do mesmo arquivo.
                string tempPath = $"{localPath}.tmp_{Guid.NewGuid()}";
                
                // Timeout individual de 30 segundos por imagem para evitar travamento da sub-tarefa.
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(30));

                try
                {
                    Debug.WriteLine($"[{taskNumber}] Download {currentRetry + 1}/{maxRetries} - {url}");

                    // Criado dentro do loop: no Android 12+ o filesystem FUSE pode ainda não ter
                    // confirmado o diretório criado antes do loop quando há muitos downloads paralelos.
                    // Garante que o diretório pai (que pode incluir subpasta em localFilename) exista.
                    var directoryPath = Path.GetDirectoryName(localPath);
                    if (!string.IsNullOrEmpty(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }

                    // Grava em arquivo temporário para evitar que falhas parciais deixem
                    // o arquivo de destino corrompido. Só é movido após download completo.
                    using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, useAsync: true))
                    {
                        var success = await _client.DownloadAsync(url, fileStream, progress, cts.Token).ConfigureAwait(false);
                        if (!success) throw new Exception("DownloadAsync retornou falso");
                    }

                    if (new FileInfo(tempPath).Length > 0)
                    {
                        if (File.Exists(localPath)) File.Delete(localPath);
                        File.Move(tempPath, localPath);
                        progress?.Report(100);
                        return true;
                    }
                    else
                    {
                        // DownloadAsync retornou sucesso mas o arquivo ficou vazio — avança retry
                        // explicitamente (sem isso o loop seria infinito).
                        currentRetry++;
                        Debug.WriteLine($"[{taskNumber}] Arquivo recebido vazio, retry {currentRetry}/{maxRetries}");
                    }
                }
                // Timeout disparado pelo cts.CancelAfter(30s).
                // Se o cancellationToken (do usuário) não foi cancelado, então foi um timeout da nossa tarefa.
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    currentRetry++;
                    Debug.WriteLine($"[{taskNumber}] Timeout no download (30s atingido), retry {currentRetry}/{maxRetries}");
                    await Task.Delay(1000 * currentRetry, cancellationToken).ConfigureAwait(false);
                }
                // OperationCanceledException excluída: cancelamento do usuário não deve
                // ser tratado como erro e não deve acionar retentativas.
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    currentRetry++;
                    Debug.WriteLine($"[{taskNumber}] Erro retry {currentRetry}: {ex.Message}");
                    if (currentRetry >= maxRetries)
                    {
                        if (!IsNetworkException(ex))
                        {
                            SentryHelper.CaptureExceptionWithUser(ex, url);
                        }
                    }
                    await Task.Delay(1000 * currentRetry, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    // Limpeza garantida do temp
                    if (File.Exists(tempPath)) try { File.Delete(tempPath); } catch { }
                }
            }

            return false;
        }

        #endregion

        #region Advanced Error Handling (New System)

        /// <summary>
        /// Executa requisição com Circuit Breaker e logging avançado
        /// </summary>
        private async Task<ISIWebServiceResult> ExecuteWithCircuitBreakerAsync(
            Func<Task<ISIWebServiceResult>> operation,
            string methodName,
            string requestId,
            string requestUrl = "",
            string requestPayload = "",
            string requestPayloadDecrypted = "")
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                // Verifica se o circuit breaker permite execução
                if (!_circuitBreaker.CanExecute())
                {
                    var errorDetails = ErrorLogger.CreateFromException(
                        new CircuitBreakerOpenException($"Circuit breaker aberto para {methodName}"),
                        methodName,
                        requestId,
                        requestUrl,
                        requestPayload,
                        requestPayloadDecrypted,
                        null,
                        "Circuit breaker aberto - serviço temporariamente indisponível",
                        DateTime.UtcNow - startTime);

                    await ErrorLogger.LogErrorAsync(errorDetails);
                    
                    // Mostra popup de erro detalhado
                    await ShowDetailedErrorAsync(errorDetails);
                    
                    return new ISIWebServiceResult 
                    { 
                        sucesso = false, 
                        data = "Serviço temporariamente indisponível. Aguarde alguns minutos.",
                        mensagem = "Circuit breaker aberto"
                    };
                }

                // Executa através do circuit breaker
                var result = await _circuitBreaker.ExecuteAsync(
                    async () =>
                    {
                        var operationResult = await operation();
                        
                        if (!operationResult.sucesso)
                        {
                            throw new Exception(operationResult.mensagem ?? "Erro na operação");
                        }
                        
                        return operationResult;
                    },
                    // Fallback: retorna resultado de cache se disponível
                    async () =>
                    {
                        Debug.WriteLine($"[CircuitBreaker] Usando fallback para {methodName}");
                        return new ISIWebServiceResult 
                        { 
                            sucesso = false, 
                            data = "Serviço temporariamente indisponível. Usando dados em cache quando possível.",
                            mensagem = "Fallback ativado"
                        };
                    });

                return result;
            }
            catch (CircuitBreakerOpenException ex)
            {
                // Tratamento específico para circuit breaker aberto
                var errorDetails = ErrorLogger.CreateFromException(
                    ex,
                    methodName,
                    requestId,
                    requestUrl,
                    requestPayload,
                    requestPayloadDecrypted,
                    null,
                    ex.Message,
                    DateTime.UtcNow - startTime);

                await ErrorLogger.LogErrorAsync(errorDetails);
                
                return new ISIWebServiceResult 
                { 
                    sucesso = false, 
                    data = "Serviço temporariamente indisponível. Tente novamente em alguns minutos.",
                    mensagem = ex.Message
                };
            }
            catch (Exception ex)
            {
                // Log detalhado do erro
                var errorDetails = ErrorLogger.CreateFromException(
                    ex,
                    methodName,
                    requestId,
                    requestUrl,
                    requestPayload,
                    requestPayloadDecrypted,
                    null,
                    await GetResponseContentAsync(ex),
                    DateTime.UtcNow - startTime);

                await ErrorLogger.LogErrorAsync(errorDetails);
                
                // Mostra popup detalhado se for erro crítico
                if (errorDetails.ErrorType == ErrorType.ServerError || 
                    errorDetails.ErrorType == ErrorType.DecryptionError ||
                    errorDetails.ErrorType == ErrorType.SerializationError)
                {
                    await ShowDetailedErrorAsync(errorDetails);
                }
                
                return new ISIWebServiceResult 
                { 
                    sucesso = false, 
                    data = errorDetails.UserFriendlyMessage,
                    mensagem = errorDetails.UserFriendlyMessage
                };
            }
        }

        /// <summary>
        /// Mostra popup de erro detalhado baseado no tipo de erro
        /// </summary>
        private async Task ShowDetailedErrorAsync(ErrorDetails errorDetails)
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    try
                    {
                        if (errorDetails.ErrorType == ErrorType.HtmlError)
                        {
                            // Usa popup existente para erros HTML
                            var popup = new HtmlErrorPopup(errorDetails.ResponseContent, errorDetails.MethodName);
                            await NavigationUtils.ShowPageAsModalAsync(popup).ConfigureAwait(false);
                        }
                        else
                        {
                            // Usa novo popup detalhado para outros erros
                            var popup = new DetailedErrorPopup(errorDetails);
                            await NavigationUtils.ShowPageAsModalAsync(popup).ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ShowDetailedErrorAsync] Erro ao exibir popup: {ex.Message}");
                        // Fallback: mostra popup simples
                        await PopUpOK.ShowAsync("Erro", errorDetails.UserFriendlyMessage);
                    }
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ShowDetailedErrorAsync] Erro geral: {ex.Message}");
            }
        }

        /// <summary>
        /// Tenta extrair conteúdo da resposta de uma exceção
        /// </summary>
        private async Task<string> GetResponseContentAsync(Exception ex)
        {
            try
            {
                if (ex is HttpRequestException httpEx)
                {
                    return httpEx.Message;
                }
                
                if (ex is WebServiceHtmlErrorException htmlEx)
                {
                    return htmlEx.HtmlContent;
                }
                
                return ex.Message;
            }
            catch
            {
                return ex.Message;
            }
        }

        /// <summary>
        /// Obtém estatísticas do circuit breaker para monitoramento
        /// </summary>
        public (CircuitState State, int Failures, (int Total, int Successful, double SuccessRate, TimeSpan AvgResponseTime) Stats) GetCircuitBreakerStats()
        {
            return (_circuitBreaker.State, _circuitBreaker.FailureCount, _circuitBreaker.GetStatistics());
        }

        /// <summary>
        /// Reset manual do circuit breaker (para uso administrativo)
        /// </summary>
        public void ResetCircuitBreaker()
        {
            _circuitBreaker.Reset();
            Debug.WriteLine("[ISIWebService] Circuit breaker resetado manualmente");
        }

        /// <summary>
        /// Executa diagnóstico completo do sistema de erros
        /// </summary>
        public async Task<string> RunDiagnosticAsync()
        {
            var diagnostic = new StringBuilder();
            diagnostic.AppendLine("=== DIAGNÓSTICO ISI WEB SERVICE ===");
            diagnostic.AppendLine($"Data/Hora: {DateTime.Now:yyyy-MM-dd HH:mm:ss} UTC");
            diagnostic.AppendLine();

            // 1. Status do Circuit Breaker
            var (state, failures, stats) = GetCircuitBreakerStats();
            diagnostic.AppendLine("--- CIRCUIT BREAKER ---");
            diagnostic.AppendLine($"Estado: {state}");
            diagnostic.AppendLine($"Falhas: {failures}");
            diagnostic.AppendLine($"Total Requisições: {stats.Total}");
            diagnostic.AppendLine($"Sucesso: {stats.Successful}");
            diagnostic.AppendLine($"Taxa Sucesso: {stats.SuccessRate:P2}");
            diagnostic.AppendLine($"Tempo Médio Resposta: {stats.AvgResponseTime.TotalMilliseconds:F0}ms");
            diagnostic.AppendLine();

            // 2. Logs Recentes
            var recentErrors = ErrorLogger.GetRecentErrors(10);
            diagnostic.AppendLine("--- ERROS RECENTES (Últimos 10) ---");
            if (recentErrors.Any())
            {
                foreach (var error in recentErrors.TakeLast(5))
                {
                    diagnostic.AppendLine($"[{error.Timestamp:HH:mm:ss}] {error.MethodName} - {error.ErrorType}: {error.UserFriendlyMessage}");
                }
            }
            else
            {
                diagnostic.AppendLine("Nenhum erro recente.");
            }
            diagnostic.AppendLine();

            // 3. Status do Usuário
            diagnostic.AppendLine("--- STATUS USUÁRIO ---");
            if (LoggedUser != null)
            {
                diagnostic.AppendLine($"Usuário: {LoggedUser.nome} ({LoggedUser.email})");
                diagnostic.AppendLine($"ID: {LoggedUser.id}");
                diagnostic.AppendLine($"Device ID: {LoggedUser.dispositivoId}");
                diagnostic.AppendLine($"Sessão Ativa: {!string.IsNullOrEmpty(LoggedUser.session)}");
            }
            else
            {
                diagnostic.AppendLine("Nenhum usuário logado.");
            }
            diagnostic.AppendLine();

            // 4. Conectividade
            diagnostic.AppendLine("--- CONECTIVIDADE ---");
            diagnostic.AppendLine($"Network Access: {Connectivity.NetworkAccess}");
            diagnostic.AppendLine($"Connection Profiles: {string.Join(", ", Connectivity.ConnectionProfiles)}");
            diagnostic.AppendLine();

            // 5. Memória
            diagnostic.AppendLine("--- MEMÓRIA ---");
            diagnostic.AppendLine($"Logs em Memória: {ErrorLogger.GetRecentErrors(1000).Count}");
            diagnostic.AppendLine($"Cache Directory: {FileSystem.CacheDirectory}");
            diagnostic.AppendLine();

            diagnostic.AppendLine("==========================");

            return diagnostic.ToString();
        }

        /// <summary>
        /// Realiza limpeza de manutenção (logs antigos, cache, etc.)
        /// </summary>
        public async Task PerformMaintenanceAsync()
        {
            try
            {
                Debug.WriteLine("[ISIWebService] Iniciando manutenção...");

                // 1. Limpa logs antigos
                ErrorLogger.CleanupOldLogs();
                Debug.WriteLine("[ISIWebService] Logs antigos limpos");

                // 2. Reseta circuit breaker se estiver aberto há muito tempo
                var (state, failures, _) = GetCircuitBreakerStats();
                if (state == CircuitState.Open && failures > 10)
                {
                    ResetCircuitBreaker();
                    Debug.WriteLine("[ISIWebService] Circuit breaker resetado por manutenção");
                }

                // 3. Limpa cache de imagens antigas (opcional)
                await CleanupOldImagesAsync();

                Debug.WriteLine("[ISIWebService] Manutenção concluída");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ISIWebService] Erro na manutenção: {ex.Message}");
            }
        }

        /// <summary>
        /// Limpa imagens antigas do cache
        /// </summary>
        private async Task CleanupOldImagesAsync()
        {
            try
            {
                var documentsPath = FileSystem.AppDataDirectory;
                var imageFiles = Directory.GetFiles(documentsPath, "*.jpg")
                    .Concat(Directory.GetFiles(documentsPath, "*.png"))
                    .Concat(Directory.GetFiles(documentsPath, "*.jpeg"));

                var cutoffDate = DateTime.Now.AddDays(-7); // Mantém 7 dias
                var cleanedCount = 0;

                foreach (var file in imageFiles)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        try
                        {
                            File.Delete(file);
                            cleanedCount++;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[ISIWebService] Erro ao deletar imagem {file}: {ex.Message}");
                        }
                    }
                }

                if (cleanedCount > 0)
                {
                    Debug.WriteLine($"[ISIWebService] {cleanedCount} imagens antigas removidas");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ISIWebService] Erro na limpeza de imagens: {ex.Message}");
            }
        }

        #endregion

        #region Internal Helpers (Core Logic)

        /// <summary>
        /// Processa a resposta HTTP bruta: verifica status, HTML error, GZIP manual (se necessário), deserializa e desencripta.
        /// </summary>
        private async Task<ISIWebServiceResult> ProcessResponseAndDecryptAsync(HttpResponseMessage response, string methodName, string requestId, string rootNameToRemove = "")
        {
            string rawResult = "";

            // 1. Leitura dos Bytes
            var responseBytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

            // 2. Verificação e Descompressão GZIP Manual (Fallback)
            // Verifica Magic Numbers do GZIP (0x1F 0x8B) caso o HttpClientHandler não tenha pego (ex: headers incorretos do server)
            if (responseBytes.Length >= 2 && responseBytes[0] == 0x1F && responseBytes[1] == 0x8B)
            {
                try
                {
                    using var compressedStream = new MemoryStream(responseBytes);
                    using var gzipStream = new System.IO.Compression.GZipStream(compressedStream, System.IO.Compression.CompressionMode.Decompress);
                    using var decompressedStream = new MemoryStream();
                    await gzipStream.CopyToAsync(decompressedStream).ConfigureAwait(false);
                    rawResult = Encoding.UTF8.GetString(decompressedStream.ToArray());
                }
                catch (Exception ex)
                {
                    SentryHelper.CaptureExceptionWithUser(ex, methodName, "GzipDecompressError");
                    throw;
                }
            }
            else
            {
                rawResult = Encoding.UTF8.GetString(responseBytes);
            }

            Debug.WriteLine($"[REQ:{requestId}] Response Size: {rawResult.Length} chars");

            // 3. Validação de Erro HTML (Servidor retornando página de erro 500/404 ao invés de JSON)
            if (!string.IsNullOrWhiteSpace(rawResult) && IsHtmlResponse(rawResult))
            {
                await ShowHtmlErrorModalAsync(rawResult, methodName).ConfigureAwait(false);
                throw new WebServiceHtmlErrorException("O servidor retornou HTML.", rawResult, methodName);
            }

            // 4. Validação de Status HTTP
            if (!response.IsSuccessStatusCode)
            {
                await SentryHelper.LogErrorAsync("ISIWebService.ProcessResponse",
                    $"Erro HTTP {methodName}",
                    $"Status: {response.StatusCode}, Content: {rawResult}").ConfigureAwait(false);

                return new ISIWebServiceResult { sucesso = false, data = $"HTTP {response.StatusCode}: {rawResult}" };
            }

            // 5. Deserialização do Wrapper
            var result = JsonConvert.DeserializeObject<ISIWebServiceResult>(rawResult);
            if (result == null) throw new Exception("JSON Response is null");

            // 5.1 Correção de encoding duplo no campo mensagem
            // O servidor às vezes retorna strings com encoding duplo (bytes UTF-8 interpretados como Latin-1 e recodificados).
            // Ex: "epidemiolÃ³gicas" em vez de "epidemiológicas".
            if (!string.IsNullOrEmpty(result.mensagem))
            {
                try { result.mensagem = Encoding.UTF8.GetString(Encoding.Latin1.GetBytes(result.mensagem)); }
                catch { /* mantém o valor original se a conversão falhar */ }
            }

            // 6. Descriptografia
            if (result.data != null)
            {
                try
                {
                    result.data = EncryptDecrypt.Decrypt(result.data);

                    // Extração de nó raiz opcional (ex: remover "session" wrapper)
                    if (!string.IsNullOrEmpty(rootNameToRemove))
                    {
                        var jsonNode = JsonNode.Parse(result.data);
                        result.data = jsonNode?[rootNameToRemove]?.ToString() ?? result.data;
                    }
                }
                catch (Exception ex)
                {
                    SentryHelper.CaptureExceptionWithUser(ex, methodName, result.data);
                    result.sucesso = false;
                    result.data = "Erro de descriptografia.";
                }
            }

            return result;
        }

        private async Task<ISIWebServiceResult> HandleRequestExceptionAsync(Exception ex, string methodName, string requestId, bool showUiError)
        {
            Debug.WriteLine($"[REQ:{requestId}] ERRO: {ex.Message}");

            string msg;
            bool isNetwork = IsNetworkException(ex);

            if (ex is OperationCanceledException)
            {
                msg = "Tempo limite excedido.";
                SentryHelper.CaptureExceptionWithUser(new TimeoutException(msg, ex), $"method={methodName}", $"req={requestId}");
            }
            else if (ex is WebServiceHtmlErrorException)
            {
                msg = "Erro interno do servidor (HTML).";
                // Já tratado/logado no throw
            }
            else if (isNetwork)
            {
                msg = "A conexão com o servidor foi interrompida ou está instável.";
                Debug.WriteLine($"[REQ:{requestId}] Erro de rede suprimido do Sentry: {ex.GetType().Name} - {ex.Message}");
            }
            else
            {
                msg = $"Erro: {ex.Message}";
                SentryHelper.CaptureExceptionWithUser(ex, $"method={methodName}", $"req={requestId}");
            }

            var result = new ISIWebServiceResult { sucesso = false, data = msg, mensagem = msg };

            if (showUiError)
            {
                // Atenção: Disparar UI thread a partir daqui
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (isNetwork)
                    {
                        // Se for erro de rede, oferece a opção de RE-TENTAR
                        bool retry = await PopUpYesNo.ShowAsync("Erro de Conexão", msg, "Tentar Novamente", "Cancelar");
                        result.RetryRequested = retry;
                    }
                    else
                    {
                        await PopUpOK.ShowAsync("Erro", msg);
                    }
                });
            }

            return result;
        }

        private bool IsNetworkException(Exception ex)
        {
            var exType = ex.GetType().Name;
            var innerExType = ex.InnerException?.GetType().Name;
            var msg = ex.Message.ToLowerInvariant();

            if (exType == "SocketException" || innerExType == "SocketException" ||
                exType == "HttpRequestException" || innerExType == "HttpRequestException" ||
                exType == "WebException" || innerExType == "WebException" ||
                exType == "IOException" || innerExType == "IOException" ||
                exType == "Java.Net.SocketException" || innerExType == "Java.Net.SocketException")
            {
                return true;
            }

            if (msg.Contains("socket closed") ||
                msg.Contains("connection abort") ||
                msg.Contains("connection reset") ||
                msg.Contains("network is unreachable") ||
                msg.Contains("no route to host") ||
                msg.Contains("net::err_") ||
                msg.Contains("the network connection was lost") ||
                msg.Contains("host is down") ||
                msg.Contains("connection failure") ||
                msg.Contains("no address associated with hostname") ||
                msg.Contains("unable to resolve host") ||
                msg.Contains("eai_nodata") ||
                msg.Contains("nodename nor servname provided"))
            {
                return true;
            }

            // Verificar inner exceptions recursivamente (Android encadeia Java exceptions)
            if (ex.InnerException != null && ex.InnerException != ex)
                return IsNetworkException(ex.InnerException);

            return false;
        }

        private static bool IsHtmlResponse(string raw)
        {
            // Procura nos primeiros 512 chars para cobrir BOM, whitespace e output de debug do ColdFusion antes do DOCTYPE
            var probe = raw.Length > 512 ? raw[..512] : raw;
            var idx = probe.IndexOf('<');
            if (idx < 0) return false;
            var fromTag = probe.AsSpan(idx);
            return fromTag.StartsWith("<!DOCTYPE html", StringComparison.OrdinalIgnoreCase) ||
                   fromTag.StartsWith("<html", StringComparison.OrdinalIgnoreCase);
        }

        private void AddEncryptedField(MultipartFormDataContent form, string name, string value)
        {
            var encryptedValue = EncryptDecrypt.Encrypt(value);
            var content = new StringContent(encryptedValue);

            // Remove o Content-Type automático (text/plain) para o ColdFusion não achar que é arquivo
            content.Headers.ContentType = null;

            // Força as aspas no nome do campo: name="usuario"
            content.Headers.Remove("Content-Disposition");
            content.Headers.TryAddWithoutValidation("Content-Disposition", $"form-data; name=\"{name}\"");

            form.Add(content);
        }

        private async Task AddImageToFormAsync(MultipartFormDataContent form, string path, string formName, string requestId, string methodName)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

            try
            {
                var bytes = await File.ReadAllBytesAsync(path).ConfigureAwait(false);
                if (bytes.Length == 0) return;

                // (Seu código de redimensionamento permanece aqui...)
                var resizeResult = await _resizeImageService.ExecuteAsync(new()
                {
                    Height = 1440,
                    Width = 1920,
                    Quality = 0.90f,
                    OriginalImage = bytes
                }).ConfigureAwait(false);

                if (resizeResult.TaskResult == TaskResult.Success && resizeResult.ResizedImage?.Length > 0)
                {
                    bytes = resizeResult.ResizedImage;
                }
                // (Fim do redimensionamento)

                var content = new ByteArrayContent(bytes);

                var extension = Path.GetExtension(path).ToLower();
                var mimeType = extension switch
                {
                    ".png" => "image/png",
                    ".gif" => "image/gif",
                    _ => "image/jpeg"
                };
                content.Headers.ContentType = new MediaTypeHeaderValue(mimeType);

                var fileName = Path.GetFileName(path)
                    .Replace(" ", "_")
                    .Replace("(", "")
                    .Replace(")", "");

                // --- AQUI ESTÁ A MÁGICA ---
                // 1. Remove o Content-Disposition padrão que o .NET tentaria criar
                content.Headers.Remove("Content-Disposition");

                // 2. Adiciona o cabeçalho MANUALMENTE, com aspas e sem frescuras
                // Isso garante: name="fileImagem_1"; filename="foto.jpg"
                content.Headers.TryAddWithoutValidation("Content-Disposition", $"form-data; name=\"{formName}\"; filename=\"{fileName}\"");

                // 3. Adiciona ao form APENAS O CONTEÚDO (sem passar nome/arquivo nos argumentos, pois já pusemos no header)
                form.Add(content);

                Debug.WriteLine($"[REQ:{requestId}] Imagem {formName} adicionada (Header manual forçado).");
            }
            catch (Exception ex)
            {
                SentryHelper.CaptureExceptionWithUser(ex, $"method={methodName}", $"image={formName}");
            }
        }

        private async Task HandleJsonSerializationError(string content)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
                await PopUpOK.ShowAsync(Traducao.SincronizarDados, Traducao.ErroSincronizarCompartilhe));

            var path = Path.Combine(FileSystem.CacheDirectory, "json_error.txt");
            await File.WriteAllTextAsync(path, content);
            await Share.RequestAsync(new ShareFileRequest("Erro JSON", new ShareFile(path)));
        }

        private async Task ShowHtmlErrorModalAsync(string htmlContent, string methodName)
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    try
                    {
                        // Tenta abrir popup customizado usando a instância criada
                        var popup = new HtmlErrorPopup(htmlContent, methodName);
                        await NavigationUtils.ShowPageAsModalAsync(popup).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ShowHtmlErrorModalAsync] Erro ao exibir HtmlErrorPopup: {ex.Message}");
                        // Fallback se a view não existir ou falhar na exibição
                        await SaveAndShareHtmlAsync(htmlContent, methodName).ConfigureAwait(false);
                    }
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                SentryHelper.CaptureExceptionWithUser(ex, methodName);
            }
        }

        private async Task SaveAndShareHtmlAsync(string htmlContent, string methodName)
        {
            var path = Path.Combine(FileSystem.CacheDirectory, $"error_{methodName}_{DateTime.Now:HHmmss}.html");
            await File.WriteAllTextAsync(path, htmlContent);
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await PopUpOK.ShowAsync("Erro Servidor", "O servidor retornou uma página HTML. Compartilhe o arquivo.");
                await Share.RequestAsync(new ShareFileRequest("HTML Error", new ShareFile(path)));
            });
        }

        private async Task LimpaDadosLocaisAsync()
        {
            try
            {
                // 1. Preferences
                var keys = new[] { "user", "lastsyncdatetime", "PrecisaSincronizacaoCompleta", "Permissoes", "DadosDashboard", "FormularioEmAndamento" };
                foreach (var k in keys) Preferences.Remove(k);

                // 2. Database
                try
                {
                    await Database.CloseDatabaseAsync().ConfigureAwait(false);
                    if (File.Exists(Database.PathDB)) File.Delete(Database.PathDB);
                }
                catch (Exception ex) { Debug.WriteLine($"DB Delete Error: {ex.Message}"); }

                // 3. Imagens
                try
                {
                    var docs = FileSystem.AppDataDirectory;
                    foreach (var f in Directory.EnumerateFiles(docs, "*.jpg")) File.Delete(f);
                }
                catch { }

                // 4. Caches
                try
                {
                    ServiceHelper.GetRequiredService<CacheService>().ClearAllData();
                    Graficos.ZeraDadosGraficos();
                }
                catch { }

                // 5. Recria DB vazio
                try
                {
                    await Database.ReopenDatabaseAsync().ConfigureAwait(false);
                    await ManutencaoTabelas.CriaOuAtualizaTabelas().ConfigureAwait(false);
                }
                catch { }
            }
            catch (Exception ex)
            {
                SentryHelper.CaptureExceptionWithUser(ex, "LimpaDadosLocais");
            }
        }

        #endregion
    }
}