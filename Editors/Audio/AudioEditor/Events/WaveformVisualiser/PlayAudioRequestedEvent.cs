using System.Collections.Generic;

namespace Editors.Audio.AudioEditor.Events.WaveformVisualiser
{
    public record PlayAudioRequestedEvent(List<string> WavFilePaths);
}
