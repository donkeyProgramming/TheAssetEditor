using System.Collections.Generic;
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

        public static Dictionary<string, string> ValidateStateGroupsOrder(Dictionary<string, object> dataGridItem, string selectedAudioProjectEvent)
        {
            var stateGroupsAndStates = new Dictionary<string, string>();
            var stateGroupsWithQualifiers = DialogueEventsWithStateGroupsWithQualifiers[selectedAudioProjectEvent];
            var orderedStateGroupsAndStates = new Dictionary<string, string>();

            foreach (var kvp in dataGridItem)
            {
                var key = kvp.Key;
                var value = kvp.Value.ToString();

                if (key != "AudioFiles" && key != "AudioFilesDisplay" && key != "StatePath") // access only the State Group data items as they contain the States data.
                {
                    var stateGroupWithQualifier = RemoveExtraUnderscoresFromString(key);
                    var state = value;

                    var stateGroup = stateGroupsWithQualifiers[stateGroupWithQualifier];
                    stateGroupsAndStates[stateGroup] = state;
                }
            }

            foreach (var kvp in stateGroupsWithQualifiers)
            {
                var stateGroup = kvp.Value;

                if (stateGroupsAndStates.ContainsKey(stateGroup))
                    orderedStateGroupsAndStates[stateGroup] = stateGroupsAndStates[stateGroup];
            }

            return orderedStateGroupsAndStates;
        }

        // Apparently WPF doesn't_like_underscores so double them up in order for them to be displayed in the UI.
        public static string AddExtraUnderscoresToString(string wtfWPF)
        {
            return wtfWPF.Replace("_", "__");
        }

        public static string RemoveExtraUnderscoresFromString(string wtfWPF)
        {
            return wtfWPF.Replace("__", "_");
        }
    }
}
