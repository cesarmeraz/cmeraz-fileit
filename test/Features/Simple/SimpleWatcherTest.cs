using FileIt.App.Common.Tools;
using FileIt.App.Features.Simple;
using Microsoft.Extensions.Logging;
using Moq;

namespace FileIt.Test.Features.Simple
{
    [TestClass]
    public class SimpleWatcherTest
    {
        private Mock<ILogger<SimpleWatcher>>? loggerMock;
        private Mock<IBlobTool>? blobToolMock;
        private Mock<IBusTool>? busToolMock;
        private SimpleConfig? config;
        private Mock<ISimpleRequestLogRepo>? requestLogRepoMock;
        private SimpleWatcher? target;

        public SimpleWatcherTest() { }

        [TestInitialize]
        public void Setup()
        {
            loggerMock = new Mock<ILogger<SimpleWatcher>>();
            blobToolMock = new Mock<IBlobTool>();
            busToolMock = new Mock<IBusTool>();
            requestLogRepoMock = new Mock<ISimpleRequestLogRepo>();
            config = new SimpleConfig()
            {
                ApiAddQueueName = string.Empty,
                ApiAddTopicName = string.Empty,
                FeatureName = "simple",
                FinalContainer = "final",
                QueueName = "simple",
                SourceContainer = "source",
                WorkingContainer = "working",
                SimpleTestEventId = 1,
                SimpleIntakeEventId = 2,
                SimpleSubscriberEventId = 3,
            };
            target = new SimpleWatcher(
                loggerMock.Object,
                blobToolMock.Object,
                busToolMock.Object,
                requestLogRepoMock.Object,
                config
            );
        }
    }
}
