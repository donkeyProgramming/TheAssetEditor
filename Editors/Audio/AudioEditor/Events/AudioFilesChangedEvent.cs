using System.Collections.ObjectModel;
using Editors.Audio.AudioEditor.Settings;

namespace Editors.Audio.AudioEditor.Events
{
    public record AudioFilesChangedEvent(ObservableCollection<AudioFile> AudioFiles);
}
