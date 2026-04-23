using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using SilvaData.Models;
using SilvaData.Pages.PopUps;
using SilvaData.Utilities;

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace SilvaData.ViewModels
{
    /// <summary>
    /// ViewModel para a página "Minha Conta".
    /// MIGRADO: Usa CacheService ao invés de DadosStatic.
    /// </summary>
    public partial class MinhaContaViewModel : ViewModelBase
    {
        private readonly ISIWebService _webService;
        private readonly CacheService _cacheService;

        [ObservableProperty]
        private dataLoginResult? _loggedUser;

        [ObservableProperty]
        private string _dispositivoId = string.Empty;

        [ObservableProperty]
        private string _sessionId = "---";

        [ObservableProperty]
        private bool _isDebug = false;

        [ObservableProperty]
        private string _versaoApp = $"Silvadata  •  Versão {AppInfo.VersionString}";

        // Paths para download temporário
        private static string PathZipTemp => Path.Combine(FileSystem.CacheDirectory, "TempDownload");
        private static string PathZipTempFileName => Path.Combine(PathZipTemp, "download.zip");

        /// <summary>
        /// MIGRADO: Construtor agora recebe CacheService via DI
        /// </summary>
        public MinhaContaViewModel(ISIWebService webService, CacheService cacheService)
        {
            _webService = webService;
            _cacheService = cacheService;

            // Escuta mudanças no LoggedUser do WebService para atualizar a UI automaticamente
            _webService.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ISIWebService.LoggedUser))
                {
                    CarregarDadosUsuario();
                }
            };

#if DEBUG
            IsDebug = true;
#endif

            // Ouve se o usuário logou para recarregar os dados
            WeakReferenceMessenger.Default.Register<LoginSuccessMessage>(this, (r, m) =>
            {
                CarregarDadosUsuario();
            });

            // Carrega o usuário inicial
            CarregarDadosUsuario();
        }

        /// <summary>
        /// Ouve mudanças na propriedade IsBusy da classe base.
        /// </summary>
        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.PropertyName == nameof(IsBusy))
            {
                LogOffCommand.NotifyCanExecuteChanged();
                DownloadDataWebCommand.NotifyCanExecuteChanged();
                DownloadDataWebCommand.NotifyCanExecuteChanged();
                GoToLoginWithoutLogOffCommand.NotifyCanExecuteChanged();
                CompartilhaDBCommand.NotifyCanExecuteChanged();
                EnviarResetCommand.NotifyCanExecuteChanged();
            }
        }

        /// <summary>
        /// Comando para carregar ou atualizar os dados do usuário logado.
        /// </summary>
        [RelayCommand]
        public void CarregarDadosUsuario()
        {
            LoggedUser = _webService.LoggedUser;

            // O ID do dispositivo deve aparecer mesmo se não estiver logado (recuperado de Preferences)
            DispositivoId = Preferences.Get("my_id", "Não identificado");

            // A sessão depende do usuário logado
            SessionId = LoggedUser?.session ?? "---";
        }

        /// <summary>
        /// Verifica se os comandos podem ser executados.
        /// </summary>
        private bool CanExecuteCommands() => !IsBusy;

        /// <summary>
        /// Comando para iniciar o processo de LogOff.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
        public async Task LogOff()
        {
            if (!await PopUpYesNo.ShowAsync(
                Traducao.Deslogar,
                Traducao.ConfirmacaodeLogOff,
                Traducao.Sim,
                Traducao.Não))
            {
                return; // Usuário cancelou
            }

            IsBusy = true;
            await _webService.LogOut();

            // MIGRADO: Limpa o cache ao invés de DadosStatic
            _cacheService.ClearAllData();

            // Limpa o cache de gráficos
            Graficos.ZeraDadosGraficos();

            IsBusy = false;

            // Envia a mensagem para o AppShell/MainPage fechar tudo
            WeakReferenceMessenger.Default.Send(new LogoutSuccessMessage());
        }

        [RelayCommand]
        private async Task AbrirWhatsApp()
        {
            await Launcher.OpenAsync("https://wa.me/554391626247");
        }

        [RelayCommand]
        private async Task AbrirPoliticaPrivacidade()
        {
            await Launcher.OpenAsync("https://www.silvateam.com.br/node/211");
        }

        #region Comandos de Debug

        /// <summary>
        /// (DEBUG) Abre a tela de Login sem fazer LogOff.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
        private async Task GoToLoginWithoutLogOff()
        {
            await NavigationUtils.ShowViewAsModalAsync<Login>();
        }

        /// <summary>
        /// (DEBUG) Compartilha o banco de dados local via Share API.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
        private async Task CompartilhaDB()
        {
            await Share.RequestAsync(new ShareFileRequest
            {
                Title = "Compartilhar Banco de Dados",
                File = new ShareFile(Database.PathDB)
            });
        }

        /// <summary>
        /// (DEBUG) Envia um comando de reset para o servidor.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
        private async Task EnviarReset()
        {
            var result = await ISIWebService.Instance.SendData("", "reset");
            await PopUpOK.ShowAsync("Debug", "Método Reset Enviado com Sucesso");
        }

        /// <summary>
        /// (DEBUG) Baixa dados de um link externo (OneDrive, Google Drive, Dropbox, etc).
        /// Solicita o link ao usuário, baixa o ZIP, extrai e atualiza credenciais.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
        private async Task DownloadDataWeb()
        {
            try
            {
                // Solicita o link ao usuário
                var link = await Application.Current!.Windows[0].Page!.DisplayPromptAsync(
                    "Download de Dados",
                    "Cole o link do OneDrive ou URL direta do ZIP:",
                    "Baixar",
                    "Cancelar",
                    "https://...",
                    maxLength: 1000,
                    keyboard: Keyboard.Url);

                if (string.IsNullOrWhiteSpace(link))
                {
                    Debug.WriteLine("[DownloadDataFromWeb] Usuário cancelou ou link vazio");
                    return;
                }

                // Converte link do OneDrive/Google Drive/Dropbox para download direto
                link = ConvertCloudLinkToDirectDownload(link.Trim());
                Debug.WriteLine($"[DownloadDataFromWeb] Link convertido: {link}");

                // Valida se é uma URL válida
                if (!Uri.TryCreate(link, UriKind.Absolute, out var uri) ||
                    (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                {
                    await PopUpOK.ShowAsync(Traducao.Erro, "Link inválido. Certifique-se de que é uma URL válida (http:// ou https://).");
                    return;
                }

                IsBusy = true;

                // Fecha o banco antes de manipular arquivos
                await Database.CloseDatabaseAsync();

                try
                {
                    // Cria diretório temporário se não existir
                    if (!Directory.Exists(PathZipTemp))
                        Directory.CreateDirectory(PathZipTemp);

                    // Baixa o arquivo
                    Debug.WriteLine($"[DownloadDataFromWeb] Baixando: {link}");
                    var bytes = await DownloadFileAsync(link);

                    if (bytes == null || bytes.Length == 0)
                    {
                        await PopUpOK.ShowAsync(Traducao.Erro, "Falha ao baixar o arquivo. Verifique se o link está correto e acessível.");
                        return;
                    }

                    Debug.WriteLine($"[DownloadDataFromWeb] Arquivo baixado: {bytes.Length} bytes");

                    // Remove arquivo ZIP antigo se existir
                    if (File.Exists(PathZipTempFileName))
                        File.Delete(PathZipTempFileName);

                    // Salva o ZIP
                    await File.WriteAllBytesAsync(PathZipTempFileName, bytes);
                    Debug.WriteLine($"[DownloadDataFromWeb] ZIP salvo em: {PathZipTempFileName}");

                    // Tenta extrair o ZIP
                    try
                    {
                        ZipFile.ExtractToDirectory(PathZipTempFileName, PathZipTemp, overwriteFiles: true);
                        Debug.WriteLine("[DownloadDataFromWeb] ZIP extraído com sucesso");
                    }
                    catch (InvalidDataException zipEx)
                    {
                        Debug.WriteLine($"[DownloadDataFromWeb] ❌ Erro ZIP inválido: {zipEx.Message}");

                        // Oferece compartilhar o arquivo para debug
                        var compartilhar = await PopUpYesNo.ShowAsync(
                            Traducao.Erro,
                            $"O arquivo baixado não é um ZIP válido.\n\nTamanho: {bytes.Length:N0} bytes\n\nDeseja compartilhar o arquivo para verificar?",
                            "Compartilhar",
                            "Fechar");

                        if (compartilhar)
                        {
                            await Share.RequestAsync(new ShareFileRequest
                            {
                                Title = "Arquivo baixado (debug)",
                                File = new ShareFile(PathZipTempFileName)
                            });
                        }
                        return;
                    }

                    // Lê e aplica logininfo.txt
                    var loginInfoPath = Path.Combine(PathZipTemp, "logininfo.txt");
                    if (File.Exists(loginInfoPath))
                    {
                        var loginInfo = await File.ReadAllTextAsync(loginInfoPath);
                        Preferences.Set("user", loginInfo);
                        Debug.WriteLine("[DownloadDataFromWeb] logininfo.txt aplicado");
                    }
                    else
                    {
                        Debug.WriteLine("[DownloadDataFromWeb] ⚠️ logininfo.txt não encontrado no ZIP");
                    }

                    // Lê e aplica deviceid.txt
                    var deviceIdPath = Path.Combine(PathZipTemp, "deviceid.txt");
                    if (File.Exists(deviceIdPath))
                    {
                        var deviceId = await File.ReadAllTextAsync(deviceIdPath);
                        Preferences.Set("my_id", deviceId);
                        Debug.WriteLine("[DownloadDataFromWeb] deviceid.txt aplicado");
                    }
                    else
                    {
                        Debug.WriteLine("[DownloadDataFromWeb] ⚠️ deviceid.txt não encontrado no ZIP");
                    }

                    // (Opcional) Copia o banco de dados se existir
                    var dbPath = Path.Combine(PathZipTemp, "ISIDatabase.db3");
                    if (File.Exists(dbPath))
                    {
                        File.Copy(dbPath, Database.PathDB, overwrite: true);
                        Debug.WriteLine("[DownloadDataFromWeb] ISIDatabase.db3 copiado");
                    }

                    await PopUpOK.ShowAsync(Traducao.Sucesso, Traducao.DadosRecebidosComSucesso);
                }
                catch (HttpRequestException httpEx)
                {
                    Debug.WriteLine($"[DownloadDataFromWeb] ❌ Erro HTTP: {httpEx.Message}");
                    await PopUpOK.ShowAsync(Traducao.Erro, $"Erro de rede ao baixar:\n{httpEx.Message}");
                }
                catch (IOException ioEx)
                {
                    Debug.WriteLine($"[DownloadDataFromWeb] ❌ Erro de I/O: {ioEx.Message}");
                    await PopUpOK.ShowAsync(Traducao.Erro, $"Erro ao manipular arquivos:\n{ioEx.Message}");
                }
                finally
                {
                    // Reabre o banco
                    await Database.ReopenDatabaseAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DownloadDataFromWeb] ❌ Erro inesperado: {ex}");
                await PopUpOK.ShowAsync(Traducao.Erro, $"Erro inesperado:\n{ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Converte links de compartilhamento de serviços de nuvem para download direto.
        /// Suporta: OneDrive (1drv.ms, onedrive.live.com), SharePoint, Google Drive, Dropbox.
        /// </summary>
        private static string ConvertCloudLinkToDirectDownload(string url)
        {
            // Link curto do OneDrive (1drv.ms) - converte para download direto
            // https://1drv.ms/u/s!ABC123 → https://api.onedrive.com/v1.0/shares/u!{base64}/root/content
            if (url.Contains("1drv.ms"))
            {
                var base64Value = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(url))
                    .TrimEnd('=')
                    .Replace('/', '_')
                    .Replace('+', '-');

                return $"https://api.onedrive.com/v1.0/shares/u!{base64Value}/root/content";
            }

            // OneDrive web link - substitui redir para download
            if (url.Contains("onedrive.live.com"))
            {
                if (url.Contains("redir?"))
                {
                    url = url.Replace("redir?", "download?");
                }
                else if (!url.Contains("download=1"))
                {
                    url += url.Contains("?") ? "&download=1" : "?download=1";
                }
                return url;
            }

            // SharePoint / OneDrive Business
            if (url.Contains("sharepoint.com") || url.Contains("my.sharepoint.com"))
            {
                if (!url.Contains("download=1"))
                {
                    url += url.Contains("?") ? "&download=1" : "?download=1";
                }
                return url;
            }

            // Google Drive
            // https://drive.google.com/file/d/FILE_ID/view → download direto
            if (url.Contains("drive.google.com/file/d/"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(url, @"drive\.google\.com/file/d/([^/]+)");
                if (match.Success)
                {
                    var fileId = match.Groups[1].Value;
                    return $"https://drive.google.com/uc?export=download&id={fileId}";
                }
            }

            // Dropbox - substitui dl=0 por dl=1
            if (url.Contains("dropbox.com"))
            {
                return url.Replace("dl=0", "dl=1").Replace("www.dropbox.com", "dl.dropboxusercontent.com");
            }

            // Retorna sem modificação se não for reconhecido
            return url;
        }

        /// <summary>
        /// Baixa um arquivo de uma URL e retorna os bytes.
        /// Segue redirects automaticamente.
        /// </summary>
        private static async Task<byte[]?> DownloadFileAsync(string url)
        {
            try
            {
                var handler = new HttpClientHandler
                {
                    AllowAutoRedirect = true,
                    MaxAutomaticRedirections = 10
                };

                using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(5) };

                // User-Agent ajuda a evitar bloqueios
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; SilvaData-MAUI)");

                var response = await httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"[DownloadFileAsync] ❌ Status: {response.StatusCode} - {response.ReasonPhrase}");
                    return null;
                }

                // Log do content-type para debug
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "unknown";
                Debug.WriteLine($"[DownloadFileAsync] Content-Type: {contentType}");

                return await response.Content.ReadAsByteArrayAsync();
            }
            catch (TaskCanceledException)
            {
                Debug.WriteLine("[DownloadFileAsync] ❌ Timeout - download demorou demais");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DownloadFileAsync] ❌ Exceção: {ex.Message}");
                throw;
            }
        }

        #endregion
    }
}
