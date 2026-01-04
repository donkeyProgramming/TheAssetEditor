using System.Collections.Generic;

namespace Editors.Audio.AudioEditor.Events
{
    public record DecacheWaveformRequestedEvent(List<string> FilePaths);
}
