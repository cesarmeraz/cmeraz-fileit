using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
// Use static import for a more readable fluent API
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace FileIt.Module.Services.Test;

[TestClass]
public class ArchitectureTests
{
    // Load your architecture once for all tests to improve performance
    private static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(
            typeof(FileIt.Domain.Interfaces.IAuditable).Assembly,
            typeof(FileIt.Infrastructure.IInfrastructureConfig).Assembly,
            typeof(FileIt.Module.Services.App.ServicesConfig).Assembly,
            typeof(FileIt.Module.Services.Host.Health).Assembly
        )
        .Build();

    [TestMethod]
    public void Domain_Should_Not_Have_Dependency_On_Infrastructure()
    {
        var testLayer = Types().That().HaveFullNameContaining("FileIt.Domain.");
        var targetLayer = Types().That().HaveFullNameContaining("FileIt.Infrastructure.");

        var rule = Types().That().Are(testLayer).Should().NotDependOnAny(targetLayer);

        Assert.IsTrue(rule.HasNoViolations(Architecture));
    }

    [TestMethod]
    public void Domain_Should_Not_Have_Dependency_On_Host()
    {
        var testLayer = Types().That().HaveFullNameContaining("FileIt.Domain.");
        var targetLayer = Types().That().HaveFullNameContaining("FileIt.Module.Services.Host.");

        var rule = Types().That().Are(testLayer).Should().NotDependOnAny(targetLayer);

        Assert.IsTrue(rule.HasNoViolations(Architecture));
    }

    [TestMethod]
    public void Domain_Should_Not_Have_Dependency_On_App()
    {
        var testLayer = Types().That().HaveFullNameContaining("FileIt.Domain.");
        var targetLayer = Types().That().HaveFullNameContaining("FileIt.Module.Services.App.");

        var rule = Types().That().Are(testLayer).Should().NotDependOnAny(targetLayer);

        Assert.IsTrue(rule.HasNoViolations(Architecture));
    }

    [TestMethod]
    public void Host_Should_Not_Have_Dependency_On_Domain()
    {
        var targetLayer = Types().That().HaveFullNameContaining("FileIt.Domain.");
        var testLayer = Types().That().HaveFullNameContaining("FileIt.Module.Services.Host.");

        var rule = Types().That().Are(testLayer).Should().NotDependOnAny(targetLayer);

        Assert.IsTrue(rule.HasNoViolations(Architecture));
    }

    [TestMethod]
    public void App_Should_Not_Have_Dependency_On_Infrastructure()
    {
        var testLayer = Types().That().HaveFullNameContaining("FileIt.Module.Services.App.");
        var targetLayer = Types().That().HaveFullNameContaining("FileIt.Infrastructure.");

        var rule = Types().That().Are(testLayer).Should().NotDependOnAny(targetLayer);

        Assert.IsTrue(rule.HasNoViolations(Architecture));
    }
}
