using Shared.GameFormats.Twui.Data;

namespace Editors.Twui.Editor.Events
{
    internal record RedrawTwuiEvent(TwuiFile? TwuiFile, Component? SelectedComponent);
}
