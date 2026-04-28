using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SilvaData.Models;
using SilvaData.Utilities;
using System.Diagnostics;

namespace SilvaData.Controls
{
    public partial class ValorInteiroComBotoes : ValidatableFieldBase
    {
        private ParametroComAlternativas? _parametroComAlternativas;
        public ParametroComAlternativas? ParametroComAlternativas
        {
            get => _parametroComAlternativas;
            set
            {
                _parametroComAlternativas = value;
                OnPropertyChanged();
            }
        }
        private bool _suppressValueSideEffects;

        #region IsReadOnly BindableProperty

        public static readonly BindableProperty IsReadOnlyProperty =
            BindableProperty.Create(
                nameof(IsReadOnly),
                typeof(bool),
                typeof(ValorInteiroComBotoes),
                false,
                BindingMode.TwoWay,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var control = (ValorInteiroComBotoes)bindable;
                    control.OnPropertyChanged(nameof(IsReadOnly));
                    control.OnPropertyChanged(nameof(NotIsReadOnly));
                    Debug.WriteLine($"[ValorInteiroComBotoes] IsReadOnly={newValue}");
                    control.ScheduleValidationRefresh();
                });

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        public bool NotIsReadOnly => !IsReadOnly;

        #endregion

        #region Valor BindableProperty

        public static readonly BindableProperty ValorProperty =
            BindableProperty.Create(
                nameof(Valor),
                typeof(int?),
                typeof(ValorInteiroComBotoes),
                null,
                BindingMode.TwoWay,
                propertyChanged: OnValorChanged);

        private static void OnValorChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (ValorInteiroComBotoes)bindable;
            int? previousValue = (int?)oldValue;
            int? val = (int?)newValue;

            if (control._suppressValueSideEffects)
            {
                int? suppressedNormalizedValue = control.NormalizeValue(val);
                control.SyncValueToParameter(suppressedNormalizedValue);
                control.OnPropertyChanged(nameof(Valor));
                control.OnPropertyChanged(nameof(ValorTexto));
                return;
            }

            int? normalizedValue = control.NormalizeValue(val);

            if (normalizedValue != val)
            {
                try
                {
                    control._suppressValueSideEffects = true;
                    control.Valor = normalizedValue;
                }
                finally
                {
                    control._suppressValueSideEffects = false;
                }

                val = normalizedValue;
            }

            control.SyncValueToParameter(val);

            control.OnPropertyChanged(nameof(Valor));
            control.OnPropertyChanged(nameof(ValorTexto));
            Debug.WriteLine($"[ValorInteiroComBotoes] Valor: {val} - Respondida: {control.ParametroComAlternativas?.EstaRespondida}");

            // ✅ Dispara mensagens para recalcular score/validações imediatamente
            WeakReferenceMessenger.Default.Send(new PropriedadeMudouMessage("ValorInt", previousValue, val));
            WeakReferenceMessenger.Default.Send(new UpdateScoreMessage());

            control.ScheduleValidationRefresh();
        }

        public int? Valor
        {
            get => (int?)GetValue(ValorProperty);
            set => SetValue(ValorProperty, value);
        }

        public string ValorTexto
        {
            get => Valor?.ToString() ?? string.Empty;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    if (Valor != null)
                        Valor = null;
                    return;
                }

                if (int.TryParse(value, out int parsed))
                {
                    if (Valor != parsed)
                        Valor = parsed;
                }
            }
        }

        #endregion

        public ValorInteiroComBotoes()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CRASH_INICIALIZACAO] ValorInteiroComBotoes: {ex.Message}");
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

        protected override void OnContextAttached()
        {
            HasError = false;

            if (BindingContext is not ParametroComAlternativas param)
            {
                OnContextCleared();
                return;
            }

            ParametroComAlternativas = param;

            try
            {
                _suppressValueSideEffects = true;
                Valor = null;

                int? loadedValue = null;
                if (ParametroComAlternativas.ValorInt.HasValue)
                {
                    loadedValue = ParametroComAlternativas.ValorInt;
                }
                else if (int.TryParse(ParametroComAlternativas.valorPadrao, out int valorPadraoInt))
                {
                    loadedValue = valorPadraoInt;
                }

                loadedValue = NormalizeValue(loadedValue);
                Valor = loadedValue;
            }
            finally
            {
                _suppressValueSideEffects = false;
            }

            RefreshHelperState();
        }

        protected override void OnContextCleared()
        {
            try
            {
                _suppressValueSideEffects = true;
                ParametroComAlternativas = null;
                Valor = null;
                HasError = false;
            }
            finally
            {
                _suppressValueSideEffects = false;
            }

            RefreshHelperState();
        }

        private void RefreshHelperState()
        {
            OnPropertyChanged(nameof(ValorTexto));
            OnPropertyChanged(nameof(HelperText));
            OnPropertyChanged(nameof(ShowHelperText));
        }

        // Helper text (intervalo permitido)
        public string HelperText
        {
            get
            {
                if (ParametroComAlternativas == null || !ShowHelperText)
                    return string.Empty;

                string min = ParametroComAlternativas.valorMinimo?.ToString("0") ?? "∞";
                string max = ParametroComAlternativas.valorMaximo?.ToString("0") ?? "∞";
                return $"Intervalo: {min} - {max}";
            }
        }

        public bool ShowHelperText =>
            ParametroComAlternativas != null &&
            (ParametroComAlternativas.valorMinimo.HasValue || ParametroComAlternativas.valorMaximo.HasValue);

        protected override bool ComputeHasError()
        {
            // ✅ Sincronização Extra: Se Valor estiver nulo mas o NumericUpDown tiver valor, força a atualização.
            // Isso evita o erro reportado onde Valor fica null mesmo quando preenchido na UI.
            var inner = this.FindByName<ISINumericUpDown>("innerNumeric");
            if (!Valor.HasValue && inner != null && inner.Value.HasValue)
            {
                Valor = inner.Value;
                Debug.WriteLine($"[VALIDATION] Sincronização rápida em ValorInteiro (SilvaData): Valor atualizado para {Valor}");
            }

            SyncValueToParameter(Valor);

            // ✅ CORREÇÃO: 0 é um valor válido. O erro ocorre apenas se for nulo e obrigatório.
            return ParametroComAlternativas?.required == 1
                && !Valor.HasValue
                && IsAnyValidationActive
                && !IsReadOnly;
        }

        protected override void ApplyValidationVisualState(bool hasError)
        {
            // Garante que a UI seja atualizada na thread principal
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var titleLabel = this.FindByName<Label>("labelTitle");
                var border = this.FindByName<Border>("valorInteiroBorder");

                ValidationVisualHelper.ApplyTitleColor(titleLabel, hasError);

                if (border != null)
                {
                    border.Stroke = hasError ? Colors.Red : ValidationVisualHelper.GetPrimaryColor();
                    border.StrokeThickness = hasError ? 2 : 1;
                }
            });
        }

        private int? NormalizeValue(int? value)
        {
            if (ParametroComAlternativas == null || !value.HasValue)
            {
                return value;
            }

            if (ParametroComAlternativas.valorMinimo.HasValue && value < (int)ParametroComAlternativas.valorMinimo.Value)
            {
                return (int)ParametroComAlternativas.valorMinimo.Value;
            }

            if (ParametroComAlternativas.valorMaximo.HasValue && value > (int)ParametroComAlternativas.valorMaximo.Value)
            {
                return (int)ParametroComAlternativas.valorMaximo.Value;
            }

            return value;
        }

        private void SyncValueToParameter(int? value)
        {
            if (ParametroComAlternativas == null)
            {
                return;
            }

            ParametroComAlternativas.ValorInt = value;
        }
    }
}
