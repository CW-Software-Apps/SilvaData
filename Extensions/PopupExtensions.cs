using SilvaData.Models;

namespace SilvaData.Extensions
{
    /// <summary>
    /// Extens�es para facilitar o uso de popups em toda a aplica��o
    /// </summary>
    public static class PopupExtensions
    {
        #region PhotoPopup
        public static Task ShowPhotoPopupAsync(this Page page, string photoUrl, string titulo)
        {
            return Pages.PopUps.PhotoPopup.ShowAsync(photoUrl, titulo);
        }

        public static Task ShowPhotoPopupAsync(string photoUrl, string titulo)
        {
            return Pages.PopUps.PhotoPopup.ShowAsync(photoUrl, titulo);
        }
        #endregion

        #region YesNoPopup
        /// <summary>
        /// Mostra um popup de confirma��o com bot�es Sim/N�o
        /// </summary>
        public static Task<bool> ShowYesNoPopupAsync(this Page page, string titulo, string mensagem, string? textoSim = null, string? textoNao = null)
        {
            var sim = string.IsNullOrEmpty(textoSim) ? Traducao.Sim : textoSim;
            var nao = string.IsNullOrEmpty(textoNao) ? Traducao.Nao : textoNao;

            return Pages.PopUps.PopUpYesNo.ShowAsync(titulo, mensagem, sim, nao);
        }

        /// <summary>
        /// Mostra um popup de confirma��o com bot�es Sim/N�o a partir do Shell atual
        /// </summary>
        public static Task<bool> ShowYesNoPopupAsync(string titulo, string mensagem, string? textoSim = null, string? textoNao = null)
        {
            var sim = string.IsNullOrEmpty(textoSim) ? Traducao.Sim : textoSim;
            var nao = string.IsNullOrEmpty(textoNao) ? Traducao.Nao : textoNao;

            return Pages.PopUps.PopUpYesNo.ShowAsync(titulo, mensagem, sim, nao);
        }
        #endregion

        #region OKPopup
        public static Task ShowOKPopupAsync(this Page page, string titulo, string mensagem)
        {
            return Pages.PopUps.PopUpOK.ShowAsync(titulo, mensagem);
        }

        public static Task ShowOKPopupAsync(string titulo, string mensagem)
        {
            return Pages.PopUps.PopUpOK.ShowAsync(titulo, mensagem);
        }
        #endregion

        #region NPSPopup
        public static Task<NPSResult> ShowNPSPopupAsync(this Page page, string titulo, string mensagem)
        {
            return Pages.PopUps.PopUpNPS.ShowAsync(titulo, mensagem);
        }

        public static Task<NPSResult> ShowNPSPopupAsync(string titulo, string mensagem)
        {
            return Pages.PopUps.PopUpNPS.ShowAsync(titulo, mensagem);
        }
        #endregion

        #region FecharLotePopup
        public static Task<LoteFechamentoInfo> ShowFecharLotePopupAsync(this Page page, string titulo, string mensagem)
        {
            return Pages.PopUps.PopUpFecharLote.ShowAsync(titulo, mensagem);
        }

        public static Task<LoteFechamentoInfo> ShowFecharLotePopupAsync(string titulo, string mensagem)
        {
            return Pages.PopUps.PopUpFecharLote.ShowAsync(titulo, mensagem);
        }
        #endregion

        #region PrivacyPopup
        public static Task<bool> ShowPrivacyPopupAsync(this Page page, string titulo, string textoPrivacidade)
        {
            return Pages.PopUps.PopUpPrivacy.ShowAsync(titulo, textoPrivacidade);
        }

        public static Task<bool> ShowPrivacyPopupAsync(string titulo, string textoPrivacidade)
        {
            return Pages.PopUps.PopUpPrivacy.ShowAsync(titulo, textoPrivacidade);
        }
        #endregion

        //#region SelectModeloPopup
        ///// <summary>
        ///// Mostra um popup para sele��o de modelo
        ///// </summary>
        //public static Task<T?> ShowSelectModeloPopupAsync<T>(this Page page, string titulo, IEnumerable<T> modelos, Func<T, string> nomeSelector) where T : class
        //{
        //    return Pages.PopUps.SelectModeloPopup.ShowAsync(titulo, modelos, nomeSelector);
        //}

        ///// <summary>
        ///// Mostra um popup para sele��o de modelo a partir do Shell atual
        ///// </summary>
        //public static Task<T?> ShowSelectModeloPopupAsync<T>(string titulo, IEnumerable<T> modelos, Func<T, string> nomeSelector) where T : class
        //{
        //    return Pages.PopUps.SelectModeloPopup.ShowAsync(titulo, modelos, nomeSelector);
        //}
        //#endregion
    }
}
