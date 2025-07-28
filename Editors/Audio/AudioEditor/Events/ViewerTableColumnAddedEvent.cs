using System.Data;

namespace Editors.Audio.AudioEditor.Events
{
    public record ViewerTableColumnAddedEvent(DataColumn Column);
}
