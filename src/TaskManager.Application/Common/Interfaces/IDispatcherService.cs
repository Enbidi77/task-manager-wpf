namespace TaskManager.Application.Common.Interfaces;

public interface IDispatcherService
{
    void Invoke(Action action);
    Task InvokeAsync(Action action);
}
