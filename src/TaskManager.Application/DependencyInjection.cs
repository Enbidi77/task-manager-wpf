using Microsoft.Extensions.DependencyInjection;
using TaskManager.Application.ViewModels.Dashboard;
using TaskManager.Application.ViewModels.Performance;
using TaskManager.Application.ViewModels.Processes;
using TaskManager.Application.ViewModels.Services;
using TaskManager.Application.ViewModels.Settings;
using TaskManager.Application.ViewModels.Shell;
using TaskManager.Application.ViewModels.StartupApps;
using TaskManager.Application.ViewModels.Users;

namespace TaskManager.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // ViewModels
        services.AddSingleton<ShellViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<ProcessesViewModel>();
        services.AddTransient<PerformanceViewModel>();
        services.AddTransient<ServicesViewModel>();
        services.AddTransient<StartupAppsViewModel>();
        services.AddTransient<UsersViewModel>();
        services.AddTransient<SettingsViewModel>();

        return services;
    }
}
