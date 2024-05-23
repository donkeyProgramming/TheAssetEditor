using SharedCore.Events;
using SharedCore.Misc;
using SharedCore.PackFiles.Models;
using SharedCore.ToolCreation;
using System;

namespace BaseTests.ToolCreation
{
    public class TestEditor : IEditorViewModel, IDisposable
    {
        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("TestEditor");
        public PackFile? MainFile { get; set; } = null;
        public EventHub EventHub { get; }
        public bool IsDisposed { get; private set; } = false;
        public void Close(){}

        public void Dispose()
        {
            IsDisposed = true;
        }

        public TestEditor(EventHub eventHub)
        {
            EventHub = eventHub;
        }
    }

}
