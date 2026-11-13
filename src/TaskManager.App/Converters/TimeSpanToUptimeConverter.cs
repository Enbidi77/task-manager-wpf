using System.Globalization;
using System.Windows.Data;

namespace TaskManager.App.Converters;

public class TimeSpanToUptimeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TimeSpan ts)
        {
            if (ts.TotalDays >= 1)
            {
                return $"{(int)ts.TotalDays}d {ts.Hours:D2}h {ts.Minutes:D2}m {ts.Seconds:D2}s";
            }
            return $"{ts.Hours:D2}h {ts.Minutes:D2}m {ts.Seconds:D2}s";
        }

        return "00h 00m 00s";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
