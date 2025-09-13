using System.Collections.ObjectModel;
using System.Linq;
using Editors.Audio.AudioEditor.AudioFilesExplorer;
using Editors.Audio.AudioEditor.Events;
using Editors.Audio.AudioEditor.Settings;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.UICommands
{
    public class SetAudioFilesCommand(IAudioEditorStateService audioEditorStateService, IEventHub eventHub) : IUiCommand
    {
        private readonly IAudioEditorStateService _audioEditorStateService = audioEditorStateService;
        private readonly IEventHub _eventHub = eventHub;

        public void Execute(ObservableCollection<AudioFilesTreeNode> selectedAudioFiles)
        {
            var audioFiles = new ObservableCollection<AudioFile>();
            foreach (var wavFile in selectedAudioFiles)
            {
                audioFiles.Add(new AudioFile
                {
                    FileName = wavFile.FileName,
                    FilePath = wavFile.FilePath
                });
            }

            _audioEditorStateService.StoreAudioFiles(audioFiles.ToList());
            _eventHub.Publish(new AudioFilesChangedEvent(audioFiles));
        }
    }
}
