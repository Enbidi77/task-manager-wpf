using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.Monitoring.Interfaces;
using TaskManager.Infrastructure.Data;
using TaskManager.Infrastructure.Monitoring;
using TaskManager.Infrastructure.Persistence;
using TaskManager.Infrastructure.Services;

namespace TaskManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString) || connectionString == "Data Source=taskmanager.db")
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dbDirectory = Path.Combine(appData, "TaskManager");
            if (!Directory.Exists(dbDirectory))
            {
                Directory.CreateDirectory(dbDirectory);
            }
            connectionString = $"Data Source={Path.Combine(dbDirectory, "taskmanager.db")}";
        }

        services.AddDbContext<TaskManagerDbContext>(options =>
            options.UseSqlite(connectionString));

        // Repositories
        services.AddScoped<ISettingsRepository, SettingsRepository>();
        services.AddScoped<IMetricHistoryRepository, MetricHistoryRepository>();
        services.AddScoped<IProcessFilterRepository, ProcessFilterRepository>();

        // Services
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IDiagnosticLogService, DiagnosticLogService>();
        services.AddSingleton<SystemDiagnosticsService>();

        // Phase 2: Monitoring Infrastructure Providers
        services.AddSingleton<ICpuMonitor, WindowsCpuMonitor>();
        services.AddSingleton<IMemoryMonitor, WindowsMemoryMonitor>();
        services.AddSingleton<IDiskMonitor, WindowsDiskMonitor>();
        services.AddSingleton<INetworkMonitor, WindowsNetworkMonitor>();
        services.AddSingleton<IGpuMonitor, WindowsGpuMonitor>();
        services.AddSingleton<IProcessMonitor, WindowsProcessMonitor>();
        services.AddSingleton<IServiceMonitor, WindowsServiceMonitor>();
        services.AddStartupAppMonitor();
        services.AddSingleton<IUserSessionMonitor, WindowsUserSessionMonitor>();

        // Centralized Monitoring Coordinator
        services.AddSingleton<MonitoringCoordinator>(sp => new MonitoringCoordinator(
            sp.GetRequiredService<ICpuMonitor>(),
            sp.GetRequiredService<IMemoryMonitor>(),
            sp.GetRequiredService<IDiskMonitor>(),
            sp.GetRequiredService<INetworkMonitor>(),
            sp.GetRequiredService<IGpuMonitor>(),
            sp.GetRequiredService<IProcessMonitor>(),
            sp.GetRequiredService<ILogger<MonitoringCoordinator>>(),
            sp.GetRequiredService<IServiceScopeFactory>()));

        services.AddSingleton<IMonitoringCoordinator>(sp => sp.GetRequiredService<MonitoringCoordinator>());
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<MonitoringCoordinator>());

        return services;
    }

    private static void AddStartupAppMonitor(this IServiceCollection services)
    {
        services.AddSingleton<IStartupApplicationMonitor, WindowsStartupApplicationMonitor>();
    }
}
