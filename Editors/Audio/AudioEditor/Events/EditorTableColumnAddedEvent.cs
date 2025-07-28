using System.Data;

namespace Editors.Audio.AudioEditor.Events
{
    public record EditorTableColumnAddedEvent(DataColumn Column);
}
