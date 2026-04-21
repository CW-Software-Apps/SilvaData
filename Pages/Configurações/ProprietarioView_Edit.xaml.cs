using CommunityToolkit.Mvvm.Messaging;

using SilvaData.Infrastructure;
using SilvaData.Models;
using SilvaData.Utilities;
using SilvaData.ViewModels;

namespace SilvaData.Controls
{
    /// <summary>
    /// View para editar ou criar um Propriet�rio.
    /// MIGRADO: Usa Messaging para comunica��o com o ViewModel (sem acoplamento direto).
    /// </summary>
    public partial class ProprietarioView_Edit : ContentPageEdit
    {
        private new readonly ProprietarioEditViewModel ViewModel;

        /// <summary>
        /// MIGRADO: Construtor n�o passa mais IValidatablePage para o ViewModel
        /// </summary>
        public ProprietarioView_Edit(Proprietario? proprietario = null)
        {
            InitializeComponent();

            ViewModel = ServiceHelper.GetRequiredService<ProprietarioEditViewModel>();

            // MUDAN�A: N�o passa mais 'this' (IValidatablePage) para o SetInitialState
            ViewModel.SetInitialState(proprietario);

            BindingContext = ViewModel;

            RequiredInputFields.Add(this.FindByName<ISITextField>("nome"));
        }


        protected override void OnDisappearing()
        {
            base.OnDisappearing();
        }
    }
}
