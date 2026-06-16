using System.Globalization;

namespace GorodTV.Core.Converters;

/// <summary>true → false и обратно. Для «показывай подложку, когда иконки НЕТ».</summary>
public class InvertedBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : true;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : false;
}
