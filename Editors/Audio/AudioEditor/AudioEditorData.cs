using System.Collections.Generic;
using static Editors.Audio.AudioEditor.AudioProjectData;

namespace Editors.Audio.AudioEditor
{
    public class AudioEditorData
    {
        private static readonly AudioEditorData _instance = new();

        public static AudioEditorData AudioEditorInstance => _instance;

        public static Dictionary<string, Dictionary<string, string>> DialogueEventsWithStateGroupsWithQualifiers { get; set; } = new();

        public AudioProject AudioProject { get; set; } = new AudioProject();

        public string AudioProjectFileName { get; set; }

        public string AudioProjectDirectory { get; set; }

        public Dictionary<string, List<string>> StateGroupsWithCustomStates { get; set; } = new();

        public string SelectedAudioProjectEvent { get; set; }

        public string PreviousSelectedAudioProjectEvent { get; set; }


        private AudioEditorData()
        {
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

        public void ResetAudioEditorData()
        {
            AudioProject = null;
            StateGroupsWithCustomStates.Clear();
            SelectedAudioProjectEvent = null;
            PreviousSelectedAudioProjectEvent = null;
        }
    }
}
