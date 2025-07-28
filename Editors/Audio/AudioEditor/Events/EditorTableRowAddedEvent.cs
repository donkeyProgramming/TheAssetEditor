using System.Data;

namespace Editors.Audio.AudioEditor.Events
{
    public record EditorTableRowAddedEvent(DataRow Row);
}
