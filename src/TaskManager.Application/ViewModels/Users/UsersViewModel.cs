using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using TaskManager.Application.Common;
using TaskManager.Domain.Models;

namespace TaskManager.Application.ViewModels.Users;

public partial class UsersViewModel : ViewModelBase
{
    [ObservableProperty]
    private UserSessionItem? _selectedUser;

    public ObservableCollection<UserSessionItem> UserSessions { get; } = new();

    public UsersViewModel()
    {
        Title = "Users";
        LoadSampleUsers();
    }

    private void LoadSampleUsers()
    {
        UserSessions.Clear();
        UserSessions.Add(new UserSessionItem(1) { UserName = Environment.UserName, SessionName = "Console", State = "Active", CpuPercent = 8.4, MemoryBytes = 4294967296, DiskBytesPerSec = 1048576, NetworkBytesPerSec = 524288 });
    }
}
