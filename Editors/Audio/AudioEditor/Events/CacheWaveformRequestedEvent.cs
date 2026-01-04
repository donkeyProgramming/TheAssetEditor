using System.Collections.Generic;

namespace Editors.Audio.AudioEditor.Events
{
    public record CacheWaveformRequestedEvent(List<string> FilePaths);
}
