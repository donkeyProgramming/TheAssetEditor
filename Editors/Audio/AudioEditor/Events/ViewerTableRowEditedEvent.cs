using System.Data;

namespace Editors.Audio.AudioEditor.Events
{
    public record ViewerTableRowEditedEvent(DataRow Row);
}
