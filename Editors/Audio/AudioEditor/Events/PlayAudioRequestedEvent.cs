using System.Collections.Generic;

namespace Editors.Audio.AudioEditor.Events
{
    public record PlayAudioRequestedEvent(List<string> WavFilePaths);
}
