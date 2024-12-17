using Shared.Core.ToolCreation;

namespace Test.TestingUtility.TestUtility
{
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
