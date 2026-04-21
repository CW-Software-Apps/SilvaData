// ═══════════════════════════════════════════════════════════════════════════
// ★★★ CONVERTERS DE ÍCONES E VALIDAÇÃO ★★★
// ═══════════════════════════════════════════════════════════════════════════

using System.Globalization;
using System.Collections;

namespace SilvaData.Converters
{
    /// <summary>
    /// Converte bool → ícone FontAwesome (true = Check, false = Square).
    /// ✅ Usado para mostrar estado de seleção com ícones.
    /// 
    /// Uso em XAML:
    /// <Label FontFamily="FontAwesomeSolid"
    ///        Text="{Binding IsSelected, Converter={StaticResource BoolToIconConverter}}" />
    /// </summary>
    public class BoolToIconConverter : IValueConverter
    {
        /// <summary>
        /// Ícone exibido quando o valor é TRUE.
        /// Padrão: CheckSquare (✓ dentro de um quadrado).
        /// </summary>
        public string TrueIcon { get; set; } = FontAwesome.FontAwesomeIcons.SquareCheck;

        /// <summary>
        /// Ícone exibido quando o valor é FALSE ou NULL.
        /// Padrão: Square (quadrado vazio).
        /// </summary>
        public string FalseIcon { get; set; } = FontAwesome.FontAwesomeIcons.Square;

        /// <summary>
        /// Converte bool → string (ícone FontAwesome).
        /// </summary>
        /// <param name="value">Valor booleano</param>
        /// <returns>String do ícone FontAwesome</returns>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Se parameter for fornecido, usa como TrueIcon
            if (parameter is string customIcon && value is bool b && b)
                return customIcon;

            return value is bool boolValue && boolValue ? TrueIcon : FalseIcon;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException("BoolToIconConverter é um converter unidirecional (somente Convert)");
        }
    }

    /// <summary>
    /// Verifica se um valor numérico é ZERO (ou null).
    /// ✅ Útil para exibir Empty Views quando Count == 0.
    /// 
    /// Uso em XAML:
    /// <Label IsVisible="{Binding Items.Count, Converter={StaticResource IsZeroConverter}}" 
    ///        Text="Nenhum item encontrado" />
    /// </summary>
    public class IsZeroConverter : IValueConverter
    {
        /// <summary>
        /// Converte valor → bool (retorna true se for 0 ou null).
        /// </summary>
        /// <param name="value">Valor numérico (int, double, float, etc)</param>
        /// <returns>True se for 0 ou null, false caso contrário</returns>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Se for null, considera como zero
            if (value == null)
                return true;

            // Tenta converter para double
            if (value is IConvertible convertible)
            {
                try
                {
                    double numericValue = convertible.ToDouble(culture);
                    return Math.Abs(numericValue) < 0.001; // Considera 0 com tolerância para float/double
                }
                catch (FormatException)
                {
                    return true; // Se não conseguir converter, considera como zero
                }
            }

            // Se não for convertível, considera como zero
            return true;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException("IsZeroConverter é um converter unidirecional (somente Convert)");
        }
    }

    /// <summary>
    /// Verifica se um valor numérico NÃO é ZERO (inverso do IsZeroConverter).
    /// ✅ Útil para esconder Empty Views quando há dados.
    /// 
    /// Uso em XAML:
    /// <ListView IsVisible="{Binding Items.Count, Converter={StaticResource IsNotZeroConverter}}" />
    /// </summary>
    public class IsNotZeroConverter : IValueConverter
    {
        private static readonly IsZeroConverter _isZeroConverter = new();

        /// <summary>
        /// Converte valor → bool (retorna false se for 0 ou null).
        /// </summary>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Reutiliza IsZeroConverter e inverte o resultado
            var isZero = _isZeroConverter.Convert(value, targetType, parameter, culture);
            return isZero is bool b ? !b : false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException("IsNotZeroConverter é um converter unidirecional (somente Convert)");
        }
    }

    // ═══════════════════════════════════════
    // CONVERTERS ADICIONADOS (FALTANTES NO XAML)
    // ═══════════════════════════════════════

    public class StatusStyleConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is null) return Colors.Transparent;
            var status = value.ToString()?.Trim().ToLowerInvariant();
            return status switch
            {
                "aberto" => Color.FromArgb("#70AC3E"),
                "fechado" => Color.FromArgb("#E0E0E0"),
                _ => Color.FromArgb("#5A8A32")
            };
        }
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException("StatusStyleConverter é unidirecional");
    }

    public class DateToStringConverter : IValueConverter
    {
        public string Format { get; set; } = "dd/MM/yyyy";
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is DateTime dt) return dt.ToString(Format, culture);
            if (value is DateTimeOffset dto) return dto.ToString(Format, culture);
            return string.Empty;
        }
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string s && DateTime.TryParse(s, culture, DateTimeStyles.None, out var dt))
                return dt;
            return null;
        }
    }

    public class BooleanToStringConverter : IValueConverter
    {
        public string TrueText { get; set; } = "Sim";
        public string FalseText { get; set; } = "Não";
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Permite customização via parameter "True;False"
            if (parameter is string p && p.Contains(';'))
            {
                var parts = p.Split(';');
                if (parts.Length >= 2)
                {
                    TrueText = parts[0];
                    FalseText = parts[1];
                }
            }
            return value is bool b && b ? TrueText : FalseText;
        }
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string s)
            {
                if (string.Equals(s, TrueText, StringComparison.OrdinalIgnoreCase)) return true;
                if (string.Equals(s, FalseText, StringComparison.OrdinalIgnoreCase)) return false;
            }
            return false;
        }
    }

    public class StringToNullableDoubleConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double or float or decimal) return value;
            if (value is string s)
            {
                if (string.IsNullOrWhiteSpace(s)) return null;
                if (double.TryParse(s, NumberStyles.Any, culture, out var d)) return d;
            }
            return null;
        }
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double d) return d.ToString(culture);
            return string.Empty;
        }
    }

    public class TemDadosConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is null) return false;
            if (value is string s) return !string.IsNullOrWhiteSpace(s);
            if (value is ICollection col) return col.Count > 0;
            if (value is IEnumerable enumerable) return enumerable.Cast<object?>().Any();
            return true; // Qualquer outro objeto ≠ null conta como tendo dados
        }
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException("TemDadosConverter é unidirecional");
    }

    public class NaoTemDadosConverter : IValueConverter
    {
        private static readonly TemDadosConverter _temDados = new();
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var has = _temDados.Convert(value, targetType, parameter, culture);
            return has is bool b ? !b : false;
        }
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException("NaoTemDadosConverter é unidirecional");
    }

    public class NullToDefaultConverter : IValueConverter
    {
        public object? DefaultValue { get; set; } = string.Empty;
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var def = parameter ?? DefaultValue;
            if (value is null) return def;
            if (value is string s && string.IsNullOrWhiteSpace(s)) return def;
            return value;
        }
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value;
    }

    public class BooleanToOpacityConverter : IValueConverter
    {
        public double TrueOpacity { get; set; } = 1.0;
        public double FalseOpacity { get; set; } = 0.4;
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool b && b ? TrueOpacity : FalseOpacity;
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException("BooleanToOpacityConverter é unidirecional");
    }

    public class BoolToColorConverter : IValueConverter
    {
        public Color TrueColor { get; set; } = Colors.LightGray;
        public Color FalseColor { get; set; } = Color.FromArgb("#70AC3E"); // Primary
        public Color AccentColor { get; set; } = Color.FromArgb("#70AC3E");
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var useAccent = parameter is string p && p.Equals("Accent", StringComparison.OrdinalIgnoreCase);
            if (value is bool b && b)
                return useAccent ? AccentColor : TrueColor;
            return FalseColor;
        }
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException("BoolToColorConverter é unidirecional");
    }

    public class DateTimeNullableConverter : IValueConverter
    {
        public string Format { get; set; } = "dd/MM/yyyy";
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is DateTime dt)
            {
                if (targetType == typeof(string) || parameter as string == "String")
                    return dt.ToString(Format, culture);
                return dt;
            }
            return targetType == typeof(string) ? string.Empty : null;
        }
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string s && DateTime.TryParse(s, culture, DateTimeStyles.None, out var dt))
                return dt;
            return null;
        }
    }

    /// <summary>
    /// Converte porcentagem (0-100) para largura (WidthRequest) para barras de progresso.
    /// ✅ Usado em barras mini de resultados de avaliação
    /// 
    /// Uso em XAML:
    /// <Border WidthRequest="{Binding Porcentagem, Converter={StaticResource PercentToWidthConverter}}" />
    /// </summary>
    public class PercentToWidthConverter : IValueConverter
    {
        /// <summary>
        /// Largura máxima da barra (pode ser customizado via parameter)
        /// </summary>
        public double MaxWidth { get; set; } = 60.0;

        /// <summary>
        /// Converte porcentagem → largura proporcional
        /// </summary>
        /// <param name="value">Porcentagem (0-100)</param>
        /// <param name="parameter">Largura máxima (opcional)</param>
        /// <returns>Largura calculada proporcionalmente</returns>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Tenta obter largura máxima do parameter
            double maxWidth = MaxWidth;
            if (parameter is string paramStr && double.TryParse(paramStr, NumberStyles.Any, culture, out var customWidth))
            {
                maxWidth = customWidth;
            }

            // Converte valor para porcentagem
            if (value is double porcentagem)
            {
                // Garante que está no range 0-100
                porcentagem = Math.Max(0, Math.Min(100, porcentagem));
                // Calcula largura proporcional
                return (porcentagem / 100.0) * maxWidth;
            }

            if (value is int porcentagemInt)
            {
                var porc = Math.Max(0, Math.Min(100, porcentagemInt));
                return (porc / 100.0) * maxWidth;
            }

            // Se não conseguir converter, retorna 0
            return 0.0;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException("PercentToWidthConverter é unidirecional (somente Convert)");
        }
    }

    /// <summary>
    /// Converte porcentagem (0-100) para fração (0.0-1.0) para uso em ProgressBar.
    /// </summary>
    public class PercentToFractionConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            double percent = 0;
            if (value is IConvertible c)
            {
                try { percent = c.ToDouble(culture); } catch { percent = 0; }
            }
            if (double.IsNaN(percent) || double.IsInfinity(percent)) percent = 0;
            percent = Math.Clamp(percent, 0, 100);
            return percent / 100.0;
        }
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is IConvertible c)
            {
                try { var frac = Math.Clamp(c.ToDouble(culture), 0, 1); return (int)Math.Round(frac * 100); } catch { }
            }
            return 0;
        }
    }

    /// <summary>
    /// Converte bool → !bool.
    /// ✅ Substituto local para o InvertedBoolConverter do Toolkit (evita erros de compilação).
    /// </summary>
    public class InvertBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b) return !b;
            return value;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b) return !b;
            return value;
        }
    }

    public class IsListNullOrEmptyConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is null) return true;
            if (value is ICollection col) return col.Count == 0;
            if (value is IEnumerable enumerable) return !enumerable.Cast<object?>().Any();
            return false;
        }
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    public class IsListNotNullOrEmptyConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is null) return false;
            if (value is ICollection col) return col.Count > 0;
            if (value is IEnumerable enumerable) return enumerable.Cast<object?>().Any();
            return true;
        }
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    public class IsStringNullOrEmptyConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => string.IsNullOrEmpty(value as string);
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    public class IsStringNotNullOrEmptyConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => !string.IsNullOrEmpty(value as string);
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    public class IntToBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null) return false;
            
            try 
            {
                // Tenta converter qualquer valor numérico ou string para int
                int i = System.Convert.ToInt32(value);
                return i > 0;
            }
            catch 
            {
                // Se o valor for um booleano por algum motivo, retorna ele mesmo
                if (value is bool b) return b;
                
                return false;
            }
        }
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
