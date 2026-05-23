using System.Linq;
using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Fluent.Extensions;
using ArchUnitNET.Loader;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace FileIt.Module.FileItModule.Test;

[TestClass]
public class ArchitectureTests
{
    private static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(
            typeof(FileIt.Domain.Interfaces.IAuditable).Assembly,
            typeof(FileIt.Infrastructure.IInfrastructureConfig).Assembly,
            typeof(FileIt.Module.FileItModule.App.FileItModuleConfig).Assembly,
            typeof(FileIt.Module.FileItModule.Health).Assembly
        )
        .Build();

    [TestMethod]
    public void Domain_Should_Not_Have_Dependency_On_Infrastructure()
    {
        var testLayer = Types().That().HaveFullNameContaining("FileIt.Domain.");
        var targetLayer = Types().That().HaveFullNameContaining("FileIt.Infrastructure.");

        var rule = Types().That().Are(testLayer).Should().NotDependOnAny(targetLayer);

        AssertNoViolations(rule, nameof(Domain_Should_Not_Have_Dependency_On_Infrastructure));
    }

    [TestMethod]
    public void Domain_Should_Not_Have_Dependency_On_Host()
    {
        var testLayer = Types().That().HaveFullNameContaining("FileIt.Domain.");
        var targetLayer = Types().That().HaveFullNameContaining("FileIt.Module.FileItModule.");

        var rule = Types().That().Are(testLayer).Should().NotDependOnAny(targetLayer);

        AssertNoViolations(rule, nameof(Domain_Should_Not_Have_Dependency_On_Host));
    }

    [TestMethod]
    public void Domain_Should_Not_Have_Dependency_On_App()
    {
        var testLayer = Types().That().HaveFullNameContaining("FileIt.Domain.");
        var targetLayer = Types().That().HaveFullNameContaining("FileIt.Module.FileItModule.App.");

        var rule = Types().That().Are(testLayer).Should().NotDependOnAny(targetLayer);

        AssertNoViolations(rule, nameof(Domain_Should_Not_Have_Dependency_On_App));
    }

    [TestMethod]
    public void Host_Should_Not_Have_Dependency_On_Domain()
    {
        var targetLayer = Types().That().HaveFullNameContaining("FileIt.Domain.");
        var testLayer = Types()
            .That()
            .HaveFullNameContaining("FileIt.Module.FileItModule.")
            .And()
            .DoNotHaveFullNameContaining("FileIt.Module.FileItModule.App.");

        var rule = Types().That().Are(testLayer).Should().NotDependOnAny(targetLayer);

        AssertNoViolations(rule, nameof(Host_Should_Not_Have_Dependency_On_Domain));
    }

    [TestMethod]
    public void App_Should_Not_Have_Dependency_On_Infrastructure()
    {
        var testLayer = Types().That().HaveFullNameContaining("FileIt.Module.FileItModule.App.");
        var targetLayer = Types().That().HaveFullNameContaining("FileIt.Infrastructure.");

        var rule = Types().That().Are(testLayer).Should().NotDependOnAny(targetLayer);

        AssertNoViolations(rule, nameof(App_Should_Not_Have_Dependency_On_Infrastructure));
    }

    private static void AssertNoViolations(IArchRule rule, string ruleName)
    {
        var results = rule.Evaluate(Architecture).ToList();
        if (results.Any(result => !result.Passed))
        {
            var details = results.ToErrorMessage();
            Assert.Fail(
                $"Architecture rule failed: {ruleName}{Environment.NewLine}{Environment.NewLine}{details}"
            );
        }
    }
}
