using SilvaData.Models;

using Microsoft.Maui.Controls;

namespace SilvaData.Utilities
{
    /// <summary>
    /// ? Selector que escolhe qual template/content renderizar
    /// Renderiza APENAS o que � necess�rio
    /// </summary>
    public class AvaliacaoTypeTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? ISIMacroTemplate { get; set; }
        public DataTemplate? QuantitativoTemplate { get; set; }
        public DataTemplate? QualitativoTemplate { get; set; }

        protected override DataTemplate? OnSelectTemplate(object item, BindableObject container)
        {
            // item � um enum ou tipo que define qual visualizar
            if (item is AvaliacaoType tipo)
            {
                return tipo switch
                {
                    AvaliacaoType.ISIMacro => ISIMacroTemplate,
                    AvaliacaoType.Quantitativo => QuantitativoTemplate,
                    AvaliacaoType.Qualitativo => QualitativoTemplate,
                    _ => ISIMacroTemplate
                };
            }

            return ISIMacroTemplate;
        }
    }

    /// <summary>
    /// Enum para definir tipo de avalia��o
    /// </summary>
    public enum AvaliacaoType
    {
        ISIMacro,
        Quantitativo,
        Qualitativo
    }
}
