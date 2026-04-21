namespace SilvaData.Controls
{
    /// <summary>
    /// Essa INTERFACE serve para permitir o controle de campos obrigat�rios no Form
    /// </summary>
    public interface ICampoObrigatorio
    {
        /// <summary>
        /// Fun��o Obrigat�ria para Verificar se o Campo Est� Preenchido Corratamente
        /// Nesta fun��o � necess�rio setar o hasError do sfInputField
        /// </summary>
        bool PreenchidoCorretamente();
    }
}
