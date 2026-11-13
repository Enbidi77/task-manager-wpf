# Development Guide

## 1. Environment Setup

### Required Tools
- **SDK**: .NET 10.0 LTS SDK (x64)
- **IDE**: Visual Studio 2026 (with .NET desktop development workload), JetBrains Rider 2026+, or Visual Studio Code with C# Dev Kit
- **OS**: Windows 10 (Build 1809+) or Windows 11

### Repository Clone & Build
```powershell
git clone https://github.com/organization/task-manager-wpf.git
cd task-manager-wpf

# Restore dependencies
dotnet restore TaskManager.sln

# Build entire solution with warnings treated as errors
dotnet build TaskManager.sln -c Debug --no-incremental /warnaserror
```

---

## 2. Solution Structure & Standards

### Coding Conventions
- **C# Language Version**: C# 13 / .NET 10.
- **Nullable Reference Types**: `<Nullable>enable</Nullable>` is enforced on all projects. No warnings are permitted.
- **MVVM Framework**: Use CommunityToolkit.Mvvm source generators (`[ObservableProperty]`, `[RelayCommand]`).
- **Clean Architecture Purity**:
  - `TaskManager.Domain`: Pure C# objects, no UI, no external framework dependencies.
  - `TaskManager.Application`: Contains business logic, interfaces, view models, and DTOs.
  - `TaskManager.Infrastructure`: Implements platform and persistence interfaces.
  - `TaskManager.App`: Only presentation concerns, controls, styles, and startup bootstrap.

---

## 3. How to Extend the Application

### Adding a New Hardware Telemetry Monitor

1. **Define the Snapshot Model** in `TaskManager.Application/Monitoring/Models/MonitoringSnapshots.cs`:
   ```csharp
   public record BatterySnapshot(
       double ChargePercent,
       bool IsPluggedIn,
       TimeSpan EstimatedRemaining,
       DateTime Timestamp);
   ```

2. **Define the Monitor Interface** in `TaskManager.Application/Monitoring/Interfaces/MonitoringInterfaces.cs`:
   ```csharp
   public interface IBatteryMonitor
   {
       ValueTask<BatterySnapshot?> GetSnapshotAsync(CancellationToken cancellationToken = default);
   }
   ```

3. **Implement the Concrete Monitor** in `TaskManager.Infrastructure/Monitoring/WindowsBatteryMonitor.cs`:
   ```csharp
   [SupportedOSPlatform("windows")]
   public class WindowsBatteryMonitor : IBatteryMonitor
   {
       public ValueTask<BatterySnapshot?> GetSnapshotAsync(CancellationToken cancellationToken = default)
       {
           var status = System.Windows.Forms.SystemInformation.PowerStatus;
           // Query power status...
           return ValueTask.FromResult<BatterySnapshot?>(snapshot);
       }
   }
   ```

4. **Register in DI** in `TaskManager.Infrastructure/DependencyInjection.cs`:
   ```csharp
   services.AddSingleton<IBatteryMonitor, WindowsBatteryMonitor>();
   ```

5. **Consume in ViewModel** or `MonitoringCoordinator`.

---

### Adding a New Page to the Navigation Shell

1. **Add enum member** to `TaskManager.Shared/Enums/NavigationPage.cs`:
   ```csharp
   public enum NavigationPage
   {
       Dashboard,
       Processes,
       Performance,
       Services,
       StartupApps,
       Users,
       Settings,
       NewFeature // <-- Add here
   }
   ```

2. **Create ViewModel** in `TaskManager.Application/ViewModels/NewFeature/NewFeatureViewModel.cs`:
   ```csharp
   public partial class NewFeatureViewModel : ViewModelBase
   {
       public NewFeatureViewModel()
       {
           Title = "New Feature";
       }
   }
   ```

3. **Create WPF View** in `TaskManager.App/Views/NewFeatureView.xaml`:
   ```xaml
   <UserControl x:Class="TaskManager.App.Views.NewFeatureView"
                xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
       <Grid Margin="24">
           <TextBlock Text="New Feature Content" FontSize="20" Foreground="{DynamicResource TextPrimaryBrush}"/>
       </Grid>
   </UserControl>
   ```

4. **Register DataTemplate** in `TaskManager.App/App.xaml`:
   ```xaml
   <DataTemplate DataType="{x:Type vmNew:NewFeatureViewModel}">
       <views:NewFeatureView />
   </DataTemplate>
   ```

5. **Register Navigation Item** in `ShellViewModel.cs`:
   ```csharp
   NavigationItems.Add(new NavigationItem(
       NavigationPage.NewFeature,
       "New Feature",
       "\uE7C5")); // Segoe MDL2 icon glyph
   ```

---

## 4. Testing Guidelines

- **Unit Tests**:
  - `TaskManager.Domain.Tests`: Test domain models, state logic, calculation helpers.
  - `TaskManager.Application.Tests`: Test ViewModels using `ImmediateDispatcherService` and Moq.
  - `TaskManager.Infrastructure.Tests`: Test repositories with in-memory SQLite (`Data Source=:memory:`), and monitors with mocks.
  - `TaskManager.App.Tests`: Test converters and navigation service.
- **Run Tests**:
  ```powershell
  dotnet test TaskManager.sln
  ```
