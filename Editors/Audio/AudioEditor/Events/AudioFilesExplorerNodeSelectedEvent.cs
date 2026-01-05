using System.Collections.Generic;

namespace Editors.Audio.AudioEditor.Events
{
    public record AudioFilesExplorerNodeSelectedEvent(List<string> WavFilePaths);
}
