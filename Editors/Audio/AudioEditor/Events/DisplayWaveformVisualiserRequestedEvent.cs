using System.Collections.Generic;

namespace Editors.Audio.AudioEditor.Events
{
    public record DisplayWaveformVisualiserRequestedEvent(List<string> WavFilePaths);
}
