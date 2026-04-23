namespace SilvaData.Controls
{
    public partial class GalpoesView : ContentView
    {
        public GalpoesView()
        {
            InitializeComponent();
            BindingContext = ServiceHelper.GetRequiredService<GalpoesViewModel>();
        }
    }
}
