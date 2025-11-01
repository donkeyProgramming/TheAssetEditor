using System.Collections.Generic;

namespace Editors.Audio.AudioEditor.Events
{
    public record RemoveFromWaveformCacheRequestedEvent(List<string> FilePaths);
}
