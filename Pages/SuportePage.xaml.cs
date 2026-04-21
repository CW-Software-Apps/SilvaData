using SilvaData.Infrastructure; // Para ServiceHelper
using SilvaData.PageModels; // Para SuportePageViewModel

namespace SilvaData.Controls
{
    /// <summary>
    /// View (ContentView) para a p�gina de Suporte.
    /// </summary>
    public partial class SuportePage : ContentView
    {
        /// <summary>
        /// Inicializa uma nova inst�ncia da classe <see cref="SuportePage"/>.
        /// </summary>
        public SuportePage()
        {
            InitializeComponent();

            // Define o BindingContext para o ViewModel obtido via ServiceHelper
            BindingContext = ServiceHelper.GetRequiredService<SuportePageViewModel>();
        }
    }
}
