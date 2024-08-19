using System.Text;
using System.Text.Json;
using Editors.Audio.AudioEditor.ViewModels;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using static Editors.Audio.AudioEditor.AudioEditorData;

namespace Editors.Audio.AudioEditor
{
    public static class AudioEditorHelpers
    {
        readonly static ILogger s_logger = Logging.Create<AudioEditorViewModel>();

        public static void AddAudioProjectToPackFile(PackFileService packFileService)
        {
            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true
            };

            var audioProject = JsonSerializer.Serialize(AudioEditorInstance.AudioProject, options);

            var pack = packFileService.GetEditablePack();
            var byteArray = Encoding.ASCII.GetBytes(audioProject);

            var audioProjectFileName = AudioEditorInstance.AudioProject.Settings.AudioProjectName;
            
            packFileService.AddFileToPack(pack, "AudioProjects", new PackFile($"{audioProjectFileName}.audioproject", new MemorySource(byteArray)));
            s_logger.Here().Information($"Saved Audio Project file: {audioProjectFileName}.audioproject");
        }

        // Apparently WPF doesn't_like_underscores so double them up in order for them to be displayed in the UI.
        public static string AddExtraUnderScoresToString(string wtfWPF)
        {
            return wtfWPF.Replace("_", "__");
        }

        public static string RemoveExtraUnderScoresFromString(string wtfWPF)
        {
            return wtfWPF.Replace("__", "_");
        }
    }
}
