using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SilvaData.Controls; // Para a classe Login
using SilvaData.Models;
using SilvaData.Pages.PopUps;
using SilvaData.Utilities;
using System;
using System.Threading.Tasks;
using LocalizationResourceManager.Maui; // Para o LocalizationManager

namespace SilvaData.ViewModels
{
    /// <summary>
    /// ViewModel para a página de Login.
    /// </summary>
    public partial class LoginViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string _busyText = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(NotBusy))] // Notifica a propriedade inversa
        private bool _isBusy;

        public bool NotBusy => !IsBusy;


        [ObservableProperty]
        private string _versionText = string.Empty;

        // Propriedades para a opacidade das bandeiras de idioma
        [ObservableProperty]
        private double _englishOpacity = 0.1;
        [ObservableProperty]
        private double _portugueseOpacity = 0.1;
        [ObservableProperty]
        private double _spanishOpacity = 0.1;

        private readonly ConfigViewModel _configViewModel;

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="LoginViewModel"/>.
        /// </summary>
        public LoginViewModel(ConfigViewModel configViewModel)
        {
            _configViewModel = configViewModel;

            // Define o texto da versão
            VersionText = $"{Traducao.Versão} {AppInfo.Current.VersionString}";

            // Define o idioma inicial
            UpdateLanguage();
        }

        partial void OnEmailChanged(string value) => LoginNowCommand.NotifyCanExecuteChanged();
        partial void OnPasswordChanged(string value) => LoginNowCommand.NotifyCanExecuteChanged();

        private bool CanExecuteLogin() =>
            !IsBusy && !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Password);

        /// <summary>
        /// Comando para executar o login.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanExecuteLogin))]
        private async Task LoginNowAsync()
        {
            IsBusy = true;
            BusyText = Traducao.ChecandoLogin;

            if (Connectivity.NetworkAccess != NetworkAccess.Internet)
            {
                await PopUpOK.ShowAsync(Traducao.SemInternet, Traducao.NaoPodeAtualizarSemInternet);
                IsBusy = false;
                return;
            }

            var sessionAtiva = await ISIWebService.Instance.checkSessionAtiva(Email, Password);

            if (sessionAtiva != null && !string.IsNullOrEmpty(sessionAtiva.dispositivoDescricao))
            {
                string mensagem = string.Format(Traducao.UsuarioJaLogado, sessionAtiva.dispositivoDescricao, sessionAtiva.dataInicio);

                if (!await PopUpYesNo.ShowAsync(Traducao.Atenção, mensagem, Traducao.Sim, Traducao.Não))
                {
                    IsBusy = false;
                    return;
                }

                await ISIWebService.Instance.LogOut();
            }

            var logged = await ISIWebService.Instance.Login(Email, Password);

            if (logged is LoginResult.FailedWebService or LoginResult.Wrong)
            {
                if (logged == LoginResult.Wrong)
                    await PopUpOK.ShowAsync(Traducao.Login, Traducao.LoginInválido);
                else
                    await PopUpOK.ShowAsync(Traducao.SemInternet, Traducao.NaoPodeAtualizarSemInternet);

                IsBusy = false;
                return;
            }

            Preferences.Set("PrecisaSincronizacaoCompleta", true);
            Graficos.ZeraDadosGraficos();

            // Define a flag que o MainPageModel verificará
            Login.AcabouDeLogar = true;
            WeakReferenceMessenger.Default.Send(new LoginSuccessMessage());

            _ = NavigationUtils.PopModalAsync();

            IsBusy = false;
        }

        /// <summary>
        /// Comando para mostrar a política de privacidade.
        /// </summary>
        [RelayCommand]
        private async Task PolicyPrivacyAsync()
        {
            await _configViewModel.MostraPrivacidade();
        }

        /// <summary>
        /// Comando para abrir a página de "Esqueci a Senha".
        /// </summary>
        [RelayCommand]
        private async Task ForgotPasswordAsync()
        {
            var uri = new Uri("https://webapp.isiinstitute.com/public/login/_thickbox_password.cfm");
            await Browser.Default.OpenAsync(uri, BrowserLaunchMode.SystemPreferred);
        }

        /// <summary>
        /// Comando para alterar o idioma da aplicação.
        /// </summary>
        [RelayCommand]
        private void SetLanguage(string culture)
        {
            LocalizationManager.LocManager.SetLanguage(culture);
            UpdateLanguage();
        }

        /// <summary>
        /// Atualiza a opacidade das bandeiras de idioma com base na seleção atual.
        /// </summary>
        private void UpdateLanguage()
        {
            switch (LocalizationManager.LocManager.CurrentLanguage)
            {
                case "pt-BR":
                    PortugueseOpacity = 1.0;
                    EnglishOpacity = 0.1;
                    SpanishOpacity = 0.1;
                    break;
                case "es-ES":
                    PortugueseOpacity = 0.1;
                    EnglishOpacity = 0.1;
                    SpanishOpacity = 1.0;
                    break;
                default: // "en-US"
                    PortugueseOpacity = 0.1;
                    EnglishOpacity = 1.0;
                    SpanishOpacity = 0.1;
                    break;
            }
        }
    }
}