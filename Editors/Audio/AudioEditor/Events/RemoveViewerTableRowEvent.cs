using System.Data;

namespace Editors.Audio.AudioEditor.Events
{
    public record RemoveViewerTableRowEvent(DataRow Row);
}
