using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Maui.Views;

namespace SilvaData.Pages.PopUps
{
    /// <summary>
    /// Popup que exibe uma imagem em tela cheia com t�tulo.
    /// </summary>
    public partial class PhotoPopup : Popup
    {
        /// <summary>
        /// Inicializa uma nova inst�ncia do popup de foto.
        /// </summary>
        /// <param name="photoUrl">URL ou caminho da imagem a ser exibida</param>
        /// <param name="titulo">T�tulo a ser mostrado acima da imagem</param>
        public PhotoPopup(string photoUrl, string titulo)
        {
            InitializeComponent();
            BindingContext = new PhotoPopupViewModel(this, photoUrl, titulo);
        }

        /// <summary>
        /// Exibe um popup com uma imagem em tela cheia.
        /// </summary>
        /// <param name="photoUrl">URL ou caminho da imagem a ser exibida</param>
        /// <param name="titulo">T�tulo a ser mostrado acima da imagem</param>
        /// <returns>Task que completa quando o popup � fechado</returns>
        public static Task ShowAsync(string photoUrl, string titulo)
        {
            var popup = new PhotoPopup(photoUrl, titulo);

            // Usa o m�todo do NavigationUtils para exibir o popup
            return NavigationUtils.ShowPopupAsync(popup);
        }
    }

    public partial class PhotoPopupViewModel : ObservableObject
    {
        private readonly Popup _popup; // Tipo Popup simples (correto)

        [ObservableProperty] ImageSource? photoSource;
        [ObservableProperty] string titulo;

        public PhotoPopupViewModel(Popup popup, string photoUrl, string titulo)
        {
            _popup = popup;
            Titulo = titulo;

            if (!string.IsNullOrEmpty(photoUrl))
            {
                PhotoSource = ImageSource.FromFile(photoUrl);
            }
        }

        /// <summary>
        /// Comando para fechar o popup.
        /// </summary>
        [RelayCommand]
        private Task CloseAsync() // **MUDAN�A:** Renomeado e tornado ass�ncrono para Task
        {
            return _popup.CloseAsync();
        }
    }
}
