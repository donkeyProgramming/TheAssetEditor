using System.Collections.Generic;

namespace Editors.Audio.WaveformVisualiser.Events
{
    public record DecacheWaveformRequestedEvent(List<string> FilePaths);
}
