using DateChangedEventArgs = Microsoft.Maui.Controls.DateChangedEventArgs;

using SilvaData.ViewModels;

namespace SilvaData.Controls
{
    public partial class LoteFormHeader : ContentView
    {
        // Construtor sem par�metros necess�rio para instancia��o via XAML
        public LoteFormHeader()
        {
            InitializeComponent();
        }

        // Construtor opcional com ViewModel para cen�rios de DI
        public LoteFormHeader(LoteFormularioViewModel loteFormViewModel)
        {
            InitializeComponent();
            BindingContext = loteFormViewModel;
        }

        private void Date_Picker_OnDateChanged(object sender, DateChangedEventArgs e)
        {
            if (BindingContext is LoteFormularioViewModel vm)
            {
                vm.UpdateIdadeLote();
            }
        }
    }
}
