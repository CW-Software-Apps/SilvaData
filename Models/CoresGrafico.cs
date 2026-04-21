using System.Collections.Generic;
using System.Linq;

using Microsoft.Maui.Graphics;

namespace SilvaData.Models
{
    /// <summary>
    /// Gerencia a paleta de cores din�micas para os gr�ficos.
    /// </summary>
    public class CoresGrafico
    {
        public static CoresGrafico Instance { get; } = new();

        List<CorGrafico> ListaCoresSuperCategoria;
        List<CorGrafico> ListaCoresCategoria;
        List<CorGrafico> ListaCoresParametros;

        private class CorGrafico
        {
            public string CodigoGrupo;
            public string DescricaoGrupo;
            public Color Color;

            public CorGrafico(string name, Color color)
            {
                CodigoGrupo = name;
                DescricaoGrupo = string.Empty;
                Color = color;
            }
        }

        private CoresGrafico()
        {
            ListaCoresSuperCategoria = new List<CorGrafico> {
                new("", Color.FromArgb("#fb532e")), new("", Color.FromArgb("#663ab3") ),
                new("", Color.FromArgb("#2996ef") ), new("", Color.FromArgb("#fd9626")),
                new("", Color.FromArgb("#28d195")), new("", Color.FromArgb("#fdea4e") ),
                new("", Color.FromArgb("#D14028") ), new("", Color.FromArgb("#ff90ca") ),
                new("", Color.FromArgb("#83d132") ), new("", Color.FromArgb("#61d4e1") ),
                new("", Color.FromArgb("#3f51b1")), new("", Color.FromArgb("#4faf54")),
                new("", Color.FromArgb("#3A93BD")), new("", Color.FromArgb("#FD2173")),
                new("", Color.FromArgb("#344CBF") ), new("", Color.FromArgb("#1EF8E6") ),
                new("", Color.FromArgb("#FE4D19")), new("", Color.FromArgb("#3992BD")),
                new("", Color.FromArgb("#FEC242") ), new("", Color.FromArgb("#78ED5A") ),
                new("", Color.FromArgb("#ff4466") ), new("", Color.FromArgb("#AB2ADE") ),
                new("", Color.FromArgb("#DE731F") ), new("", Color.FromArgb("#3f51b1"))
            };
            ListaCoresCategoria = new List<CorGrafico> {
                new("", Color.FromArgb("#3a405a")), new("", Color.FromArgb("#aec5eb") ),
                new("", Color.FromArgb("#f9dec9") ), new("", Color.FromArgb("#e9afa3")),
                new("", Color.FromArgb("#685044")), new("", Color.FromArgb("#e3b23c") ),
                new("", Color.FromArgb("#edebd7") ), new("", Color.FromArgb("#a39594") ),
                new("", Color.FromArgb("#f94144") ), new("", Color.FromArgb("#f3722c") ),
                new("", Color.FromArgb("#90be6d")), new("", Color.FromArgb("#43aa8b")),
                new("", Color.FromArgb("#577590")), new("", Color.FromArgb("#d0ddd7"))
            };
            ListaCoresParametros = new List<CorGrafico> {
                new("", Color.FromArgb("#3a405a")), new("", Color.FromArgb("#aec5eb") ),
                new("", Color.FromArgb("#558564") ), new("", Color.FromArgb("#e9afa3")),
                new("", Color.FromArgb("#685044")), new("", Color.FromArgb("#e3b23c") ),
                new("", Color.FromArgb("#edebd7") ), new("", Color.FromArgb("#a39594") ),
                new("", Color.FromArgb("#f94144") ), new("", Color.FromArgb("#f3722c") ),
                new("", Color.FromArgb("#90be6d")), new("", Color.FromArgb("#43aa8b")),
                new("", Color.FromArgb("#577590")), new("", Color.FromArgb("#d0ddd7")),
                new("", Color.FromArgb("#3a405a")), new("", Color.FromArgb("#aec5eb") ),
                new("", Color.FromArgb("#f9dec9") ), new("", Color.FromArgb("#e9afa3")),
                new("", Color.FromArgb("#685044")), new("", Color.FromArgb("#e3b23c") ),
                new("", Color.FromArgb("#edebd7") ), new("", Color.FromArgb("#a39594") ),
                new("", Color.FromArgb("#f94144") ), new("", Color.FromArgb("#f3722c") ),
                new("", Color.FromArgb("#90be6d")), new("", Color.FromArgb("#43aa8b")),
                new("", Color.FromArgb("#577590")), new("", Color.FromArgb("#d0ddd7"))
            };
        }

        public Color PegaCor(string codigoGrupo, string descricaoGrupo, int Lista)
        {
            List<CorGrafico>? PegaCorDeQualLista = null;
            if (Lista == 1) PegaCorDeQualLista = ListaCoresSuperCategoria;
            if (Lista == 2) PegaCorDeQualLista = ListaCoresCategoria;
            if (Lista == 3) PegaCorDeQualLista = ListaCoresParametros;

            if (PegaCorDeQualLista == null) return Colors.Black;

            List<CorGrafico> TodasCores = new List<CorGrafico>();
            TodasCores.AddRange(ListaCoresSuperCategoria);
            TodasCores.AddRange(ListaCoresCategoria);
            TodasCores.AddRange(ListaCoresParametros);

            foreach (var cor in TodasCores.Where(cor => cor.DescricaoGrupo != null && cor.DescricaoGrupo.Equals(descricaoGrupo)))
                return cor.Color;

            foreach (var cor in PegaCorDeQualLista.Where(cor => string.IsNullOrEmpty(cor.CodigoGrupo)))
            {
                cor.CodigoGrupo = codigoGrupo;
                cor.DescricaoGrupo = descricaoGrupo;
                return cor.Color;
            }

            var fallback = PegaCorDeQualLista.FirstOrDefault();
            return fallback?.Color ?? Colors.Black;
        }
    }
}
