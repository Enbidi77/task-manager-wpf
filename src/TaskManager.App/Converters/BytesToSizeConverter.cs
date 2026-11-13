using System.Globalization;
using System.Windows.Data;

namespace TaskManager.App.Converters;

public class BytesToSizeConverter : IValueConverter
{
    private static readonly string[] SizeSuffixes = { "B", "KB", "MB", "GB", "TB", "PB" };

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return "0 B";

        double bytes = 0;
        if (value is long l) bytes = l;
        else if (value is int i) bytes = i;
        else if (value is double d) bytes = d;
        else if (value is ulong ul) bytes = ul;
        else return value.ToString() ?? string.Empty;

        if (bytes <= 0) return "0 B";

        int place = System.Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
        if (place < 0) place = 0;
        if (place >= SizeSuffixes.Length) place = SizeSuffixes.Length - 1;

        double num = bytes / Math.Pow(1024, place);
        return $"{num:F1} {SizeSuffixes[place]}";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
