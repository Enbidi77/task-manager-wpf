using FluentAssertions;
using TaskManager.Domain.Models;
using Xunit;

namespace TaskManager.Domain.Tests;

public class ProcessModelTests
{
    [Fact]
    public void ProcessModel_Initialization_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var process = new ProcessModel(42, "explorer")
        {
            CpuPercent = 2.5,
            WorkingSetBytes = 1048576,
            UserName = "SYSTEM",
            Status = "Running",
            Description = "Windows Explorer"
        };

        // Assert
        process.Id.Should().Be(42);
        process.Name.Should().Be("explorer");
        process.CpuPercent.Should().Be(2.5);
        process.WorkingSetBytes.Should().Be(1048576);
        process.UserName.Should().Be("SYSTEM");
        process.Status.Should().Be("Running");
        process.Description.Should().Be("Windows Explorer");
    }

    [Fact]
    public void ProcessModel_Equality_BasedOnId()
    {
        // Arrange
        var p1 = new ProcessModel(100, "procA");
        var p2 = new ProcessModel(100, "procB");
        var p3 = new ProcessModel(200, "procA");

        // Assert
        p1.Should().Be(p2);
        p1.Should().NotBe(p3);
    }
}
