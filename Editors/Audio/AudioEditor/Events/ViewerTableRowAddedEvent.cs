using System.Data;

namespace Editors.Audio.AudioEditor.Events
{
    public record ViewerTableRowAddedEvent(DataRow Row);
}
