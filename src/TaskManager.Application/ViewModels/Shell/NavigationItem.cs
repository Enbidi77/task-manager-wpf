using CommunityToolkit.Mvvm.ComponentModel;
using TaskManager.Shared.Enums;

namespace TaskManager.Application.ViewModels.Shell;

public partial class NavigationItem : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _iconGlyph = string.Empty;

    [ObservableProperty]
    private NavigationPage _page;

    [ObservableProperty]
    private bool _isSelected;

    public NavigationItem(string title, string iconGlyph, NavigationPage page)
    {
        Title = title;
        IconGlyph = iconGlyph;
        Page = page;
    }
}
