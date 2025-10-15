using System.Collections.Generic;
using System.Collections.ObjectModel;
using Editors.Audio.AudioEditor.Core;
using Editors.Audio.AudioEditor.Events;
using Editors.Audio.AudioEditor.Presentation.Shared;
using Editors.Audio.Shared.AudioProject.Compiler;
using Editors.Audio.Shared.AudioProject.Models;
using Editors.Audio.Shared.Storage;
using Editors.Audio.Shared.Wwise;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.Commands
{
    public class SetAudioFilesCommand(IAudioEditorStateService audioEditorStateService, IEventHub eventHub, IAudioRepository audioRepository) : IUiCommand
    {
        private readonly IAudioEditorStateService _audioEditorStateService = audioEditorStateService;
        private readonly IEventHub _eventHub = eventHub;
        private readonly IAudioRepository _audioRepository = audioRepository;

        public void Execute(ObservableCollection<AudioFilesTreeNode> selectedAudioFiles, bool addToExistingAudioFiles)
        {
            var usedSourceIds = new HashSet<uint>();
            var audioProject = _audioEditorStateService.AudioProject;

            var audioProjectSourceIds = audioProject.GetAudioFileIds();
            var languageId = WwiseHash.Compute(audioProject.Language);
            var languageSourceIds = _audioRepository.GetUsedVanillaSourceIdsByLanguageId(languageId);

            usedSourceIds.UnionWith(audioProjectSourceIds);
            usedSourceIds.UnionWith(languageSourceIds);

            var audioFiles = new List<AudioFile>();
            foreach (var wavFile in selectedAudioFiles)
            {
                var audioFile = audioProject.GetAudioFile(wavFile.FilePath);
                if (audioFile == null)
                {
                    var audioFileIds = IdGenerator.GenerateIds(usedSourceIds);
                    audioFile = AudioFile.Create(audioFileIds.Guid, audioFileIds.Id, wavFile.FileName, wavFile.FilePath);
                }
                audioFiles.Add(audioFile);
            }

            _audioEditorStateService.StoreAudioFiles(audioFiles);
            _eventHub.Publish(new AudioFilesChangedEvent(audioFiles, addToExistingAudioFiles, false, false));
        }
    }
}
