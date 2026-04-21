using SilvaData.Infrastructure;
using SilvaData.Models;
using SilvaData.ViewModels;
using SilvaData.Utils; // Adicionado

namespace ISIInstitute.Views // (Mantendo seu namespace original)
{
    /// <summary>
    /// View (P�gina) para a tela "Minha Conta".
    /// </summary>
    public partial class MinhaConta : ContentPageWithLocalization
    {
        private readonly MinhaContaViewModel ViewModel;

        /// <summary>
        /// Inicializa uma nova inst�ncia da classe <see cref="MinhaConta"/>.
        /// </summary>
        public MinhaConta()
        {
            InitializeComponent();

            ViewModel = ServiceHelper.GetRequiredService<MinhaContaViewModel>();
            BindingContext = ViewModel;
        }
    }
}
