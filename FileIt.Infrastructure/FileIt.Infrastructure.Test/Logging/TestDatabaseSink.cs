using FileIt.Infrastructure.Logging;
using Moq;
using Serilog.Events;
using Serilog.Parsing;

namespace FileIt.Infrastructure.Test.Logging;

[TestClass]
public class TestDatabaseSink
{
    public required Mock<ICommonLogConfig> _configMock;
    public required DatabaseSink target;

    [TestInitialize]
    public void Setup()
    {
        _configMock = new Mock<ICommonLogConfig>();
        // Default: empty connection string so Emit short-circuits without DB I/O.
        _configMock.SetupGet(c => c.DbConnectionString).Returns(string.Empty);
        _configMock.SetupGet(c => c.Environment).Returns("DEV");
        _configMock.SetupGet(c => c.Application).Returns("FileIt.Test");
        _configMock.SetupGet(c => c.ApplicationVersion).Returns("1.0.0");
        _configMock.SetupGet(c => c.Host).Returns("test-host");
        target = new DatabaseSink(_configMock.Object);
    }

    private static LogEvent BuildLogEvent(
        LogEventLevel level = LogEventLevel.Information,
        string template = "Hello {Name}",
        IEnumerable<LogEventProperty>? properties = null)
    {
        var parser = new MessageTemplateParser();
        var msgTemplate = parser.Parse(template);
        return new LogEvent(
            DateTimeOffset.UtcNow,
            level,
            exception: null,
            messageTemplate: msgTemplate,
            properties: properties ?? Enumerable.Empty<LogEventProperty>());
    }

    [TestMethod]
    public void Emit_NullConnectionString_ShortCircuitsWithoutThrowing()
    {
        _configMock.SetupGet(c => c.DbConnectionString).Returns((string?)null);
        target = new DatabaseSink(_configMock.Object);
        var evt = BuildLogEvent();

        // No exception, no DB call.
        target.Emit(evt);
    }

    [TestMethod]
    public void Emit_EmptyConnectionString_ShortCircuitsWithoutThrowing()
    {
        // Default setup is empty connection string.
        var evt = BuildLogEvent();
        target.Emit(evt);
    }

    [TestMethod]
    public void GetEventIdId_StructuredEventId_ReturnsId()
    {
        var props = new[]
        {
            new LogEventProperty("EventId", new StructureValue(new[]
            {
                new LogEventProperty("Id", new ScalarValue(42)),
                new LogEventProperty("Name", new ScalarValue("TestEvent")),
            })),
        };
        var evt = BuildLogEvent(properties: props);

        var result = target.GetEventIdId(evt);

        Assert.AreEqual(42, result);
    }

    [TestMethod]
    public void GetEventIdId_NoEventIdProperty_ReturnsNull()
    {
        var evt = BuildLogEvent();

        var result = target.GetEventIdId(evt);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetEventIdId_EventIdIsScalarNotStructure_ReturnsNull()
    {
        var props = new[]
        {
            new LogEventProperty("EventId", new ScalarValue(42)),
        };
        var evt = BuildLogEvent(properties: props);

        var result = target.GetEventIdId(evt);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetEventIdName_StructuredEventId_ReturnsName()
    {
        var props = new[]
        {
            new LogEventProperty("EventId", new StructureValue(new[]
            {
                new LogEventProperty("Id", new ScalarValue(42)),
                new LogEventProperty("Name", new ScalarValue("DeadLetterMessageReceived")),
            })),
        };
        var evt = BuildLogEvent(properties: props);

        var result = target.GetEventIdName(evt);

        Assert.AreEqual("DeadLetterMessageReceived", result);
    }

    [TestMethod]
    public void GetEventIdName_NoEventIdProperty_ReturnsNull()
    {
        var evt = BuildLogEvent();

        var result = target.GetEventIdName(evt);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetEventIdName_StructuredEventIdMissingNameSubProperty_ReturnsNull()
    {
        var props = new[]
        {
            new LogEventProperty("EventId", new StructureValue(new[]
            {
                new LogEventProperty("Id", new ScalarValue(42)),
            })),
        };
        var evt = BuildLogEvent(properties: props);

        var result = target.GetEventIdName(evt);

        Assert.IsNull(result);
    }
}
