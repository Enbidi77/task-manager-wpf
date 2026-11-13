using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TaskManager.App.Converters;

public class ProcessStatusConverter : IValueConverter
{
    public Brush RunningBrush { get; set; } = new SolidColorBrush(Color.FromRgb(0x0F, 0x7B, 0x0F));
    public Brush SuspendedBrush { get; set; } = new SolidColorBrush(Color.FromRgb(0x9D, 0x5D, 0x00));
    public Brush NotRespondingBrush { get; set; } = new SolidColorBrush(Color.FromRgb(0xC4, 0x2B, 0x1C));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string status = value?.ToString() ?? string.Empty;

        if (status.Contains("Not Responding", StringComparison.OrdinalIgnoreCase))
            return NotRespondingBrush;

        if (status.Contains("Suspended", StringComparison.OrdinalIgnoreCase) ||
            status.Contains("Stopped", StringComparison.OrdinalIgnoreCase))
            return SuspendedBrush;

        return RunningBrush;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
