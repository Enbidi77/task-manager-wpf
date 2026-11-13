using System.Windows;
using System.Windows.Controls;

namespace TaskManager.App.Controls;

public partial class StatusIndicator : UserControl
{
    public static readonly DependencyProperty StatusTextProperty =
        DependencyProperty.Register(nameof(StatusText), typeof(string), typeof(StatusIndicator), new PropertyMetadata("Running"));

    public string StatusText
    {
        get => (string)GetValue(StatusTextProperty);
        set => SetValue(StatusTextProperty, value);
    }

    public StatusIndicator()
    {
        InitializeComponent();
    }
}
