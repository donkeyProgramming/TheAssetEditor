using System.Collections.Generic;
using Editors.Audio.AudioEditor.Core;
using Editors.Audio.AudioEditor.Events.AudioFilesExplorer;
using Editors.Audio.AudioEditor.Presentation.Shared.Models;
using Editors.Audio.Shared.AudioProject.Compiler;
using Editors.Audio.Shared.AudioProject.Models;
using Editors.Audio.Shared.Storage;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.Commands.AudioFilesExplorer
{
    public class SetAudioFilesCommand(IAudioEditorStateService audioEditorStateService, IEventHub eventHub, IAudioRepository audioRepository) : IAeCommand
    {
        private readonly IAudioEditorStateService _audioEditorStateService = audioEditorStateService;
        private readonly IEventHub _eventHub = eventHub;
        private readonly IAudioRepository _audioRepository = audioRepository;
        private List<AudioFilesTreeNode> _selectedAudioFiles = new();
        private bool _addToExistingAudioFiles;

        public void Configure(List<AudioFilesTreeNode> selectedAudioFiles, bool addToExistingAudioFiles)
        {
            _selectedAudioFiles = selectedAudioFiles;
            _addToExistingAudioFiles = addToExistingAudioFiles;
        }

        public void Execute()
        {
            var usedSourceIds = IdGenerator.GetUsedSourceIds(_audioRepository, _audioEditorStateService.AudioProject);

            var audioFiles = new List<AudioFile>();
            foreach (var wavFile in _selectedAudioFiles)
            {
                var audioFile = _audioEditorStateService.AudioProject.GetAudioFile(wavFile.FilePath);
                if (audioFile == null)
                {
                    var audioFileIds = IdGenerator.GenerateIds(usedSourceIds);
                    audioFile = new AudioFile(audioFileIds.Guid, audioFileIds.Id, wavFile.FileName, wavFile.FilePath);
                }
                audioFiles.Add(audioFile);
            }

            _audioEditorStateService.StoreAudioFiles(audioFiles);
            _eventHub.Publish(new AudioFilesChangedEvent(audioFiles, _addToExistingAudioFiles, false, false));
        }
    }
}
