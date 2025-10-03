using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using Editors.Audio.AudioEditor.Events;
using Editors.Audio.AudioEditor.Models;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.Core.Settings;

namespace Editors.Audio.AudioEditor
{
    public interface IAudioProjectFileService
    {
        void Save(AudioProject audioProject, string fileName, string filePath);
        void Load();
    }

    public class AudioProjectFileService(
        IEventHub eventHub,
        IAudioEditorStateService audioEditorStateService,
        IPackFileService packFileService,
        IFileSaveService fileSaveService,
        IStandardDialogs standardDialogs,
        IAudioEditorIntegrityService audioEditorIntegrityService,
        ApplicationSettingsService applicationSettingsService) : IAudioProjectFileService
    {
        private readonly IEventHub _eventHub = eventHub;
        private readonly IAudioEditorStateService _audioEditorStateService = audioEditorStateService;
        private readonly IPackFileService _packFileService = packFileService;
        private readonly IFileSaveService _fileSaveService = fileSaveService;
        private readonly IStandardDialogs _standardDialogs = standardDialogs;
        private readonly IAudioEditorIntegrityService _audioEditorIntegrityService = audioEditorIntegrityService;
        private readonly ApplicationSettingsService _applicationSettingsService = applicationSettingsService;

        public void Save(AudioProject audioProject, string fileName, string filePath)
        {
            var cleanedAudioProject = AudioProject.Clean(audioProject);

            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true
            };
            var audioProjectJson = JsonSerializer.Serialize(cleanedAudioProject, options);

            var packFile = PackFile.CreateFromASCII(fileName, audioProjectJson);
            _fileSaveService.Save(filePath, packFile.DataSource.ReadData(), false);
        }

        public void Load()
        {
            var result = _standardDialogs.DisplayBrowseDialog([".aproj"]);
            if (result.Result)
            {
                var filePath = _packFileService.GetFullPath(result.File);
                var fileName = Path.GetFileName(filePath);
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

                if (fileName.Contains(' '))
                {
                    MessageBox.Show("You must rename the Audio Project as its name contains spaces which is not allowed.", "Error");
                    return;
                }

                var packFile = _packFileService.FindFile(filePath);
                var bytes = packFile.DataSource.ReadData();
                var audioProjectJson = Encoding.UTF8.GetString(bytes);
                var audioProject = JsonSerializer.Deserialize<AudioProject>(audioProjectJson);

                // We create a 'dirty' Audio Project to display the whole model in the Audio Project Explorer rather than
                // just the clean data from the loaded Audio Project as when it's saved any unused parts are removed.
                var currentGame = _applicationSettingsService.CurrentSettings.CurrentGame;
                var dirtyAudioProject = AudioProject.Create(audioProject, currentGame, fileNameWithoutExtension);

                _audioEditorIntegrityService.CheckAudioProjectDialogueEventIntegrity(dirtyAudioProject);
                _audioEditorIntegrityService.CheckAudioProjectWavFilesIntegrity(dirtyAudioProject);
                _audioEditorIntegrityService.CheckAudioProjectDataIntegrity(dirtyAudioProject, fileNameWithoutExtension);

                _audioEditorStateService.StoreAudioProject(dirtyAudioProject);
                _audioEditorStateService.StoreAudioProjectFileName(fileName);
                _audioEditorStateService.StoreAudioProjectFilePath(filePath);

                _eventHub.Publish(new AudioProjectInitialisedEvent());
            }
        }
    }
}
