using System.Collections.Generic;

namespace Editors.Audio.AudioEditor.Events
{
    public record AddToWaveformCacheRequestedEvent(List<string> FilePaths);
}
