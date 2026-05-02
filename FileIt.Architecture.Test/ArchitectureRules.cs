using System.Reflection;
using NetArchTest.Rules;
using TestResult = NetArchTest.Rules.TestResult;

namespace FileIt.Architecture.Test;

/// <summary>
/// Architectural rules that fail the build if anyone violates the layering
/// or module-isolation contracts. Run as part of the test suite. The rules
/// are intentionally narrow and strict: every failure points to a specific
/// type, so violations are easy to fix.
/// </summary>
[TestClass]
public class ArchitectureRules
{
    private static Assembly LoadByName(string name)
    {
        return Assembly.Load(name);
    }

    private static readonly Assembly DomainAsm       = LoadByName("FileIt.Domain");
    private static readonly Assembly InfraAsm        = LoadByName("FileIt.Infrastructure");
    private static readonly Assembly ServicesAppAsm  = LoadByName("FileIt.Module.Services.App");
    private static readonly Assembly ServicesHostAsm = LoadByName("FileIt.Module.Services.Host");
    private static readonly Assembly SimpleAppAsm    = LoadByName("FileIt.Module.SimpleFlow.App");
    private static readonly Assembly SimpleHostAsm   = LoadByName("FileIt.Module.SimpleFlow.Host");
    private static readonly Assembly DataFlowAppAsm  = LoadByName("FileIt.Module.DataFlow.App");
    private static readonly Assembly DataFlowHostAsm = LoadByName("FileIt.Module.DataFlow.Host");
    private static readonly Assembly ComplexAppAsm   = LoadByName("FileIt.Module.Complex.App");
    private static readonly Assembly ComplexHostAsm  = LoadByName("FileIt.Module.Complex.Host");

    private static IEnumerable<Assembly> AllAppAssemblies() =>
        new[] { ServicesAppAsm, SimpleAppAsm, DataFlowAppAsm, ComplexAppAsm };

    private static IEnumerable<Assembly> AllHostAssemblies() =>
        new[] { ServicesHostAsm, SimpleHostAsm, DataFlowHostAsm, ComplexHostAsm };

    private static void AssertPass(TestResult result, string ruleDescription)
    {
        if (!result.IsSuccessful)
        {
            var failingTypes = result.FailingTypeNames ?? Array.Empty<string>();
            Assert.Fail(
                $"Architecture rule failed: {ruleDescription}\n" +
                $"Offending types:\n  {string.Join("\n  ", failingTypes)}");
        }
    }

    // ---- Dependency direction ---------------------------------------------

    [TestMethod]
    public void Rule01_DomainHasNoInfrastructureDependency()
    {
        var result = Types.InAssembly(DomainAsm)
            .Should()
            .NotHaveDependencyOn("FileIt.Infrastructure")
            .GetResult();
        AssertPass(result, "Domain must not depend on Infrastructure namespaces");
    }

    [TestMethod]
    public void Rule02_DomainHasNoModuleDependency()
    {
        var result = Types.InAssembly(DomainAsm)
            .Should()
            .NotHaveDependencyOnAny(
                "FileIt.Module.Services",
                "FileIt.Module.SimpleFlow",
                "FileIt.Module.DataFlow",
                "FileIt.Module.Complex")
            .GetResult();
        AssertPass(result, "Domain must not depend on any Module");
    }

    [TestMethod]
    public void Rule03_DomainHasNoAzureOrEFDependency()
    {
        var result = Types.InAssembly(DomainAsm)
            .Should()
            .NotHaveDependencyOnAny(
                "Azure.Messaging.ServiceBus",
                "Azure.Storage.Blobs",
                "Microsoft.EntityFrameworkCore",
                "System.Net.Http")
            .GetResult();
        AssertPass(result, "Domain must remain POCO (no Azure SDK, EF, or HttpClient types)");
    }

    [TestMethod]
    public void Rule04_InfrastructureHasNoModuleDependency()
    {
        var result = Types.InAssembly(InfraAsm)
            .Should()
            .NotHaveDependencyOnAny(
                "FileIt.Module.Services",
                "FileIt.Module.SimpleFlow",
                "FileIt.Module.DataFlow",
                "FileIt.Module.Complex")
            .GetResult();
        AssertPass(result, "Infrastructure must not reference any Module project");
    }

    [TestMethod]
    public void Rule05_InfrastructureHasNoHostDependency()
    {
        var result = Types.InAssembly(InfraAsm)
            .Should()
            .NotHaveDependencyOnAny(
                "FileIt.Module.Services.Host",
                "FileIt.Module.SimpleFlow.Host",
                "FileIt.Module.DataFlow.Host",
                "FileIt.Module.Complex.Host")
            .GetResult();
        AssertPass(result, "Infrastructure must not reference any Host project");
    }

    // ---- Module isolation -------------------------------------------------

    [TestMethod]
    public void Rule06_AppProjectsReferenceDomain()
    {
        // Each App must have at least one type that uses Domain (proves Domain is referenced).
        // We assert by checking that NO App is completely Domain-free.
        foreach (var asm in AllAppAssemblies())
        {
            var hasDomainRef = Types.InAssembly(asm)
                .That()
                .HaveDependencyOn("FileIt.Domain")
                .GetTypes()
                .Any();
            Assert.IsTrue(hasDomainRef,
                $"{asm.GetName().Name} has no types depending on FileIt.Domain. App layer must use Domain interfaces.");
        }
    }

    [TestMethod]
    public void Rule07_AppProjectsDoNotReferenceTheirOwnHost()
    {
        var pairs = new (Assembly app, string hostNs)[]
        {
            (ServicesAppAsm, "FileIt.Module.Services.Host"),
            (SimpleAppAsm,   "FileIt.Module.SimpleFlow.Host"),
            (DataFlowAppAsm, "FileIt.Module.DataFlow.Host"),
            (ComplexAppAsm,  "FileIt.Module.Complex.Host"),
        };
        foreach (var (app, hostNs) in pairs)
        {
            var result = Types.InAssembly(app)
                .Should()
                .NotHaveDependencyOn(hostNs)
                .GetResult();
            AssertPass(result, $"{app.GetName().Name} must not depend on its own Host ({hostNs})");
        }
    }

    [TestMethod]
    public void Rule08_AppProjectsDoNotReferenceOtherAppProjects()
    {
        var pairs = new (Assembly app, string[] forbidden)[]
        {
            (ServicesAppAsm, new[] { "FileIt.Module.SimpleFlow.App", "FileIt.Module.DataFlow.App", "FileIt.Module.Complex.App" }),
            (SimpleAppAsm,   new[] { "FileIt.Module.Services.App",   "FileIt.Module.DataFlow.App", "FileIt.Module.Complex.App" }),
            (DataFlowAppAsm, new[] { "FileIt.Module.Services.App",   "FileIt.Module.SimpleFlow.App", "FileIt.Module.Complex.App" }),
            (ComplexAppAsm,  new[] { "FileIt.Module.Services.App",   "FileIt.Module.SimpleFlow.App", "FileIt.Module.DataFlow.App" }),
        };
        foreach (var (app, forbidden) in pairs)
        {
            var result = Types.InAssembly(app)
                .Should()
                .NotHaveDependencyOnAny(forbidden)
                .GetResult();
            AssertPass(result, $"{app.GetName().Name} must not reference other module App projects");
        }
    }

    [TestMethod]
    public void Rule09_HostsDoNotReferenceOtherHosts()
    {
        var pairs = new (Assembly host, string[] forbidden)[]
        {
            (ServicesHostAsm, new[] { "FileIt.Module.SimpleFlow.Host", "FileIt.Module.DataFlow.Host", "FileIt.Module.Complex.Host" }),
            (SimpleHostAsm,   new[] { "FileIt.Module.Services.Host",   "FileIt.Module.DataFlow.Host", "FileIt.Module.Complex.Host" }),
            (DataFlowHostAsm, new[] { "FileIt.Module.Services.Host",   "FileIt.Module.SimpleFlow.Host", "FileIt.Module.Complex.Host" }),
            (ComplexHostAsm,  new[] { "FileIt.Module.Services.Host",   "FileIt.Module.SimpleFlow.Host", "FileIt.Module.DataFlow.Host" }),
        };
        foreach (var (host, forbidden) in pairs)
        {
            var result = Types.InAssembly(host)
                .Should()
                .NotHaveDependencyOnAny(forbidden)
                .GetResult();
            AssertPass(result, $"{host.GetName().Name} must not reference other module Host projects");
        }
    }

    [TestMethod]
    public void Rule10_HostsDoNotReferenceOtherModuleAppProjects()
    {
        var pairs = new (Assembly host, string[] forbidden)[]
        {
            (ServicesHostAsm, new[] { "FileIt.Module.SimpleFlow.App", "FileIt.Module.DataFlow.App", "FileIt.Module.Complex.App" }),
            (SimpleHostAsm,   new[] { "FileIt.Module.Services.App",   "FileIt.Module.DataFlow.App", "FileIt.Module.Complex.App" }),
            (DataFlowHostAsm, new[] { "FileIt.Module.Services.App",   "FileIt.Module.SimpleFlow.App", "FileIt.Module.Complex.App" }),
            (ComplexHostAsm,  new[] { "FileIt.Module.Services.App",   "FileIt.Module.SimpleFlow.App", "FileIt.Module.DataFlow.App" }),
        };
        foreach (var (host, forbidden) in pairs)
        {
            var result = Types.InAssembly(host)
                .Should()
                .NotHaveDependencyOnAny(forbidden)
                .GetResult();
            AssertPass(result, $"{host.GetName().Name} must not reference other module App projects");
        }
    }

    // ---- Naming and structure ---------------------------------------------

    [TestMethod]
    public void Rule11_AppPublicTypesLiveUnderAppNamespace()
    {
        var pairs = new (Assembly asm, string requiredPrefix)[]
        {
            (ServicesAppAsm, "FileIt.Module.Services.App"),
            (SimpleAppAsm,   "FileIt.Module.SimpleFlow.App"),
            (DataFlowAppAsm, "FileIt.Module.DataFlow.App"),
            (ComplexAppAsm,  "FileIt.Module.Complex.App"),
        };
        foreach (var (asm, prefix) in pairs)
        {
            var result = Types.InAssembly(asm)
                .That().ArePublic()
                .Should()
                .ResideInNamespaceStartingWith(prefix)
                .GetResult();
            AssertPass(result, $"Public types in {asm.GetName().Name} must live under {prefix}");
        }
    }

    [TestMethod]
    public void Rule12_HostPublicTypesLiveUnderHostNamespace()
    {
        // Function App hosts have a top-level Program.cs with no namespace
        // declaration, which lands the auto-generated Program class in the
        // global namespace. The compiler also emits <Module>. Both are
        // framework concerns, not authored types.
        var pairs = new (Assembly asm, string requiredPrefix)[]
        {
            (ServicesHostAsm, "FileIt.Module.Services.Host"),
            (SimpleHostAsm,   "FileIt.Module.SimpleFlow.Host"),
            (DataFlowHostAsm, "FileIt.Module.DataFlow.Host"),
            (ComplexHostAsm,  "FileIt.Module.Complex.Host"),
        };
        foreach (var (asm, prefix) in pairs)
        {
            var result = Types.InAssembly(asm)
                .That().ArePublic()
                .And().DoNotHaveName("Program")
                .And().DoNotHaveName("<Module>")
                .Should()
                .ResideInNamespaceStartingWith(prefix)
                .GetResult();
            AssertPass(result, $"Public types in {asm.GetName().Name} must live under {prefix}");
        }
    }

    [TestMethod]
    public void Rule13_CommandsQueriesHandlersUseConventionalSuffix()
    {
        // Command classes must end with "Command", queries with "Query", handlers with "Handler".
        // This catches drift before it accumulates.
        foreach (var asm in AllAppAssemblies())
        {
            // Result/Response types may live alongside their command but the
            // command itself must follow the suffix convention. We assert
            // only on classes whose name does NOT end with 'Result',
            // 'Response', or 'Dto', focusing on the command/handler proper.
            var commands = Types.InAssembly(asm)
                .That()
                .ResideInNamespaceContaining(".Commands")
                .And().AreClasses()
                .And().ArePublic()
                .And().DoNotHaveNameEndingWith("Result")
                .And().DoNotHaveNameEndingWith("Response")
                .And().DoNotHaveNameEndingWith("Dto")
                .Should()
                .HaveNameEndingWith("Command")
                .GetResult();
            AssertPass(commands, $"{asm.GetName().Name}: classes under .Commands must end with 'Command' (Result/Response/Dto types exempt)");

            var queries = Types.InAssembly(asm)
                .That()
                .ResideInNamespaceContaining(".Queries")
                .And().AreClasses()
                .And().ArePublic()
                .Should()
                .HaveNameEndingWith("Query")
                .GetResult();
            AssertPass(queries, $"{asm.GetName().Name}: classes under .Queries must end with 'Query'");
        }
    }

    [TestMethod]
    public void Rule14_InterfacesStartWithI()
    {
        foreach (var asm in AllAppAssemblies().Concat(AllHostAssemblies()).Concat(new[] { DomainAsm, InfraAsm }))
        {
            var result = Types.InAssembly(asm)
                .That().AreInterfaces()
                .And().ArePublic()
                .Should()
                .HaveNameStartingWith("I")
                .GetResult();
            AssertPass(result, $"{asm.GetName().Name}: public interfaces must start with 'I'");
        }
    }

    // ---- Runtime concerns -------------------------------------------------

    [TestMethod]
    public void Rule15_AppProjectsDoNotInstantiateAzureSdkClients()
    {
        // App layer talks via Domain interfaces. Direct Azure SDK use leaks
        // infrastructure concerns into business logic.
        foreach (var asm in AllAppAssemblies())
        {
            var result = Types.InAssembly(asm)
                .Should()
                .NotHaveDependencyOnAny(
                    "Azure.Messaging.ServiceBus",
                    "Azure.Storage.Blobs",
                    "Microsoft.EntityFrameworkCore")
                .GetResult();
            // exempt: types whose only "dependency" is via Domain interface signatures, NetArchTest
            // counts the type as having a transitive dependency. We accept that and only fail
            // when the App assembly itself produces a type that names these libraries directly.
            // If this rule turns out to be too strict in practice we'll narrow the assertion.
            AssertPass(result, $"{asm.GetName().Name}: App must not depend on Azure SDK or EF directly");
        }
    }

    [TestMethod]
    public void Rule16_NoAsyncVoidExceptInEventHandlers()
    {
        // async void anywhere except event handlers is a swallow-the-exception bug.
        // Allowed exception: methods whose name ends with EventHandler or Handler are tolerated.
        foreach (var asm in AllAppAssemblies().Concat(AllHostAssemblies()).Concat(new[] { InfraAsm }))
        {
            var asyncVoidMethods = asm.GetTypes()
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic
                                              | BindingFlags.Instance | BindingFlags.Static
                                              | BindingFlags.DeclaredOnly))
                .Where(m =>
                    m.ReturnType == typeof(void)
                    && m.GetCustomAttributes(typeof(System.Runtime.CompilerServices.AsyncStateMachineAttribute), false).Any()
                    && !(m.Name.EndsWith("EventHandler") || m.Name.EndsWith("Handler")))
                .Select(m => $"{m.DeclaringType?.FullName}.{m.Name}")
                .ToArray();

            Assert.AreEqual(0, asyncVoidMethods.Length,
                $"{asm.GetName().Name}: async void found:\n  {string.Join("\n  ", asyncVoidMethods)}");
        }
    }
}
