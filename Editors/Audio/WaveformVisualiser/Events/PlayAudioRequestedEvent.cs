using System.Collections.Generic;

namespace Editors.Audio.WaveformVisualiser.Events
{
    public record PlayAudioRequestedEvent(List<string> WavFilePaths);
}
