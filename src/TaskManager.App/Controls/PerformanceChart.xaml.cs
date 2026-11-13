using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TaskManager.App.Controls;

public partial class PerformanceChart : UserControl
{
    public static readonly DependencyProperty ValuesProperty =
        DependencyProperty.Register(nameof(Values), typeof(IEnumerable<double>), typeof(PerformanceChart),
            new PropertyMetadata(null, OnValuesChanged));

    public static readonly DependencyProperty StrokeBrushProperty =
        DependencyProperty.Register(nameof(StrokeBrush), typeof(Brush), typeof(PerformanceChart),
            new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x60, 0xCD, 0xFF)), OnBrushChanged));

    public static readonly DependencyProperty HeaderTextProperty =
        DependencyProperty.Register(nameof(HeaderText), typeof(string), typeof(PerformanceChart),
            new PropertyMetadata("Utilization (60 seconds)"));

    public IEnumerable<double>? Values
    {
        get => (IEnumerable<double>?)GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    public Brush StrokeBrush
    {
        get => (Brush)GetValue(StrokeBrushProperty);
        set => SetValue(StrokeBrushProperty, value);
    }

    public string HeaderText
    {
        get => (string)GetValue(HeaderTextProperty);
        set => SetValue(HeaderTextProperty, value);
    }

    public PerformanceChart()
    {
        InitializeComponent();
        SizeChanged += (_, _) => Redraw();
    }

    private static void OnValuesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PerformanceChart chart)
        {
            if (e.OldValue is INotifyCollectionChanged oldColl)
                oldColl.CollectionChanged -= chart.OnCollectionChanged;

            if (e.NewValue is INotifyCollectionChanged newColl)
                newColl.CollectionChanged += chart.OnCollectionChanged;

            chart.Redraw();
        }
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Redraw();
    }

    private static void OnBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PerformanceChart chart)
        {
            chart.UpdateBrushes();
        }
    }

    private void UpdateBrushes()
    {
        LinePath.Stroke = StrokeBrush;

        if (StrokeBrush is SolidColorBrush scb)
        {
            var grad = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1)
            };
            grad.GradientStops.Add(new GradientStop(scb.Color, 0.0));
            grad.GradientStops.Add(new GradientStop(Color.FromArgb(10, scb.Color.R, scb.Color.G, scb.Color.B), 1.0));
            AreaPath.Fill = grad;
        }
        else
        {
            AreaPath.Fill = StrokeBrush;
        }
    }

    private void Redraw()
    {
        UpdateBrushes();

        double width = ActualWidth;
        double height = ActualHeight;
        if (width <= 10 || height <= 10) return;

        var pointsList = Values?.ToList();
        if (pointsList == null || pointsList.Count < 2)
        {
            LinePath.Data = null;
            AreaPath.Data = null;
            return;
        }

        int count = pointsList.Count;
        double stepX = width / (count - 1);

        var lineGeometry = new StreamGeometry();
        var areaGeometry = new StreamGeometry();

        using (var ctx = lineGeometry.Open())
        {
            double firstY = height - (Math.Clamp(pointsList[0], 0, 100) / 100.0 * height);
            ctx.BeginFigure(new Point(0, firstY), isFilled: false, isClosed: false);

            for (int i = 1; i < count; i++)
            {
                double x = i * stepX;
                double y = height - (Math.Clamp(pointsList[i], 0, 100) / 100.0 * height);
                ctx.LineTo(new Point(x, y), isStroked: true, isSmoothJoin: true);
            }
        }

        using (var ctx = areaGeometry.Open())
        {
            double firstY = height - (Math.Clamp(pointsList[0], 0, 100) / 100.0 * height);
            ctx.BeginFigure(new Point(0, height), isFilled: true, isClosed: true);
            ctx.LineTo(new Point(0, firstY), isStroked: false, isSmoothJoin: false);

            for (int i = 1; i < count; i++)
            {
                double x = i * stepX;
                double y = height - (Math.Clamp(pointsList[i], 0, 100) / 100.0 * height);
                ctx.LineTo(new Point(x, y), isStroked: false, isSmoothJoin: true);
            }

            ctx.LineTo(new Point(width, height), isStroked: false, isSmoothJoin: false);
        }

        lineGeometry.Freeze();
        areaGeometry.Freeze();

        LinePath.Data = lineGeometry;
        AreaPath.Data = areaGeometry;
    }
}
