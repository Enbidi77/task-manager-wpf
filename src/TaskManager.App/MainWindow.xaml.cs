using System.Windows;
using TaskManager.Application.ViewModels.Shell;

namespace TaskManager.App;

public partial class MainWindow : Window
{
    public MainWindow(ShellViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
