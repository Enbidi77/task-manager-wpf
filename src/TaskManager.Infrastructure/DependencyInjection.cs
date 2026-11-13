using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TaskManager.Application.Monitoring.Interfaces;
using TaskManager.Infrastructure.Data;
using TaskManager.Infrastructure.Monitoring;
using TaskManager.Infrastructure.Services;

namespace TaskManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Data Source=taskmanager.db";

        services.AddDbContext<TaskManagerDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddSingleton<SystemDiagnosticsService>();

        // Phase 2: Monitoring Infrastructure Providers
        services.AddSingleton<ICpuMonitor, WindowsCpuMonitor>();
        services.AddSingleton<IMemoryMonitor, WindowsMemoryMonitor>();
        services.AddSingleton<IDiskMonitor, WindowsDiskMonitor>();
        services.AddSingleton<INetworkMonitor, WindowsNetworkMonitor>();
        services.AddSingleton<IGpuMonitor, WindowsGpuMonitor>();
        services.AddSingleton<IProcessMonitor, WindowsProcessMonitor>();
        services.AddSingleton<IServiceMonitor, WindowsServiceMonitor>();
        services.AddSingleton<IStartupApplicationMonitor, WindowsStartupApplicationMonitor>();
        services.AddSingleton<IUserSessionMonitor, WindowsUserSessionMonitor>();

        // Centralized Monitoring Coordinator
        services.AddSingleton<MonitoringCoordinator>();
        services.AddSingleton<IMonitoringCoordinator>(sp => sp.GetRequiredService<MonitoringCoordinator>());
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<MonitoringCoordinator>());

        return services;
    }
}
