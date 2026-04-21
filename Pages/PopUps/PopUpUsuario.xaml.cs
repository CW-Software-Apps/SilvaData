using CommunityToolkit.Maui.Views;

using SilvaData.Infrastructure; // Para ServiceHelper
using SilvaData.ViewModels;

namespace SilvaData.Pages.PopUps
{
    /// <summary>
    /// PopUp que exibe o menu do usu�rio (Minha Conta, Privacidade, Sair).
    /// </summary>
    public partial class PopUpUsuario : Popup
    {
        private readonly PopUpUsuarioViewModel ViewModel;

        public PopUpUsuario()
        {
            InitializeComponent();

            // Resolve o ViewModel e suas depend�ncias (ConfigViewModel, ISIWebService)
            ViewModel = ServiceHelper.GetRequiredService<PopUpUsuarioViewModel>();

            // Injeta a A��o de Fechar no ViewModel
            ViewModel.SetCloseAction(() => CloseAsync());

            BindingContext = ViewModel;
        }
    }
}
