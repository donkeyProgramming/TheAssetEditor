using System.Collections.Generic;
using static Editors.Audio.AudioEditor.StatesProjectData;
using static Editors.Audio.AudioEditor.VOProjectData;

namespace Editors.Audio.AudioEditor
{
    public class AudioProject
    {
        public enum ProjectType
        {
            sfxproject,
            voproject,
            statesproject
        }

        private static readonly AudioProject _instance = new();

        public static AudioProject AudioProjectInstance => _instance;

        public VOProject VOProject { get; set; } = new VOProject();

        public StatesProject StatesProject { get; set; } = new StatesProject();

        public static Dictionary<string, Dictionary<string, string>> DialogueEventsWithStateGroupsWithQualifiers { get; set; } = new();

        public Dictionary<string, List<string>> StateGroupsWithCustomStates { get; set; } = new();

        public ProjectType Type { get; set; }

        public string FileName { get; set; }

        public string Directory { get; set; }

        public string SelectedAudioProjectEvent { get; set; }

        public string PreviousSelectedAudioProjectEvent { get; set; }

        private AudioProject()
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

        public void ResetAudioProjectData()
        {
            VOProject = null;
            StateGroupsWithCustomStates.Clear();
            SelectedAudioProjectEvent = null;
            PreviousSelectedAudioProjectEvent = null;
        }
    }
}
