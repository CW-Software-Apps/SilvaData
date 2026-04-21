using SilvaData.Models;
using SilvaData.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

// MUDAN�A: Substituindo Xamarin.Forms por Microsoft.Maui.Controls
using Microsoft.Maui.Controls;

namespace ISIInstitute.Views.Templates
{
    // A l�gica do DataTemplateSelector � 100% compat�vel com MAUI
    public class AvaliacaoGalpaoTemplateSelector : DataTemplateSelector
    {
        public DataTemplate Quantitativo { get; set; }
        public DataTemplate QualitativoUnico { get; set; }
        public DataTemplate QualitativoMultiplo { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            // O tipo do item (LoteFormAvaliacaoGalpao) e a l�gica do switch s�o mantidos
            switch (((LoteFormAvaliacaoGalpao)item).Parametro?.campoTipo)
            {
                case null:
                case "": return Quantitativo;
                case "1": return QualitativoUnico;
                case "2": return QualitativoMultiplo;
            }

            return null;
        }
    }

    // A l�gica do DataTemplateSelector � 100% compat�vel com MAUI
    public class AvaliacaoGalpaoListagemGridTemplateSelector : DataTemplateSelector
    {
        public DataTemplate Quantitativo { get; set; }
        public DataTemplate QualitativoUnico { get; set; }
        public DataTemplate QualitativoMultiplo { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            // O tipo do item (AvaliacaoGalpaoButton) e a l�gica do switch s�o mantidos
            switch (((AvaliacaoGalpaoButton)item).CampoTipo)
            {
                case null:
                case "": return Quantitativo;
                case "1": return QualitativoUnico;
                case "2": return QualitativoMultiplo;
            }

            return null;
        }
    }
}
