using System.Collections.ObjectModel;
using Editors.Audio.AudioEditor.Settings;

namespace Editors.Audio.AudioEditor.Events
{
    public record AudioFilesSetEvent(ObservableCollection<AudioFile> AudioFiles);
}
