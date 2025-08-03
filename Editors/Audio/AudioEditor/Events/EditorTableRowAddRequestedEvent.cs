using System.Data;

namespace Editors.Audio.AudioEditor.Events
{
    public record EditorTableRowAddRequestedEvent(DataRow Row);
}
