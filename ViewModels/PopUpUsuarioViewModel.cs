using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using ISIInstitute.Views;

using SilvaData.Controls;
using SilvaData.Models;
using SilvaData.Pages.PopUps;
using SilvaData.Utilities;
using SilvaData.Utils;

using System;
using System.Threading.Tasks;

namespace SilvaData.ViewModels
{
    /// <summary>
    /// ViewModel para o PopUp do Menu do Usu�rio (Minha Conta, Privacidade, Sair).
    /// </summary>
    public partial class PopUpUsuarioViewModel : ObservableObject
    {
        private readonly ConfigViewModel _configViewModel;
        private readonly ISIWebService _webService;
        private Action? _closePopupAction;

        [ObservableProperty]
        private string loggedUserName;

        [ObservableProperty]
        private string loggedUserEmail;

#if DEBUG
        [ObservableProperty]
        private bool isDebug = true;
#else
        [ObservableProperty]
        private bool isDebug = false;
#endif

        public PopUpUsuarioViewModel(ConfigViewModel configViewModel, ISIWebService webService)
        {
            _configViewModel = configViewModel;
            _webService = webService;

            LoggedUserName = _webService.LoggedUser?.nome ?? string.Empty;
            LoggedUserEmail = _webService.LoggedUser?.email ?? string.Empty;
        }

        public void SetCloseAction(Action closeAction)
        {
            _closePopupAction = closeAction;
        }

        [RelayCommand]
        private async Task MinhaContaAsync()
        {
            _closePopupAction?.Invoke();
            await NavigationUtils.ShowViewAsModalAsync<MinhaConta>();
        }

#if DEBUG
        [RelayCommand]
        private async Task PermissoesAsync()
        {
            _closePopupAction?.Invoke();
            var popup = new PermissoesPopup();
            await NavigationUtils.ShowPopupAsync(popup);
        }
#endif

        [RelayCommand]
        private async Task PrivacidadeAsync()
        {
            _closePopupAction?.Invoke(); // Fecha o popup
            await _configViewModel.MostraPrivacidade();
        }

        /// <summary>
        /// Fecha o popup e pergunta se o usu�rio quer deslogar.
        /// </summary>
        [RelayCommand]
        private async Task LogOffAsync()
        {
            _closePopupAction?.Invoke(); // Fecha o popup
            await _configViewModel.PerguntaLogOff();
        }
    }
}
