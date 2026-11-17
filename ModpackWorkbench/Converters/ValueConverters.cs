using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ModpackWorkbench.Converters;

public sealed class StringEqualConverter : IValueConverter
{
    public static readonly StringEqualConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return string.Equals(value?.ToString(), parameter?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true && parameter is not null)
            return parameter.ToString()!;
        return Avalonia.Data.BindingOperations.DoNothing;
    }
}

public sealed class HexToBrushConverter : IValueConverter
{
    public static readonly HexToBrushConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string hex && Color.TryParse(hex, out var color))
            return new SolidColorBrush(color);
        return new SolidColorBrush(Color.Parse("#848494"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Avalonia.Data.BindingOperations.DoNothing;
}

public sealed class HexToTintBrushConverter : IValueConverter
{
    public static readonly HexToTintBrushConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string hex && Color.TryParse(hex, out var color))
            return new SolidColorBrush(Color.FromArgb(38, color.R, color.G, color.B));
        return new SolidColorBrush(Color.FromArgb(20, 132, 132, 148));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Avalonia.Data.BindingOperations.DoNothing;
}

public sealed class BoolToActiveStringConverter : IValueConverter
{
    public static readonly BoolToActiveStringConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool b = value is true;
        if (parameter is string s && s.Contains('|'))
        {
            var parts = s.Split('|', 2);
            return b ? parts[0] : parts[1];
        }
        return b ? "Убрать" : "Выбрать";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Avalonia.Data.BindingOperations.DoNothing;
}
