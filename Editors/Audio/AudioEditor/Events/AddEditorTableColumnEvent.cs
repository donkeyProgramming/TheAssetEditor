using System.Data;

namespace Editors.Audio.AudioEditor.Events
{
    public record AddEditorTableColumnEvent(DataColumn Column);
}
