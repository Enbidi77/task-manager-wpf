using System.Windows;
using System.Windows.Controls;
using TaskManager.Application.ViewModels.Settings;

namespace TaskManager.App.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private void OnGeneralTabClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
        {
            vm.SelectedTabIndex = 0;
        }
    }

    private void OnDiagnosticsTabClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
        {
            vm.SelectedTabIndex = 1;
        }
    }
}
