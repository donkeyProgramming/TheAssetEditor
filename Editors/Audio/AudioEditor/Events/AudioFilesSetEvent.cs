using System.Collections.ObjectModel;
using Editors.Audio.AudioEditor.AudioSettings;

namespace Editors.Audio.AudioEditor.Events
{
    public record AudioFilesSetEvent(ObservableCollection<AudioFile> AudioFiles);
}
