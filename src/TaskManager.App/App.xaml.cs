using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using TaskManager.App.Navigation;
using TaskManager.App.Services;
using TaskManager.Application;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.Monitoring.Interfaces;
using TaskManager.Infrastructure;
using TaskManager.Infrastructure.Data;
using TaskManager.Shared.Enums;

namespace TaskManager.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private IHost? _host;

    public IHost Host => _host ?? throw new InvalidOperationException("Host has not been initialized.");

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Configure Serilog bootstrap logger
        var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        if (!Directory.Exists(logDir))
        {
            Directory.CreateDirectory(logDir);
        }

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                path: Path.Combine(logDir, "taskmanager-.log"),
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        try
        {
            Log.Information("Starting Task Manager application host initialization...");

            _host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(e.Args)
                .UseSerilog()
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                          .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                          .AddEnvironmentVariables();
                })
                .ConfigureServices((context, services) =>
                {
                    // Clean Architecture Project DI layers
                    services.AddApplication();
                    services.AddInfrastructure(context.Configuration);

                    // App services
                    services.AddSingleton<IDispatcherService, WpfDispatcherService>();
                    services.AddSingleton<INavigationService, NavigationService>();
                    services.AddSingleton<IThemeService, ThemeService>();

                    // UI Windows
                    services.AddSingleton<MainWindow>();
                })
                .Build();

            await _host.StartAsync();
            Log.Information("Host successfully started.");

            // Initialize SQLite Database schema
            using (var scope = _host.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<TaskManagerDbContext>();
                dbContext.Database.EnsureCreated();
                Log.Information("SQLite Database verified/ensured created.");
            }

            // Load saved settings and apply
            var settingsService = _host.Services.GetRequiredService<ISettingsService>();
            var settings = await settingsService.LoadSettingsAsync();

            var themeService = _host.Services.GetRequiredService<IThemeService>();
            if (Enum.TryParse<AppTheme>(settings.Theme, true, out var theme))
            {
                themeService.SetTheme(theme);
            }
            else
            {
                themeService.SetTheme(AppTheme.Dark);
            }

            var coordinator = _host.Services.GetRequiredService<IMonitoringCoordinator>();
            coordinator.Interval = TimeSpan.FromMilliseconds(settings.RefreshIntervalMs);

            // Navigate to initial view
            var navigationService = _host.Services.GetRequiredService<INavigationService>();
            navigationService.NavigateTo(NavigationPage.Dashboard);

            // Display MainWindow via DI
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Topmost = settings.AlwaysOnTop;

            settingsService.SettingsChanged += updated =>
            {
                Dispatcher.Invoke(() =>
                {
                    mainWindow.Topmost = updated.AlwaysOnTop;
                });
            };

            if (settings.StartMinimized)
            {
                mainWindow.WindowState = WindowState.Minimized;
            }
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application failed during startup.");
            System.Windows.MessageBox.Show(
                $"Failed to start application:\n{ex.Message}",
                "Fatal Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        Log.Information("Application exiting, disposing host...");

        if (_host != null)
        {
            try
            {
                await _host.StopAsync(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error while stopping application host.");
            }
            finally
            {
                _host.Dispose();
                _host = null;
            }
        }

        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
