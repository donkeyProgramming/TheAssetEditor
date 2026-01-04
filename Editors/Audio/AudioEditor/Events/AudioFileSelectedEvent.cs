using System.Collections.Generic;

namespace Editors.Audio.AudioEditor.Events
{
    public record AudioFileSelectedEvent(List<string> WavFilePaths);
}
