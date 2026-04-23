using CommunityToolkit.Mvvm.Messaging;

using SilvaData.Models;

using System.Diagnostics;

namespace SilvaData.Controls
{
    /// <summary>
    /// Controle Sim/Não com cores de destaque para botão selecionado.
    /// </summary>
    public partial class YesNoToggle : ValidatableFieldBase
    {
        public ParametroComAlternativas? ParametroComAlternativas;

        #region IsReadOnly BindableProperty

        public static readonly BindableProperty IsReadOnlyProperty =
            BindableProperty.Create(
                nameof(IsReadOnly),
                typeof(bool),
                typeof(YesNoToggle),
                false,
                BindingMode.TwoWay,
                null,
                propertyChanged: ReadOnlyChanged);

        private static void ReadOnlyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ((YesNoToggle)bindable).OnPropertyChanged(nameof(IsReadOnly));
            ((YesNoToggle)bindable).ScheduleValidationRefresh();
        }

        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        #endregion

        public YesNoToggle()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CRASH_INICIALIZACAO] YesNoToggle: {ex.Message}");
                Exception? inner = ex.InnerException;
                while (inner != null)
                {
                    Debug.WriteLine($"[CRASH_INNER] {inner.Message}");
                    Debug.WriteLine($"[CRASH_TRACE] {inner.StackTrace}");
                    inner = inner.InnerException;
                }
                throw;
            }
        }

        /// <summary>
        /// Handler do botão "Não" - Toggle false/null.
        /// </summary>
        private void BtNo_Clicked(object sender, EventArgs e)
        {
            try
            {
                if (IsReadOnly) return;

                WeakReferenceMessenger.Default.Send(new VaiProProximoMessage(this));

                if (ParametroComAlternativas != null)
                {
                    // Toggle: false -> null -> false -> null...
                    ParametroComAlternativas.ValorSimNao =
                        ParametroComAlternativas.ValorSimNao == false ? null : (bool?)false;
                }

                HapticHelper.VibrateClick();
                Debug.WriteLine($"[YesNoToggle] ✗ Não clicado: {ParametroComAlternativas?.nome} = {ParametroComAlternativas?.ValorSimNao}");

                UpdateColorText();
                ScheduleValidationRefresh();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[YesNoToggle] Erro em BtNo_Clicked: {ex.Message}");
            }
        }

        /// <summary>
        /// Handler do botão "Sim" - Toggle true/null.
        /// </summary>
        private void BtYes_Clicked(object sender, EventArgs e)
        {
            try
            {
                if (IsReadOnly) return;

                WeakReferenceMessenger.Default.Send(new VaiProProximoMessage(this));

                if (ParametroComAlternativas != null)
                {
                    // Toggle: true -> null -> true -> null...
                    ParametroComAlternativas.ValorSimNao =
                        ParametroComAlternativas.ValorSimNao == true ? null : (bool?)true;
                }

                HapticHelper.VibrateClick();
                Debug.WriteLine($"[YesNoToggle] ✓ Sim clicado: {ParametroComAlternativas?.nome} = {ParametroComAlternativas?.ValorSimNao}");

                UpdateColorText();
                ScheduleValidationRefresh();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[YesNoToggle] Erro em BtYes_Clicked: {ex.Message}");
            }
        }

        /// <summary>
        /// Chamado quando o BindingContext muda (ItemTemplate).
        /// </summary>
        protected override void OnContextAttached()
        {
            if (BindingContext is ParametroComAlternativas p)
            {
                ParametroComAlternativas = p;
                UpdateColorText();
                return;
            }

            OnContextCleared();
        }

        protected override void OnContextCleared()
        {
            ParametroComAlternativas = null;
            UpdateColorText();
        }

        #region Color Customization BindableProperties

        public static readonly BindableProperty SelectedYesColorProperty =
            BindableProperty.Create(nameof(SelectedYesColor), typeof(Color), typeof(YesNoToggle), Color.FromArgb("#C8E6C9"), propertyChanged: (b, o, n) => ((YesNoToggle)b).UpdateColorText());

        public Color SelectedYesColor
        {
            get => (Color)GetValue(SelectedYesColorProperty);
            set => SetValue(SelectedYesColorProperty, value);
        }

        public static readonly BindableProperty UnselectedYesColorProperty =
            BindableProperty.Create(nameof(UnselectedYesColor), typeof(Color), typeof(YesNoToggle), Colors.WhiteSmoke, propertyChanged: (b, o, n) => ((YesNoToggle)b).UpdateColorText());

        public Color UnselectedYesColor
        {
            get => (Color)GetValue(UnselectedYesColorProperty);
            set => SetValue(UnselectedYesColorProperty, value);
        }

        public static readonly BindableProperty SelectedNoColorProperty =
            BindableProperty.Create(nameof(SelectedNoColor), typeof(Color), typeof(YesNoToggle), Color.FromArgb("#FFCDD2"), propertyChanged: (b, o, n) => ((YesNoToggle)b).UpdateColorText());

        public Color SelectedNoColor
        {
            get => (Color)GetValue(SelectedNoColorProperty);
            set => SetValue(SelectedNoColorProperty, value);
        }

        public static readonly BindableProperty UnselectedNoColorProperty =
            BindableProperty.Create(nameof(UnselectedNoColor), typeof(Color), typeof(YesNoToggle), Colors.WhiteSmoke, propertyChanged: (b, o, n) => ((YesNoToggle)b).UpdateColorText());

        public Color UnselectedNoColor
        {
            get => (Color)GetValue(UnselectedNoColorProperty);
            set => SetValue(UnselectedNoColorProperty, value);
        }

        public static readonly BindableProperty SelectedYesTextColorProperty =
            BindableProperty.Create(nameof(SelectedYesTextColor), typeof(Color), typeof(YesNoToggle), Color.FromArgb("#1B5E20"), propertyChanged: (b, o, n) => ((YesNoToggle)b).UpdateColorText());

        public Color SelectedYesTextColor
        {
            get => (Color)GetValue(SelectedYesTextColorProperty);
            set => SetValue(SelectedYesTextColorProperty, value);
        }

        public static readonly BindableProperty UnselectedYesTextColorProperty =
            BindableProperty.Create(nameof(UnselectedYesTextColor), typeof(Color), typeof(YesNoToggle), Color.FromArgb("#4CAF50"), propertyChanged: (b, o, n) => ((YesNoToggle)b).UpdateColorText());

        public Color UnselectedYesTextColor
        {
            get => (Color)GetValue(UnselectedYesTextColorProperty);
            set => SetValue(UnselectedYesTextColorProperty, value);
        }

        public static readonly BindableProperty SelectedNoTextColorProperty =
            BindableProperty.Create(nameof(SelectedNoTextColor), typeof(Color), typeof(YesNoToggle), Color.FromArgb("#B71C1C"), propertyChanged: (b, o, n) => ((YesNoToggle)b).UpdateColorText());

        public Color SelectedNoTextColor
        {
            get => (Color)GetValue(SelectedNoTextColorProperty);
            set => SetValue(SelectedNoTextColorProperty, value);
        }

        public static readonly BindableProperty UnselectedNoTextColorProperty =
            BindableProperty.Create(nameof(UnselectedNoTextColor), typeof(Color), typeof(YesNoToggle), Color.FromArgb("#F44336"), propertyChanged: (b, o, n) => ((YesNoToggle)b).UpdateColorText());

        public Color UnselectedNoTextColor
        {
            get => (Color)GetValue(UnselectedNoTextColorProperty);
            set => SetValue(UnselectedNoTextColorProperty, value);
        }

        #endregion

        #region Applied Colors (Bindable)

        public static readonly BindableProperty TextColorProperty =
            BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(YesNoToggle), Colors.Black);

        public Color TextColor
        {
            get => (Color)GetValue(TextColorProperty);
            set => SetValue(TextColorProperty, value);
        }

        public static readonly BindableProperty YesBrushProperty =
            BindableProperty.Create(nameof(YesBrush), typeof(Brush), typeof(YesNoToggle), new SolidColorBrush(Colors.WhiteSmoke));

        public Brush YesBrush
        {
            get => (Brush)GetValue(YesBrushProperty);
            set => SetValue(YesBrushProperty, value);
        }

        public static readonly BindableProperty YesTextColorProperty =
            BindableProperty.Create(nameof(YesTextColor), typeof(Color), typeof(YesNoToggle), Colors.Black);

        public Color YesTextColor
        {
            get => (Color)GetValue(YesTextColorProperty);
            set => SetValue(YesTextColorProperty, value);
        }

        public static readonly BindableProperty NoBrushProperty =
            BindableProperty.Create(nameof(NoBrush), typeof(Brush), typeof(YesNoToggle), new SolidColorBrush(Colors.WhiteSmoke));

        public Brush NoBrush
        {
            get => (Brush)GetValue(NoBrushProperty);
            set => SetValue(NoBrushProperty, value);
        }

        public static readonly BindableProperty NoTextColorProperty =
            BindableProperty.Create(nameof(NoTextColor), typeof(Color), typeof(YesNoToggle), Colors.Black);

        public Color NoTextColor
        {
            get => (Color)GetValue(NoTextColorProperty);
            set => SetValue(NoTextColorProperty, value);
        }

        #endregion

        /// <summary>
        /// Atualiza cores e texto quando o valor muda.
        /// </summary>
        private void UpdateColorText()
        {
            try
            {
                Action updateAction = () =>
                {
                    OnPropertyChanged(nameof(YesNoText));

                    // Atualiza cores calculadas nos BindableProperties
                    TextColor = HasError ? Colors.Red : ValidationVisualHelper.GetPrimaryColor();

                    if (ParametroComAlternativas?.ValorSimNao == true)
                    {
                        YesBrush = new SolidColorBrush(SelectedYesColor);
                        YesTextColor = SelectedYesTextColor;
                        NoBrush = new SolidColorBrush(UnselectedNoColor);
                        NoTextColor = UnselectedNoTextColor;
                    }
                    else if (ParametroComAlternativas?.ValorSimNao == false)
                    {
                        YesBrush = new SolidColorBrush(UnselectedYesColor);
                        YesTextColor = UnselectedYesTextColor;
                        NoBrush = new SolidColorBrush(SelectedNoColor);
                        NoTextColor = SelectedNoTextColor;
                    }
                    else
                    {
                        YesBrush = new SolidColorBrush(UnselectedYesColor);
                        YesTextColor = UnselectedYesTextColor;
                        NoBrush = new SolidColorBrush(UnselectedNoColor);
                        NoTextColor = UnselectedNoTextColor;
                    }
                };

                if (Dispatcher != null && Dispatcher.IsDispatchRequired)
                    Dispatcher.Dispatch(updateAction);
                else
                    updateAction();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[YesNoToggle] Erro em UpdateColorText: {ex.Message}");
            }
        }

        /// <summary>
        /// Implementação de ICampoObrigatorio.
        /// </summary>
        protected override bool ComputeHasError()
        {
            bool campoEstaVazio = (ParametroComAlternativas?.ValorSimNao == null);
            return ParametroComAlternativas?.required == 1 && campoEstaVazio && IsAnyValidationActive && !IsReadOnly;
        }

        protected override void ApplyValidationVisualState(bool hasError)
        {
            UpdateColorText();
        }

        public string? YesNoText => (ParametroComAlternativas == null || ParametroComAlternativas.ValorSimNao == null)
            ? null
            : (ParametroComAlternativas.ValorSimNao == true ? Traducao.Sim : Traducao.Nao);

        #region Propriedades Calculadas legadas (removidas em favor dos BindableProperties)
        // Removidas YesColor, NoColor para usar YesBrush e NoBrush
        #endregion
    }
}
