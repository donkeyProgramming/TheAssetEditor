using System.Collections.Generic;

namespace Editors.Audio.AudioEditor.Events.WaveformVisualiser
{
    public record CacheWaveformRequestedEvent(List<string> FilePaths);
}
