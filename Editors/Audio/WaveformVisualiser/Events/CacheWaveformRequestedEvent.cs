using System.Collections.Generic;

namespace Editors.Audio.WaveformVisualiser.Events
{
    public record CacheWaveformRequestedEvent(List<string> FilePaths);
}
