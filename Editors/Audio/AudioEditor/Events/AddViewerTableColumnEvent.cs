using System.Data;

namespace Editors.Audio.AudioEditor.Events
{
    public record AddViewerTableColumnEvent(DataColumn Column);
}
