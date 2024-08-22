using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Editors.Audio.AudioEditor.ViewModels;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using static Editors.Audio.AudioEditor.AudioProject;

namespace Editors.Audio.AudioEditor
{
    public static class AudioEditorHelpers
    {
        readonly static ILogger s_logger = Logging.Create<AudioEditorViewModel>();

        public static void AddToPackFile(PackFileService packFileService, object file, string fileName, string directory, ProjectType? fileType)
        {
            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true
            };

            var fileJson = JsonSerializer.Serialize(file, options);
            var pack = packFileService.GetEditablePack();
            var byteArray = Encoding.ASCII.GetBytes(fileJson);

            packFileService.AddFileToPack(pack, directory, new PackFile($"{fileName}.{fileType}", new MemorySource(byteArray)));
            s_logger.Here().Information($"Saved Audio Project file: {directory}\\{fileName}.{fileType}");
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

        // Add qualifiers to State Groups so that dictionary keys are unique as some events have the same State Group twice e.g. VO_Actor
        public static void AddQualifiersToStateGroups(Dictionary<string, List<string>> dialogueEventsWithStateGroups)
        {
            DialogueEventsWithStateGroupsWithQualifiers = new Dictionary<string, Dictionary<string, string>>();

            foreach (var dialogueEvent in dialogueEventsWithStateGroups)
            {
                var stateGroupsWithQualifiers = new Dictionary<string, string>();
                var stateGroups = dialogueEvent.Value;

                var voActorCount = 0;
                var voCultureCount = 0;

                foreach (var stateGroup in stateGroups)
                {
                    if (stateGroup == "VO_Actor")
                    {
                        voActorCount++;

                        var qualifier = voActorCount > 1 ? "VO_Actor (Reference)" : "VO_Actor (Source)";
                        stateGroupsWithQualifiers[qualifier] = "VO_Actor";
                    }

                    else if (stateGroup == "VO_Culture")
                    {
                        voCultureCount++;

                        var qualifier = voCultureCount > 1 ? "VO_Culture (Reference)" : "VO_Culture (Source)";
                        stateGroupsWithQualifiers[qualifier] = "VO_Culture";
                    }

                    else
                    {
                        // No qualifier needed, add the same state group as both original and qualified
                        stateGroupsWithQualifiers[stateGroup] = stateGroup;
                    }
                }

                DialogueEventsWithStateGroupsWithQualifiers[dialogueEvent.Key] = stateGroupsWithQualifiers;
            }
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
