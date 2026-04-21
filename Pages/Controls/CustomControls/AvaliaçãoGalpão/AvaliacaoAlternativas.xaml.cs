using SilvaData.ViewModels;
using SilvaData.Utilities;
using SilvaData.Models;

using System.Diagnostics;

using Syncfusion.Maui.ListView; // Necess�rio para ItemTappedEventArgs

namespace SilvaData.Controls
{
    public partial class AvaliacaoAlternativas : ContentView
    {
        private readonly AvaliacaoAlternativasViewModel _viewModel;

        /// <summary>
        /// ??? CORRIGIDO: Construtor agora busca ViewModel do ServiceProvider ???
        /// </summary>
        public AvaliacaoAlternativas()
        {
            InitializeComponent();

            // ? Busca ViewModel do DI
            _viewModel = ServiceHelper.GetRequiredService<AvaliacaoAlternativasViewModel>();

            // ??? CR�TICO: Seta o BindingContext! ???
            BindingContext = _viewModel;

            Debug.WriteLine($"[AvaliacaoAlternativas] ? Construtor chamado");
            Debug.WriteLine($"  ViewModel HashCode: {_viewModel.GetHashCode()}");
            Debug.WriteLine($"  BindingContext: {BindingContext != null}");
        }

        /// <summary>
        /// ? Exp�e ViewModel para uso externo
        /// </summary>
        public AvaliacaoAlternativasViewModel ViewModel => _viewModel;

        /// <summary>
        /// ? Captura o toque nativo do SfListView e repassa para o ViewModel
        /// </summary>
        private void SfListView_ItemTapped(object sender, Syncfusion.Maui.ListView.ItemTappedEventArgs e)
        {
            if (e.DataItem is ParametroAlternativas itemTocado)
            {
                // S� processa o clique se a tela estiver em modo de edi��o
                if (_viewModel.PodeEditar)
                {
                    // Chama a l�gica de sele��o existente no ViewModel
                    _viewModel.FrameTapped(itemTocado);
                }
            }
        }

        /// <summary>
        /// Roteia o clique do bot�o nativo diretamente para o comando do ViewModel.
        /// Fura o bloqueio de bindings complexos do SfListView.
        /// </summary>
        private void OnVerFotoClicked(object sender, EventArgs e)
        {
            // Pega o bot�o que disparou o evento
            if (sender is Button button)
            {
                // O BindingContext do bot�o � o item da linha (ParametroAlternativas)
                if (button.BindingContext is ParametroAlternativas alternativaTocada)
                {
                    Debug.WriteLine($"[AvaliacaoAlternativas] Disparando clique da foto via Code-Behind: {alternativaTocada.descricao}");

                    // Executa o comando que j� existe no ViewModel
                    if (_viewModel.VerFotoCommand.CanExecute(alternativaTocada))
                    {
                        _viewModel.VerFotoCommand.Execute(alternativaTocada);
                    }
                }
            }
        }
    }
}
