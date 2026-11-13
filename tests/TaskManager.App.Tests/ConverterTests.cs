using System.Globalization;
using System.Windows;
using FluentAssertions;
using TaskManager.App.Converters;
using Xunit;

namespace TaskManager.App.Tests;

public class ConverterTests
{
    [Theory]
    [InlineData(0L, "0 B")]
    [InlineData(512L, "512.0 B")]
    [InlineData(1024L, "1.0 KB")]
    [InlineData(1048576L, "1.0 MB")]
    [InlineData(1073741824L, "1.0 GB")]
    [InlineData(1099511627776L, "1.0 TB")]
    public void BytesToSizeConverter_FormatsBytesCorrectly(long bytes, string expected)
    {
        var converter = new BytesToSizeConverter();
        var result = converter.Convert(bytes, typeof(string), null, CultureInfo.InvariantCulture);
        result.Should().Be(expected);
    }

    [Fact]
    public void PercentageToColorConverter_ReturnsAppropriateBrush_BasedOnThreshold()
    {
        var converter = new PercentageToColorConverter();

        var low = converter.Convert(25.0, typeof(object), null, CultureInfo.InvariantCulture);
        low.Should().Be(converter.LowBrush);

        var med = converter.Convert(70.0, typeof(object), null, CultureInfo.InvariantCulture);
        med.Should().Be(converter.MediumBrush);

        var high = converter.Convert(92.0, typeof(object), null, CultureInfo.InvariantCulture);
        high.Should().Be(converter.HighBrush);
    }

    [Fact]
    public void TimeSpanToUptimeConverter_FormatsDaysHoursMinutes()
    {
        var converter = new TimeSpanToUptimeConverter();
        var ts = new TimeSpan(2, 4, 30, 15);
        var result = converter.Convert(ts, typeof(string), null, CultureInfo.InvariantCulture);
        result.Should().Be("2d 04h 30m 15s");
    }

    [Fact]
    public void BooleanToVisibilityConverter_ConvertsCorrectly()
    {
        var converter = new BooleanToVisibilityConverter();
        converter.Convert(true, typeof(Visibility), null, CultureInfo.InvariantCulture)
            .Should().Be(Visibility.Visible);
        converter.Convert(false, typeof(Visibility), null, CultureInfo.InvariantCulture)
            .Should().Be(Visibility.Collapsed);

        converter.Invert = true;
        converter.Convert(true, typeof(Visibility), null, CultureInfo.InvariantCulture)
            .Should().Be(Visibility.Collapsed);
    }
}
