using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TaskManager.App.Converters;

public class PercentageToColorConverter : IValueConverter
{
    public Brush LowBrush { get; set; } = new SolidColorBrush(Color.FromRgb(0x00, 0x67, 0xC0));
    public Brush MediumBrush { get; set; } = new SolidColorBrush(Color.FromRgb(0xD8, 0x9B, 0x00));
    public Brush HighBrush { get; set; } = new SolidColorBrush(Color.FromRgb(0xC4, 0x2B, 0x1C));

    public double MediumThreshold { get; set; } = 60.0;
    public double HighThreshold { get; set; } = 80.0;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        double percent = 0;
        if (value is double d) percent = d;
        else if (value is float f) percent = f;
        else if (value is int i) percent = i;
        else if (value is long l) percent = l;

        if (percent >= HighThreshold) return HighBrush;
        if (percent >= MediumThreshold) return MediumBrush;
        return LowBrush;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
