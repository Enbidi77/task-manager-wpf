using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskManager.Application.Common;
using TaskManager.Domain.Models;

namespace TaskManager.Application.ViewModels.Processes;

public partial class ProcessesViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _searchFilter = string.Empty;

    [ObservableProperty]
    private ProcessModel? _selectedProcess;

    public ObservableCollection<ProcessModel> Processes { get; } = new();

    public ProcessesViewModel()
    {
        Title = "Processes";
        LoadSampleProcesses();
    }

    [RelayCommand]
    private void EndTask()
    {
        if (SelectedProcess != null)
        {
            Processes.Remove(SelectedProcess);
            SelectedProcess = null;
        }
    }

    [RelayCommand]
    private void Refresh()
    {
        LoadSampleProcesses();
    }

    private void LoadSampleProcesses()
    {
        Processes.Clear();
        Processes.Add(new ProcessModel(1048, "Windows Explorer") { CpuPercent = 1.2, WorkingSetBytes = 184549376, UserName = "SYSTEM", Description = "Windows Explorer" });
        Processes.Add(new ProcessModel(2044, "Task Manager") { CpuPercent = 2.4, WorkingSetBytes = 89128960, UserName = "User", Description = "Task Manager" });
        Processes.Add(new ProcessModel(3180, "Microsoft Edge") { CpuPercent = 5.8, WorkingSetBytes = 419430400, UserName = "User", Description = "Microsoft Edge Browser" });
        Processes.Add(new ProcessModel(4210, "Visual Studio Code") { CpuPercent = 3.1, WorkingSetBytes = 314572800, UserName = "User", Description = "Code Editor" });
        Processes.Add(new ProcessModel(5124, "System") { CpuPercent = 0.8, WorkingSetBytes = 41943040, UserName = "SYSTEM", Description = "NT Kernel & System" });
    }
}
