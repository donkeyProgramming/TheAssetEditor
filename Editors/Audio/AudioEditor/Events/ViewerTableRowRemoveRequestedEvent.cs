using System.Data;

namespace Editors.Audio.AudioEditor.Events
{
    public record ViewerTableRowRemoveRequestedEvent(DataRow Row);
}
