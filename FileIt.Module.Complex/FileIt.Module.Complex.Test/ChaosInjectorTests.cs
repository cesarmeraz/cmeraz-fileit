using FileIt.Module.Complex.App;
using FileIt.Module.Complex.App.Behavior;
using Microsoft.Extensions.Logging.Abstractions;

namespace FileIt.Module.Complex.Test;

[TestClass]
public class ChaosInjectorTests
{
    [TestMethod]
    public void Disabled_NeverFails()
    {
        var config = new ComplexConfig
        {
            Chaos = new ChaosOptions { Enabled = false, Failure503Rate = 1.0 }
        };
        var sut = new ChaosInjector(config, NullLogger<ChaosInjector>.Instance);

        for (int i = 0; i < 100; i++)
        {
            Assert.IsFalse(sut.ShouldFail("/api/documents"));
        }
    }

    [TestMethod]
    public void ZeroRate_NeverFails()
    {
        var config = new ComplexConfig
        {
            Chaos = new ChaosOptions { Enabled = true, Failure503Rate = 0.0 }
        };
        var sut = new ChaosInjector(config, NullLogger<ChaosInjector>.Instance);

        for (int i = 0; i < 100; i++)
        {
            Assert.IsFalse(sut.ShouldFail("/api/documents"));
        }
    }

    [TestMethod]
    public void FullRate_AlwaysFails()
    {
        var config = new ComplexConfig
        {
            Chaos = new ChaosOptions
            {
                Enabled = true,
                Failure503Rate = 1.0,
                ExemptPaths = new[] { "/api/health" }
            }
        };
        var sut = new ChaosInjector(config, NullLogger<ChaosInjector>.Instance);

        Assert.IsTrue(sut.ShouldFail("/api/documents"));
    }

    [TestMethod]
    public void ExemptPath_NeverFails()
    {
        var config = new ComplexConfig
        {
            Chaos = new ChaosOptions
            {
                Enabled = true,
                Failure503Rate = 1.0,
                ExemptPaths = new[] { "/api/health", "/api/docs" }
            }
        };
        var sut = new ChaosInjector(config, NullLogger<ChaosInjector>.Instance);

        Assert.IsFalse(sut.ShouldFail("/api/health"));
        Assert.IsFalse(sut.ShouldFail("/api/docs"));
        Assert.IsFalse(sut.ShouldFail("/api/docs/swagger.json"));
        Assert.IsTrue(sut.ShouldFail("/api/documents"));
    }
}
