namespace SilvaData.Controls
{
    /// <summary>
    /// Define uma interface para uma View que pode validar a si mesma.
    /// </summary>
    public interface IValidatablePage
    {
        /// <summary>
        /// Executa a valida��o da UI e retorna true se for v�lida.
        /// </summary>
        Task<bool> ValidateFormAsync();
    }
}
