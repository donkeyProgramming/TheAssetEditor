using Shared.Core.ToolCreation;

namespace Shared.Core.Events.Scoped
{
    public class ScopedFileSavedEvent
    {
        public IEditorInterface? FileOwner { get; set; }
        public required string NewPath { get;  set; }
    }
}
