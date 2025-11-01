using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using Editors.Audio.AudioEditor.Events;
using Editors.Audio.Shared.AudioProject;
using Editors.Audio.Shared.AudioProject.Models;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Editors.Audio.Shared.Storage;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Settings;

namespace Editors.Audio.AudioEditor.Core
{
    public interface IAudioEditorFileService
    {
        void Save(AudioProjectFile audioProject, string fileName, string filePath);
        void Load(AudioProjectFile audioProject, string fileName, string filePath, bool isNotLoadedFromDialog = true);
        void LoadFromDialog();
    }

    public class AudioEditorFileService(
        IEventHub eventHub,
        IAudioProjectFileService audioProjectFileService,
        IAudioEditorStateService audioEditorStateService,
        IFileSaveService fileSaveService,
        IAudioRepository audioRepository,
        IAudioEditorIntegrityService audioEditorIntegrityService,
        ApplicationSettingsService applicationSettingsService) : IAudioEditorFileService
    {
        private readonly IEventHub _eventHub = eventHub;
        private readonly IAudioProjectFileService _audioProjectFileService = audioProjectFileService;
        private readonly IAudioEditorStateService _audioEditorStateService = audioEditorStateService;
        private readonly IFileSaveService _fileSaveService = fileSaveService;
        private readonly IAudioRepository _audioRepository = audioRepository;
        private readonly IAudioEditorIntegrityService _audioEditorIntegrityService = audioEditorIntegrityService;
        private readonly ApplicationSettingsService _applicationSettingsService = applicationSettingsService;

        public void Save(AudioProjectFile audioProject, string fileName, string filePath)
        {
            var cleanedAudioProject = AudioProjectFile.Clean(audioProject);

            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true
            };
            var audioProjectJson = JsonSerializer.Serialize(cleanedAudioProject, options);

            var packFile = PackFile.CreateFromASCII(fileName, audioProjectJson);
            _fileSaveService.Save(filePath, packFile.DataSource.ReadData(), false);
        }

        public void LoadFromDialog()
        {
            var result = _audioProjectFileService.LoadFromDialog();
            if (result != null)
                Load(result.AudioProject, result.FileName, result.FilePath, false);
        }

        public void Load(AudioProjectFile audioProject, string fileName, string filePath, bool isNotLoadedFromDialog = true)
        {
            if (fileName.Contains(' '))
            {
                MessageBox.Show("You must rename the Audio Project as its name contains spaces which is not allowed.", "Error");
                return;
            }

            if (isNotLoadedFromDialog)
            {
                var result = _audioProjectFileService.Load(fileName, filePath);
                audioProject = result.AudioProject;
            }

            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

            // We create a 'dirty' Audio Project to display the whole model in the Audio Project Explorer rather than
            // just the clean data from the loaded Audio Project as when it's saved any unused parts are removed.
            var currentGame = _applicationSettingsService.CurrentSettings.CurrentGame;
            var dirtyAudioProject = AudioProjectFile.Create(audioProject, currentGame, fileNameWithoutExtension);

            _audioRepository.Load([audioProject.Language]);

            _audioEditorIntegrityService.CheckDialogueEventInformationIntegrity(Wh3DialogueEventInformation.Information);
            _audioEditorIntegrityService.CheckAudioProjectDialogueEventIntegrity(dirtyAudioProject);
            _audioEditorIntegrityService.CheckAudioProjectWavFilesIntegrity(dirtyAudioProject);
            _audioEditorIntegrityService.CheckAudioProjectDataIntegrity(dirtyAudioProject, fileNameWithoutExtension);

            _audioEditorStateService.StoreAudioProject(dirtyAudioProject);
            _audioEditorStateService.StoreAudioProjectFileName(fileName);
            _audioEditorStateService.StoreAudioProjectFilePath(filePath);

            _eventHub.Publish(new AudioProjectLoadedEvent());
        }
    }
}
