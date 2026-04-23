using SilvaData.Infrastructure;
using SilvaData.ViewModels;

namespace SilvaData.Controls
{
    public partial class MinhaConta : ContentView
    {
        private readonly MinhaContaViewModel ViewModel;

        public MinhaConta()
        {
            InitializeComponent();

            ViewModel = ServiceHelper.GetRequiredService<MinhaContaViewModel>();
            BindingContext = ViewModel;
        }
    }
}