using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskManager.Application.Common;
using TaskManager.Application.Monitoring.Interfaces;

namespace TaskManager.Application.ViewModels.Users;

public partial class UsersViewModel : ViewModelBase
{
    private readonly IUserSessionMonitor _userSessionMonitor;

    [ObservableProperty]
    private UserSessionSnapshot? _selectedUser;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public ObservableCollection<UserSessionSnapshot> UserSessions { get; } = new();

    public Action<string>? ShowNotification { get; set; }

    public UsersViewModel(IUserSessionMonitor userSessionMonitor)
    {
        _userSessionMonitor = userSessionMonitor;
        Title = "Users";
    }

    public override async Task InitializeAsync()
    {
        await LoadUserSessionsAsync();
    }

    [RelayCommand]
    public async Task LoadUserSessionsAsync()
    {
        IsBusy = true;
        try
        {
            var sessions = await _userSessionMonitor.GetSnapshotAsync();
            UserSessions.Clear();
            foreach (var session in sessions)
            {
                UserSessions.Add(session);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load user sessions: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DisconnectSessionAsync()
    {
        if (SelectedUser == null) return;

        int sessionId = SelectedUser.SessionId;
        string userName = SelectedUser.UserName;

        StatusMessage = $"Disconnecting session {sessionId} ({userName})...";
        bool success = await _userSessionMonitor.DisconnectSessionAsync(sessionId);
        if (success)
        {
            StatusMessage = $"Session {sessionId} disconnected.";
            ShowNotification?.Invoke(StatusMessage);
            await LoadUserSessionsAsync();
        }
        else
        {
            StatusMessage = $"Failed to disconnect session {sessionId}.";
            ShowNotification?.Invoke(StatusMessage);
        }
    }
}
