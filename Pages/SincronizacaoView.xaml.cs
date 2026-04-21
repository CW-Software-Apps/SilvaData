using SilvaData.Infrastructure;
using SilvaData.ViewModels;

using Microsoft.Maui.Controls;

namespace SilvaData.Controls
{
    /// <summary>
    /// View (ContentView) para exibir o progresso do Download.
    /// </summary>
    public partial class SincronizacaoView : ContentView
    {
        /// <summary>
        /// Inicializa uma nova inst�ncia da classe <see cref="SincronizacaoView"/>.
        /// </summary>
        public SincronizacaoView()
        {
            InitializeComponent();
            // O BindingContext � definido aqui, como no seu c�digo original
            BindingContext = ServiceHelper.GetRequiredService<SincronizacaoViewModel>();
        }
    }
}
