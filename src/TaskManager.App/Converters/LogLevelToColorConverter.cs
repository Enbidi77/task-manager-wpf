using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TaskManager.App.Converters;

public class LogLevelToColorConverter : IValueConverter
{
    private static readonly SolidColorBrush InformationBrush = new(Color.FromRgb(0, 150, 255));
    private static readonly SolidColorBrush WarningBrush = new(Color.FromRgb(255, 170, 0));
    private static readonly SolidColorBrush ErrorBrush = new(Color.FromRgb(255, 60, 60));
    private static readonly SolidColorBrush FatalBrush = new(Color.FromRgb(220, 20, 60));
    private static readonly SolidColorBrush DebugBrush = new(Color.FromRgb(150, 150, 150));
    private static readonly SolidColorBrush DefaultBrush = new(Color.FromRgb(180, 180, 180));

    static LogLevelToColorConverter()
    {
        InformationBrush.Freeze();
        WarningBrush.Freeze();
        ErrorBrush.Freeze();
        FatalBrush.Freeze();
        DebugBrush.Freeze();
        DefaultBrush.Freeze();
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string level)
            return DefaultBrush;

        return level.ToUpperInvariant() switch
        {
            "INF" or "INFORMATION" => InformationBrush,
            "WRN" or "WARNING" => WarningBrush,
            "ERR" or "ERROR" => ErrorBrush,
            "FTL" or "FATAL" => FatalBrush,
            "DBG" or "DEBUG" or "VRB" or "VERBOSE" => DebugBrush,
            _ => DefaultBrush
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
