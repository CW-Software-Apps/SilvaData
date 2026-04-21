using SilvaData.Infrastructure;
using SilvaData.Models;
using SilvaData.Utilities;
using SilvaData.ViewModels;

namespace SilvaData.Controls
{
    /// <summary>
    /// View para editar ou criar uma Regional.
    /// Herda de <see cref="ContentPageEdit"/> para compatibilidade de navega��o
    /// e delega toda a l�gica para <see cref="RegionalEditViewModel"/>.
    /// </summary>
    public partial class RegionalView_Edit : ContentPageEdit
    {
        private new readonly RegionalEditViewModel ViewModel;

        /// <summary>
        /// Inicializa uma nova inst�ncia da classe <see cref="RegionalView_Edit"/>.
        /// </summary>
        /// <param name="regional">A regional a ser editada, ou null para criar uma nova.</param>
        public RegionalView_Edit(Regional? regional = null)
        {
            InitializeComponent();

            // Obt�m o ViewModel (Regra 2)
            ViewModel = ServiceHelper.GetRequiredService<RegionalEditViewModel>();

            // Passa o par�metro para o ViewModel
            ViewModel.SetInitialState(regional);

            // Define o BindingContext da p�gina
            BindingContext = ViewModel;

            var campoNome = this.FindByName<ISITextField>("nome");
            if (campoNome != null) RequiredInputFields.Add(campoNome);
        }
    }
}
