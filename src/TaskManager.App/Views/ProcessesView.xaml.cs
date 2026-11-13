using System.Windows;
using System.Windows.Controls;
using TaskManager.App.Dialogs;
using TaskManager.Application.ViewModels.Processes;

namespace TaskManager.App.Views;

public partial class ProcessesView : UserControl
{
    public ProcessesView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ProcessesViewModel vm)
        {
            vm.ConfirmActionAsync = (title, message) =>
            {
                var dialog = new ConfirmationDialog(title, message, "End Process")
                {
                    Owner = Window.GetWindow(this)
                };
                return Task.FromResult(dialog.ShowDialog() == true);
            };

            vm.SetClipboardText = text =>
            {
                try
                {
                    Clipboard.SetText(text);
                }
                catch
                {
                    // Clipboard access error ignored
                }
            };
        }
    }
}
