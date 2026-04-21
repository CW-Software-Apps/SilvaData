using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SilvaData.Pages.PopUps
{
    public partial class PopUpOK : Popup<bool>
    {
        public PopUpOK(string titulo, string mensagem)
        {
            InitializeComponent();
            BindingContext = new PopUpOKViewModel(this, titulo, mensagem);
        }

        public static async Task ShowAsync(string titulo, string mensagem)
        {
            var popup = new PopUpOK(titulo, mensagem);
            await NavigationUtils.ShowPopupAsync(popup);
        }

        [Obsolete("O m�todo PopUpOK.Show � obsoleto. Use PopUpOK.ShowAsync.", false)]
        public static Task Show(string titulo, string mensagem) => ShowAsync(titulo, mensagem);
    }

    public partial class PopUpOKViewModel : ObservableObject
    {
        private readonly PopUpOK _popup;

        public string Titulo { get; }
        public string Mensagem { get; }

        public PopUpOKViewModel(PopUpOK popup, string titulo, string mensagem)
        {
            _popup = popup;
            Titulo = titulo;
            Mensagem = mensagem;
        }

        [RelayCommand]
        private Task OK()
        {
            return _popup.CloseAsync(true);
        }
    }
}
