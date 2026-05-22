using System.Data;

namespace Editors.Audio.AudioEditor.Events.AudioProjectViewer.Table
{
    public record ViewerTableRowEditedEvent(DataRow Row);
}
