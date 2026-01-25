using System.Data;

namespace Editors.Audio.AudioEditor.Events.AudioProjectViewer.Table
{
    public record ViewerTableColumnAddRequestedEvent(DataColumn Column);
}
