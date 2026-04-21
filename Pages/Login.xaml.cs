using SilvaData.Infrastructure;
using SilvaData.ViewModels;

namespace SilvaData.Controls
{
    /// <summary>
    /// View (P�gina) para Login. Esta p�gina � modal e n�o pode ser fechada
    /// pelo usu�rio (ex: bot�o "Voltar" do Android).
    /// </summary>
    public partial class Login : ContentPage
    {
        /// <summary>
        /// Flag est�tica usada pelo MainPageModel para saber
        /// que o app deve rodar a sincroniza��o inicial.
        /// </summary>
        public static bool AcabouDeLogar;

        /// <summary>
        /// Inicializa uma nova inst�ncia da classe <see cref="Login"/>.
        /// </summary>
        public Login(LoginViewModel viewModel)
        {
            InitializeComponent();

            Shell.SetNavBarIsVisible(this, false);

            // Define o BindingContext para o ViewModel injetado
            BindingContext = viewModel;
        }

        /// <summary>
        /// Chamado quando a p�gina � exibida.
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Define a flag est�tica que o MainPageModel usar�
            AcabouDeLogar = true;

            // Anima��o de Fade-in (l�gica de View)
            // Assumindo que o Grid no XAML tem x:Name="loginPanel"
            var loginPanel = this.FindByName<Grid>("loginPanel");
            if (loginPanel != null)
            {
                _ = loginPanel.FadeToAsync(1, 500);
            }
        }

        /// <summary>
        /// CORRE��O: Impede que o bot�o "Voltar" do hardware (Android)
        /// feche a p�gina de login.
        /// </summary>
        /// <returns>Sempre <c>true</c> para indicar que o evento foi tratado.</returns>
        protected override bool OnBackButtonPressed()
        {
            // Retorna 'true' para "consumir" o evento e impedir
            // que a p�gina seja fechada.
            return true;
        }
    }
}
