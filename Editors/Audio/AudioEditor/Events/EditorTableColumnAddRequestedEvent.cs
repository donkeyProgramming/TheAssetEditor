using System.Data;

namespace Editors.Audio.AudioEditor.Events
{
    public record EditorTableColumnAddRequestedEvent(DataColumn Column);
}
