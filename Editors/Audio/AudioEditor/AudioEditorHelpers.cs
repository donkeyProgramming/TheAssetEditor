using System.Collections.Generic;
using System.Text;
using Editors.Audio.AudioEditor.ViewModels;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using static Editors.Audio.AudioEditor.AudioEditorData;
using static Editors.Audio.AudioEditor.AudioProjectData;


namespace Editors.Audio.AudioEditor
{
    public static class AudioEditorHelpers
    {
        readonly static ILogger s_logger = Logging.Create<AudioEditorViewModel>();
        public static string AudioProjectFileName => AudioEditorInstance.AudioProjectFileName;
        public static Dictionary<string, List<string>> DialogueEventsWithStateGroupsWithQualifiers { get; set; } = [];

        public static void AddAudioProjectToPackFile(PackFileService packFileService)
        {
            var audioProject = ConvertToVOAudioProject(AudioEditorInstance.AudioProjectData);
            var pack = packFileService.GetEditablePack();
            var byteArray = Encoding.ASCII.GetBytes(audioProject);
            packFileService.AddFileToPack(pack, "AudioProjects", new PackFile($"{GetAudioProjectFileName()}.audioproject", new MemorySource(byteArray)));
            s_logger.Here().Information($"Saved Audio Project file: {GetAudioProjectFileName()}.audioproject");
        }

        public static string GetAudioProjectFileName()
        {
            var settingsList = AudioEditorInstance.AudioProjectData["Settings"];
            var settings = settingsList[0];
            return settings["AudioProjectFileName"].ToString();
        }

        public static string GetCustomStatesFilePath()
        {
            var settingsList = AudioEditorInstance.AudioProjectData["Settings"];
            var settings = settingsList[0];
            return settings["CustomStatesFilePath"].ToString();
        }

        // Add qualifiers to State Groups so that dictionary keys are unique as some events have the same State Group twice e.g. VO_Actor
        public static void AddQualifiersToStateGroups(Dictionary<string, List<string>> dialogueEventsWithStateGroups)
        {
            DialogueEventsWithStateGroupsWithQualifiers = new Dictionary<string, List<string>>();

            foreach (var dialogueEvent in dialogueEventsWithStateGroups)
            {
                DialogueEventsWithStateGroupsWithQualifiers[dialogueEvent.Key] = new List<string>();
                var stateGroups = DialogueEventsWithStateGroupsWithQualifiers[dialogueEvent.Key];

                var voActorCount = 0;
                var voCultureCount = 0;

                foreach (var stateGroup in dialogueEvent.Value)
                {
                    if (stateGroup == "VO_Actor")
                    {
                        voActorCount++;

                        if (voActorCount > 1)
                            stateGroups.Add($"VO_Actor (Reference)");

                        else
                            stateGroups.Add("VO_Actor (Source)");
                    }

                    else if (stateGroup == "VO_Culture")
                    {
                        voCultureCount++;

                        if (voCultureCount > 1)
                            stateGroups.Add($"VO_Culture (Reference)");

                        else
                            stateGroups.Add("VO_Culture (Source)");
                    }

                    else
                        stateGroups.Add(stateGroup);
                }
            }
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

        public class DictionaryEqualityComparer<TKey, TValue> : IEqualityComparer<Dictionary<TKey, TValue>>
        {
            public static readonly DictionaryEqualityComparer<TKey, TValue> Default = new();

            public bool Equals(Dictionary<TKey, TValue> x, Dictionary<TKey, TValue> y)
            {
                if (x.Count != y.Count)
                    return false;

                foreach (var kvp in x)
                {
                    if (!y.TryGetValue(kvp.Key, out var value) || !EqualityComparer<TValue>.Default.Equals(kvp.Value, value))
                        return false;
                }

                return true;
            }

            public int GetHashCode(Dictionary<TKey, TValue> obj)
            {
                var hash = 17;
                foreach (var kvp in obj)
                {
                    hash = hash * 31 + (kvp.Key?.GetHashCode() ?? 0);
                    hash = hash * 31 + (kvp.Value?.GetHashCode() ?? 0);
                }
                return hash;
            }
        }
    }
}
