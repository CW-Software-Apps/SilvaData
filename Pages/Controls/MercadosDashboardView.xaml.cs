namespace SilvaData.Controls
{
    public partial class MercadosDashboardView : ContentView
    {
        public MercadosDashboardView()
        {
            InitializeComponent();
            BindingContext = ServiceHelper.GetRequiredService<MercadosDashboardViewModel>();
        }
    }
}
