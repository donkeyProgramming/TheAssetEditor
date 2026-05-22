using System.Data;

namespace Editors.Audio.AudioEditor.Events.AudioProjectEditor.Table
{
    public record EditorTableColumnAddRequestedEvent(DataColumn Column);
}
