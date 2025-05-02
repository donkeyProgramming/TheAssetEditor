using System.Collections.Generic;
using Editors.Audio.AudioEditor.AudioFilesExplorer;

namespace Editors.Audio.AudioEditor.Events
{
    public record AudioFilesSetEvent(List<TreeNode> AudioFiles);
}
