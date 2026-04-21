using CommunityToolkit.Mvvm.ComponentModel; 
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using ISIInstitute.Views;

using SilvaData.Pages.PopUps;
using SilvaData.Utilities; 

namespace SilvaData.ViewModels
{
    public partial class ConfigViewModel : ObservableObject
    {
        public ConfigViewModel()
        {
        }

        [RelayCommand]
        public void ShowDashboard()
        {
            WeakReferenceMessenger.Default.Send(new ShowDashboardMessage());
        }

        [RelayCommand]
        public void ShowLotes()
        {
            WeakReferenceMessenger.Default.Send(new ShowLotesMessage());
        }

        [RelayCommand]
        public void ShowSync()
        {
            WeakReferenceMessenger.Default.Send(new ShowSyncMessage());
        }

        [RelayCommand]
        public void ShowSuporte()
        {
            WeakReferenceMessenger.Default.Send(new ShowSuporteMessage());
        }

        // Usa NavigationUtils para exibir as views registradas em DI
        [RelayCommand]
        public async Task AtividadesAsync()
        {
            await NavigationUtils.ShowViewAsModalAsync<AtividadeView>();
        }

        [RelayCommand]
        public async Task NotificacoesAsync()
        {
            await NavigationUtils.ShowViewAsModalAsync<NotificacoesView>();
        }

        [RelayCommand]
        public async Task MinhaContaAsync()
        {
            await NavigationUtils.ShowViewAsModalAsync<MinhaConta>();
        }

        [RelayCommand]
        public async Task PrivacyAsync()
        {
            await MostraPrivacidade();
        }

        public async Task MostraPrivacidade()
        {
            string url = LocalizationManager.LocManager.CurrentLanguage switch
            {
                "pt-BR" => "https://isiinstitute.com/pt/politica-de-privacidade/",
                "es-ES" => "https://isiinstitute.com/es/politica-de-privacidade/",
                _ => "https://isiinstitute.com/privacy-policy/"
            };

            await Browser.Default.OpenAsync(new Uri(url), BrowserLaunchMode.SystemPreferred);
        }

        [RelayCommand]
        public async Task ProprietariosAsync()
        {
            await NavigationUtils.ShowViewAsModalAsync<ProprietarioView>();
        }

        [RelayCommand]
        public async Task PropriedadesAsync()
        {
            await NavigationUtils.ShowViewAsModalAsync<PropriedadeView>();
        }

        [RelayCommand]
        public async Task RegionaisAsync()
        {
            await NavigationUtils.ShowViewAsModalAsync<RegionalView>();
        }

        [RelayCommand]
        public async Task UnidadesEpidemiologicasAsync()
        {
            await NavigationUtils.ShowViewAsModalAsync<UnidadeEpidemiologicaView>();
        }

        [RelayCommand]
        public async Task LogOffAsync()
        {
            await PerguntaLogOff();
        }

        public async Task PerguntaLogOff()
        {
            if (await PopUpYesNo.ShowAsync(Traducao.Aten��o, Traducao.ConfirmacaodeLogOff, Traducao.Sim, Traducao.Cancelar))
            {
                if (await ISIWebService.Instance.LogOut())
                {
                    await ISIWebService.Instance.LogOut();
                    await NavigationUtils.ShowViewAsModalAsync<Login>();
                }
            }
        }
    }
}
