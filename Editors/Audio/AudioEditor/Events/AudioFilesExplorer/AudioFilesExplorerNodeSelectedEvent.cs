using System.Collections.Generic;

namespace Editors.Audio.AudioEditor.Events.AudioFilesExplorer
{
    public record AudioFilesExplorerNodeSelectedEvent(List<string> WavFilePaths);
}
