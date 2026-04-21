using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using System;
using System.Threading.Tasks;

namespace SilvaData.Pages.PopUps
{
    public partial class PopUpYesNo : Popup<bool>
    {
        // 1. Construtor agora aceita 4 argumentos
        public PopUpYesNo(string titulo, string mensagem, string textoSim, string textoNao)
        {
            InitializeComponent();
            // Passa todos os argumentos para o ViewModel
            BindingContext = new PopUpYesNoViewModel(this, titulo, mensagem, textoSim, textoNao);
        }

        /// <summary>
        /// Exibe um popup de confirma��o com bot�es Sim e N�o customizados.
        /// </summary>
        // 2. ShowAsync agora aceita 4 argumentos
        public static async Task<bool> ShowAsync(string titulo, string mensagem)
        {
            var popup = new PopUpYesNo(titulo, mensagem, Traducao.Sim, Traducao.Nao);
            var result = await NavigationUtils.ShowPopupAsync<bool>(popup);
            return result;
        }


        /// <summary>
        /// Exibe um popup de confirma��o com bot�es Sim e N�o customizados.
        /// </summary>
        // 2. ShowAsync agora aceita 4 argumentos
        public static async Task<bool> ShowAsync(string titulo, string mensagem, string textoSim, string textoNao)
        {
            var popup = new PopUpYesNo(titulo, mensagem, textoSim, textoNao);

            var result = await NavigationUtils.ShowPopupAsync<bool>(popup);

            return result;
        }

        [Obsolete("O m�todo PopUpYesNo.Show � obsoleto. Use PopUpYesNo.ShowAsync.", false)]
        // 3. M�todo obsoleto Show agora aceita 4 argumentos
        public static Task<bool> Show(string titulo, string mensagem, string textoSim, string textoNao)
        {
            return ShowAsync(titulo, mensagem, textoSim, textoNao);
        }
    }

    // ViewModel Migrado para MVVM Toolkit
    public partial class PopUpYesNoViewModel : ObservableObject
    {
        private readonly PopUpYesNo _popup;

        public string Titulo { get; }
        public string Mensagem { get; }

        // Mantenha estas propriedades. Elas ser�o usadas no XAML para os textos dos bot�es.
        public string TextoSim { get; }
        public string TextoNao { get; }

        // Construtor agora aceita 4 argumentos
        public PopUpYesNoViewModel(PopUpYesNo popup, string titulo, string mensagem, string textoSim, string textoNao)
        {
            _popup = popup;
            Titulo = titulo;
            Mensagem = mensagem;

            // Inicializa as propriedades de texto dos bot�es
            TextoSim = textoSim;
            TextoNao = textoNao;
        }

        [RelayCommand]
        private Task SimAsync()
        {
            return _popup.CloseAsync(true);
        }

        [RelayCommand]
        private Task NaoAsync()
        {
            return _popup.CloseAsync(false);
        }
    }
}
