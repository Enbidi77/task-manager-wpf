using System.Windows;

namespace TaskManager.App.Dialogs;

public partial class ConfirmationDialog : Window
{
    public ConfirmationDialog(string title, string message, string confirmButtonText = "End Process")
    {
        InitializeComponent();
        TitleBlock.Text = title;
        MessageBlock.Text = message;
        ConfirmBtn.Content = confirmButtonText;
    }

    private void OnConfirmClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
