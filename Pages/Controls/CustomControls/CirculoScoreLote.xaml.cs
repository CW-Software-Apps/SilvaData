using Microsoft.Maui.Graphics;

using SilvaData.Models;

namespace SilvaData.Controls
{
    public partial class CirculoScoreLote : ContentView
    {
        public static readonly BindableProperty ValorProperty =
            BindableProperty.Create(
                nameof(Valor),
                typeof(double),
                typeof(CirculoScoreLote),
                0.0,
                BindingMode.OneWay,
                coerceValue: CoerceValor,
                propertyChanged: OnValorChanged);

        public double Valor
        {
            get => (double)GetValue(ValorProperty);
            set => SetValue(ValorProperty, value);
        }

        // Propriedades calculadas (Read-only para a UI)
        public double ValorGauge => Valor <= 0 ? 0.01 : Valor;
        public Color ValorColor => ISIMacro.StatusColor(Valor);
        public Color ValorColorBackground => ISIMacro.StatusColorBackground(Valor);

        public CirculoScoreLote()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Garante que o valor recebido esteja sempre dentro do limite esperado.
        /// Substitui a l�gica manual de Math.Min dentro da propriedade.
        /// </summary>
        private static object CoerceValor(BindableObject bindable, object value)
        {
            var valor = (double)value;
            return Math.Clamp(valor, 0, 60);
        }

        /// <summary>
        /// Notifica as propriedades dependentes de forma centralizada.
        /// </summary>
        private static void OnValorChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is CirculoScoreLote control)
            {
                // Dispara a atualiza��o das propriedades calculadas que a UI consome
                control.OnPropertyChanged(nameof(ValorGauge));
                control.OnPropertyChanged(nameof(ValorColor));
                control.OnPropertyChanged(nameof(ValorColorBackground));
            }
        }
    }
}
