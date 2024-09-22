using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using CommonControls.PackFileBrowser;
using Editors.Audio.AudioEditor.ViewModels;
using Editors.Audio.Storage;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using static Editors.Audio.AudioEditor.AudioEditorHelpers;

namespace Editors.Audio.AudioEditor
{
    public interface IAudioProjectService
    {
        AudioProjectData AudioProject { get; set; }
        Dictionary<string, List<string>> StateGroupsWithCustomStates { get; set; }
        void SaveAudioProject(PackFileService packFileService);
        void LoadAudioProject(PackFileService packFileService, IAudioRepository audioRepository, AudioEditorViewModel audioEditorViewModel);
        void InitialiseAudioProject(string fileName, string directory, string language);
        void ResetAudioProject();
    }

    public class AudioProjectService : IAudioProjectService
    {
        readonly ILogger _logger = Logging.Create<AudioEditorViewModel>();

        public AudioProjectData AudioProject { get; set; } = new AudioProjectData();
        public Dictionary<string, List<string>> StateGroupsWithCustomStates { get; set; } = [];

        public void SaveAudioProject(PackFileService packFileService)
        {
            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true
            };

            var fileJson = JsonSerializer.Serialize(AudioProject, options);
            var pack = packFileService.GetEditablePack();
            var byteArray = Encoding.ASCII.GetBytes(fileJson);

            packFileService.AddFileToPack(pack, AudioProject.Directory, new PackFile($"{AudioProject.FileName}.aproj", new MemorySource(byteArray)));

            _logger.Here().Information($"Saved Audio Project file: {AudioProject.Directory}\\{AudioProject.FileName}.aproj");
        }

        public void LoadAudioProject(PackFileService packFileService, IAudioRepository audioRepository, AudioEditorViewModel audioEditorViewModel)
        {
            using var browser = new PackFileBrowserWindow(packFileService, [".aproj"]);

            if (browser.ShowDialog())
            {
                var filePath = browser.SelectedPath;
                var fileName = Path.GetFileName(filePath);
                var file = packFileService.FindFile(filePath);
                var bytes = file.DataSource.ReadData();
                var audioProjectJson = Encoding.UTF8.GetString(bytes);

                // Reset and initialise data.
                audioEditorViewModel.ResetAudioProjectConfiguration();
                audioEditorViewModel.ResetAudioEditorViewModelData();
                ResetAudioProject();
                audioEditorViewModel.InitialiseCollections();

                // Set the AudioProject.
                AudioProject = JsonSerializer.Deserialize<AudioProjectData>(audioProjectJson);

                // Update AudioProjectTreeViewItems.
                audioEditorViewModel.UpdateAudioProjectTreeViewItems();

                // Get the Modded States and prepare them for being added to the DataGrid ComboBoxes.
                GetModdedStates(AudioProject.ModdedStates, StateGroupsWithCustomStates);

                _logger.Here().Information($"Loaded Audio Project: {fileName}");

                if (audioEditorViewModel.AudioEditorVisibility == false)
                    audioEditorViewModel.SetAudioEditorVisibility(true);
            }
        }

        public void InitialiseAudioProject(string fileName, string directory, string language)
        {
            AudioProject.FileName = fileName;
            AudioProject.Directory = directory;
            AudioProject.Language = language;
        }

        public void ResetAudioProject()
        {
            AudioProject = new AudioProjectData();
        }
    }
}
