using System.Xml.Linq;
using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
// Use static import for a more readable fluent API
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace FileIt.Module.SimpleFlow.Test;

[TestClass]
public class ArchitectureTests
{
    // Load your architecture once for all tests to improve performance
    private static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(
            typeof(FileIt.Domain.Interfaces.IAuditable).Assembly,
            typeof(FileIt.Infrastructure.IInfrastructureConfig).Assembly,
            typeof(FileIt.Module.SimpleFlow.App.SimpleConfig).Assembly,
            typeof(FileIt.Module.SimpleFlow.Host.Health).Assembly
        )
        .Build();

    [TestMethod]
    public void Domain_Should_Not_Have_Dependency_On_Infrastructure()
    {
        var testLayer = Types().That().HaveFullNameContaining("FileIt.Domain.");
        var targetLayer = Types().That().HaveFullNameContaining("FileIt.Infrastructure.");

        // Define the architectural rule
        var rule = Types().That().Are(testLayer).Should().NotDependOnAny(targetLayer);

        bool isValid = rule.HasNoViolations(Architecture);
        Assert.IsTrue(isValid);
    }

    [TestMethod]
    public void Domain_Should_Not_Have_Dependency_On_Host()
    {
        var testLayer = Types().That().HaveFullNameContaining("FileIt.Domain.");
        var targetLayer = Types().That().HaveFullNameContaining("FileIt.Module.SimpleFlow.Host.");

        // Define the architectural rule
        var rule = Types().That().Are(testLayer).Should().NotDependOnAny(targetLayer);

        bool isValid = rule.HasNoViolations(Architecture);
        Assert.IsTrue(isValid);
    }

    [TestMethod]
    public void Domain_Should_Not_Have_Dependency_On_App()
    {
        var testLayer = Types().That().HaveFullNameContaining("FileIt.Domain.");
        var targetLayer = Types().That().HaveFullNameContaining("FileIt.Module.SimpleFlow.App.");

        // Define the architectural rule
        var rule = Types().That().Are(testLayer).Should().NotDependOnAny(targetLayer);

        bool isValid = rule.HasNoViolations(Architecture);
        Assert.IsTrue(isValid);
    }

    [TestMethod]
    public void Host_Should_Not_Have_Dependency_On_Domain()
    {
        // SimpleFlowDeadLetterReader is the boundary between Host and Infrastructure;
        // it legitimately uses Domain.Entities.DeadLetter.SourceEntityType to construct
        // a DeadLetterIngestionEnvelope for the Infrastructure service.
        var targetLayer = Types().That().HaveFullNameContaining("FileIt.Domain.");
        var testLayer = Types()
            .That()
            .HaveFullNameContaining("FileIt.Module.SimpleFlow.Host.")
            .And()
            .DoNotHaveFullNameContaining("SimpleFlowDeadLetterReader");

        var rule = Types().That().Are(testLayer).Should().NotDependOnAny(targetLayer);

        Assert.IsTrue(rule.HasNoViolations(Architecture));
    }

    [TestMethod]
    public void App_Should_Not_Have_Dependency_On_Infrastructure()
    {
        var testLayer = Types().That().HaveFullNameContaining("FileIt.Module.SimpleFlow.App.");
        var targetLayer = Types().That().HaveFullNameContaining("FileIt.Infrastructure.");

        // Define the architectural rule
        var rule = Types().That().Are(testLayer).Should().NotDependOnAny(targetLayer);

        bool isValid = rule.HasNoViolations(Architecture);
        Assert.IsTrue(isValid);
    }
}
