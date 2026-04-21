using SilvaData.ViewModels;

namespace SilvaData.Controls
{
    public partial class AtividadeView : ContentPageWithLocalization
    {
        // MUDAN�A: ViewModel is injected via DI
        private readonly AtividadeViewModel ViewModel;

        public AtividadeView(AtividadeViewModel viewModel)
        {
            InitializeComponent();

            // MUDAN�A: Use injected ViewModel
            ViewModel = viewModel;
            BindingContext = ViewModel;

            // MUDAN�A: Set Locale for MAUI SfScheduler
            // Calendario.Locale = new System.Globalization.CultureInfo(LocalizationManager.LocManager.IdiomaParaCalendario);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _ = OnAppearingInternalAsync();
        }

        private async Task OnAppearingInternalAsync()
        {
            try
            {
                var loadingView = this.FindByName<LoadingView>("busyindicator");
                if (loadingView != null)
                {
                    loadingView.Text = Traducao.CarregandoAtividades;
                }

                await ViewModel.CarregaDadosAtividades();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AtividadeView] Erro em OnAppearing: {ex.Message}");
            }
        }
    }
}
