using System.Data;

namespace Editors.Audio.AudioEditor.Events.AudioProjectEditor.Table
{
    public record EditorTableRowAddRequestedEvent(DataRow Row);
}
