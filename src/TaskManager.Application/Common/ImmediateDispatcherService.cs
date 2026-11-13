using TaskManager.Application.Common.Interfaces;

namespace TaskManager.Application.Common;

public class ImmediateDispatcherService : IDispatcherService
{
    public void Invoke(Action action) => action();
    public Task InvokeAsync(Action action)
    {
        action();
        return Task.CompletedTask;
    }
}
