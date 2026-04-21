namespace SilvaData.Models
{
    /// <summary>
    /// Resultado da avalia��o NPS.
    /// </summary>
    public class NPSResult
    {
        /// <summary>
        /// Nota dada pelo usu�rio (0-10).
        /// </summary>
        public int Rating { get; set; }

        /// <summary>
        /// Coment�rios adicionais fornecidos pelo usu�rio.
        /// </summary>
        public string Comments { get; set; } = string.Empty;

        /// <summary>
        /// Cria um novo resultado NPS com valores padr�o.
        /// </summary>
        public static NPSResult Default() => new()
        {
            Rating = 0,
            Comments = string.Empty
        };
    }

}
