using System.Data;

namespace Editors.Audio.AudioEditor.Events
{
    public record ViewerTableRowAddRequestedEvent(DataRow Row);
}
