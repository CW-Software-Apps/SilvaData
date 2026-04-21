namespace SilvaData.Resources
{
    /// <summary>
    /// Helper para acesso simplificado a textos localizados
    /// </summary>
    public static class LocalizationHelper
    {
        /// <summary>
        /// Obt�m um texto localizado formatado com os argumentos fornecidos
        /// </summary>
        /// <param name="resourceKey">Chave do recurso de localiza��o</param>
        /// <param name="args">Argumentos para formata��o</param>
        /// <returns>Texto localizado e formatado</returns>
        public static string Format(string resourceKey, params object[] args)
        {
            // Obt�m a propriedade do recurso usando reflex�o
            var property = typeof(Localization.Localization).GetProperty(resourceKey);
            if (property == null)
                return $"[{resourceKey}]";
                
            var value = property.GetValue(null) as string;
            if (string.IsNullOrEmpty(value))
                return $"[{resourceKey}]";
                
            // Formata o texto com os argumentos fornecidos
            if (args != null && args.Length > 0)
                return string.Format(value, args);
                
            return value;
        }
    }
}
