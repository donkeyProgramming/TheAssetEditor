using System.Data;

namespace Editors.Audio.AudioEditor.Events
{
    public record ViewerTableColumnAddRequestedEvent(DataColumn Column);
}
