using System.Collections.Generic;

namespace Editors.Audio.AudioEditor.Events.WaveformVisualiser
{
    public record DecacheWaveformRequestedEvent(List<string> FilePaths);
}
