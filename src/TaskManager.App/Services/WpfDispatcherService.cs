using System.Windows;
using TaskManager.Application.Common.Interfaces;

namespace TaskManager.App.Services;

public class WpfDispatcherService : IDispatcherService
{
    public void Invoke(Action action)
    {
        var app = System.Windows.Application.Current;
        if (app != null && !app.Dispatcher.CheckAccess())
        {
            app.Dispatcher.Invoke(action);
        }
        else
        {
            action();
        }
    }

    public Task InvokeAsync(Action action)
    {
        var app = System.Windows.Application.Current;
        if (app != null && !app.Dispatcher.CheckAccess())
        {
            return app.Dispatcher.InvokeAsync(action).Task;
        }

        action();
        return Task.CompletedTask;
    }
}
