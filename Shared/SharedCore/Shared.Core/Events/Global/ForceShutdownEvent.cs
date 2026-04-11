using Shared.Core.DependencyInjection;
using Shared.Core.ToolCreation;

namespace Shared.Core.Events.Global
{
    public class ForceShutdownEvent(IEditorInterface editorHandle)
    {
        public IEditorInterface EditorHandle { get; private set; } = editorHandle;
    }
}
