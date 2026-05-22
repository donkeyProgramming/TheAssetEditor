using System.Data;

namespace Editors.Audio.AudioEditor.Events.AudioProjectViewer.Table
{
    public record ViewerTableRowAddRequestedEvent(DataRow Row);
}
