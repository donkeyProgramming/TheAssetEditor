using Moq;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.ToolCreation;

namespace Test.TestingUtility.TestUtility
{
    public static class MockScopedLogger
    {
        public static IScopedLogger Create()
        {
            var loggerMock = new Mock<ILogger>();
            loggerMock.Setup(x => x.ForContext(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>())).Returns(loggerMock.Object);

            var scopedMock = new Mock<IScopedLogger>();
            scopedMock.Setup(x => x.ForContext<It.IsAnyType>()).Returns(loggerMock.Object);
            return scopedMock.Object;
        }
    }

    public record BaseEvent();
    public record ExampleEventNoBase;

    public record ExampleEvent : BaseEvent;

    public class ScopedClass : IDisposable
    {
        public bool IsDisposed { get; private set; } = false;
        public void Dispose() => IsDisposed = true;
    }

    public class SimpleEditor(string name) : IEditorInterface
    {
        public bool IsClosed { get; private set; } = false;
        public string DisplayName { get; set; } = name;
        public void Close() => IsClosed = true;
    }

}
