using SilvaData.Infrastructure;
using SilvaData.Models;
using SilvaData.Utilities;
using SilvaData.ViewModels;
using SilvaData.Controls; // Para UnidadeEpidemiologicaComboBox e ISITextField
using System; // Adicionado

namespace SilvaData.Controls
{
    /// <summary>
    /// View para editar ou criar uma Propriedade.
    /// Delega toda a l�gica para <see cref="PropriedadeEditViewModel"/>.
    /// </summary>
    public partial class PropriedadeView_Edit : ContentPageEdit
    {
        private new readonly PropriedadeEditViewModel ViewModel;

        /// <summary>
        /// Inicializa uma nova inst�ncia da classe <see cref="PropriedadeView_Edit"/>.
        /// </summary>
        public PropriedadeView_Edit(Propriedade? propriedade = null, int? regionalID = -1)
        {
            InitializeComponent();

            ViewModel = ServiceHelper.GetRequiredService<PropriedadeEditViewModel>();

            // CORRE��O: Chamando o SetInitialState desacoplado (sem 'this')
            ViewModel.SetInitialState(propriedade, regionalID);

            BindingContext = ViewModel;

            // Popula a lista de campos obrigat�rios (l�gica de UI)
            RequiredInputFields.Add(this.FindByName<ISITextField>("nome"));
            RequiredInputFields.Add(this.FindByName<ISIComboBox>("proprietariocombobox"));
            RequiredInputFields.Add(this.FindByName<RegionalComboBox>("regionalcombobox"));
        }

        /// <summary>
        /// Garante que o ViewModel carregue os dados quando a p�gina aparecer.
        /// (Herdado de ContentPageEdit)
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (ViewModel != null)
            {
                _ = OnAppearingInternalAsync();
            }
        }

        private async Task OnAppearingInternalAsync()
        {
            try
            {
                await ViewModel.AppearingBaseTask();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PropriedadeView_Edit] Erro em OnAppearing: {ex.Message}");
            }
        }
    }
}
