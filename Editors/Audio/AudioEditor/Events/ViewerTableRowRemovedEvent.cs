using System.Data;

namespace Editors.Audio.AudioEditor.Events
{
    public record ViewerTableRowRemovedEvent(DataRow Row);
}
