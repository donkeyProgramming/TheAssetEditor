using System.Collections.Generic;
using Editors.Audio.AudioEditor.Models;

namespace Editors.Audio.AudioEditor.Events
{
    public record AudioFilesChangedEvent(List<AudioFile> AudioFiles, bool AddToExistingAudioFiles, bool IsSetFromViewerItem);
}
