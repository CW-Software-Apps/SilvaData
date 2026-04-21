using CommunityToolkit.Mvvm.ComponentModel; // Adicionado
using CommunityToolkit.Mvvm.Input;
using SilvaData.Models; // Para PageModelBase
using SilvaData.Utilities;
using System.IO; // Adicionado para File
using System.Threading.Tasks; // Adicionado para Task
using System; // Adicionado para Uri

namespace SilvaData.PageModels
{
    /// <summary>
    /// ViewModel da p�gina de suporte.
    /// Cont�m comandos para abrir o site, enviar e-mail, abrir WhatsApp e enviar backup do banco de dados.
    /// </summary>
    public partial class SuportePageViewModel : PageModelBase
    {
        /// <summary>
        /// Obt�m a string da vers�o formatada do aplicativo (ex: "Vers�o 1.0.0").
        /// </summary>
        [ObservableProperty]
        private string versaoApp = string.Empty;

        /// <summary>
        /// Inicializa uma nova inst�ncia da classe <see cref="SuportePageViewModel"/>.
        /// </summary>
        public SuportePageViewModel()
        {
            // Define a vers�o do app ao inicializar o ViewModel
            var version = AppInfo.Current.VersionString;
            VersaoApp = $"{Traducao.Vers�o} {version}";
        }

        /// <summary>
        /// Comando que abre o site oficial do ISI no navegador padr�o do dispositivo.
        /// </summary>
        [RelayCommand]
        private async Task Site()
        {
            await ISIUtils.TryOpenUriAsync("http://isiinstitute.com/");
        }

        /// <summary>
        /// Comando que inicia o cliente de e-mail com o endere�o do suporte.
        /// </summary>
        [RelayCommand]
        async Task Email()
        {
            var address = "isi@isiinstitute.com";
            await Launcher.OpenAsync(new Uri($"mailto:{address}"));
        }

        /// <summary>
        /// Comando que tenta abrir uma conversa no WhatsApp com o n�mero de suporte.
        /// </summary>
        [RelayCommand]
        async Task WhatsApp()
        {
            await Launcher.OpenAsync(new Uri("whatsapp://send?phone=554391626247"));
        }

        /// <summary>
        /// Comando que cria um backup completo (zip) e inicia o di�logo de compartilhamento.
        /// </summary>
        [RelayCommand]
        private async Task EnviarBancoDadosSuporte()
        {
            await RunWithBusyAsync(async () =>
            {
                try
                {
                    var zip = await ISIUtils.CreateFullBackupAsync();

                    if (!string.IsNullOrEmpty(zip) && File.Exists(zip))
                    {
                        await Share.RequestAsync(new ShareFileRequest
                        {
                            Title = Traducao.CompartilharBancoDeDados,
                            File = new ShareFile(zip)
                        });
                    }
                    else
                    {
                        await ISIUtils.ShowErrorAsync(Traducao.Erro, Traducao.ArquivoBackupNaoEncontrado);
                    }
                }
                catch (Exception ex)
                {
                    await ISIUtils.ShowErrorAsync(Traducao.Erro, string.Format(Traducao.ErroAoEnviarBancoDados + ": {0}", ex.Message));
                }
            });
        }
    }
}
