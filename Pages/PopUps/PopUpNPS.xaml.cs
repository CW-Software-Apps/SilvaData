using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;

using SilvaData.Utils;

using SilvaData.Models;
using SilvaData.Utilities;

using System.Windows.Input;

namespace SilvaData.Pages.PopUps
{
    /// <summary>
    /// Popup para avalia��o Net Promoter Score (NPS).
    /// </summary>
    public partial class PopUpNPS : Popup<NPSResult>
    {
        /// <summary>
        /// Inicializa uma nova inst�ncia do popup de avalia��o NPS.
        /// </summary>
        /// <param name="titulo">T�tulo do popup</param>
        /// <param name="mensagem">Mensagem explicativa sobre a avalia��o</param>
        public PopUpNPS(string titulo, string mensagem)
        {
            InitializeComponent();
            BindingContext = new PopUpNPSViewModel(this, titulo, mensagem);
        }

        /// <summary>
        /// Exibe um popup de avalia��o NPS e retorna o resultado da avalia��o.
        /// </summary>
        /// <param name="titulo">T�tulo do popup</param>
        /// <param name="mensagem">Mensagem explicativa sobre a avalia��o</param>
        /// <returns>Resultado da avalia��o (nota e coment�rios) ou valores padr�o se cancelado</returns>
        public static async Task<NPSResult> ShowAsync(string titulo, string mensagem)
        {
            var popup = new PopUpNPS(titulo, mensagem);

            // Usa o m�todo gen�rico do NavigationUtils que j� lida com o tipo de retorno
            var result = await NavigationUtils.ShowPopupAsync<NPSResult>(popup);

            // Garante que nunca retorne nulo, mesmo se o usu�rio fechar o popup sem selecionar
            return result ?? NPSResult.Default();
        }
    }

    /// <summary>
    /// ViewModel para o popup de avalia��o NPS.
    /// </summary>
    public partial class PopUpNPSViewModel : ObservableObject
    {
        private readonly PopUpNPS _popup;

        /// <summary>
        /// T�tulo do popup.
        /// </summary>
        public string Titulo { get; }

        /// <summary>
        /// Mensagem explicativa sobre a avalia��o.
        /// </summary>
        public string Mensagem { get; }

        /// <summary>
        /// Nota dada pelo usu�rio (0-10).
        /// </summary>
        [ObservableProperty]
        private double rating = 5;

        /// <summary>
        /// Coment�rios adicionais fornecidos pelo usu�rio.
        /// </summary>
        [ObservableProperty]
        private string comments = string.Empty;

        /// <summary>
        /// Comando para enviar a avalia��o.
        /// </summary>
        public ICommand EnviarCommand { get; }

        /// <summary>
        /// Comando para cancelar a avalia��o.
        /// </summary>
        public ICommand CancelarCommand { get; }

        /// <summary>
        /// Inicializa uma nova inst�ncia do ViewModel.
        /// </summary>
        /// <param name="popup">Refer�ncia para o popup</param>
        /// <param name="titulo">T�tulo do popup</param>
        /// <param name="mensagem">Mensagem explicativa sobre a avalia��o</param>
        public PopUpNPSViewModel(PopUpNPS popup, string titulo, string mensagem)
        {
            _popup = popup ?? throw new ArgumentNullException(nameof(popup));
            Titulo = titulo ?? "Avalia��o";
            Mensagem = mensagem ?? "Avalie nossa solu��o";

            EnviarCommand = new Command(Enviar);
            CancelarCommand = new Command(Cancelar);
        }

        private void Enviar()
        {
            _popup.CloseAsync(new NPSResult
            {
                Rating = (int)Rating,
                Comments = Comments ?? string.Empty
            });
        }

        private void Cancelar()
        {
            _popup.CloseAsync();
        }

        public static bool JaDeuNotaNPS => (!string.IsNullOrEmpty(ISIWebService.Instance.LoggedUser.nps) && ISIWebService.Instance.LoggedUser.nps != "-1");

    }
}
