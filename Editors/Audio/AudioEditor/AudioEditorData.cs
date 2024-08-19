using System;
using System.Collections.Generic;
using static Editors.Audio.AudioEditor.AudioProjectData;

namespace Editors.Audio.AudioEditor
{
    public class AudioEditorData
    {
        private static readonly AudioEditorData _instance = new();

        public static AudioEditorData AudioEditorInstance => _instance;

        public static Dictionary<string, List<Tuple<string, string>>> DialogueEventsWithStateGroupsWithQualifiers { get; set; } = new();

        public string AudioProjectFileName { get; set; }

        public AudioProject AudioProject { get; set; } = new AudioProject();

        public Dictionary<string, List<string>> StateGroupsWithCustomStates { get; set; } = new();

        public string SelectedAudioProjectEvent { get; set; }

        private AudioEditorData()
        {
        }

        // Add qualifiers to State Groups so that dictionary keys are unique as some events have the same State Group twice e.g. VO_Actor
        public static void AddQualifiersToStateGroups(Dictionary<string, List<string>> dialogueEventsWithStateGroups)
        {
            DialogueEventsWithStateGroupsWithQualifiers = new Dictionary<string, List<Tuple<string, string>>>();

            foreach (var dialogueEvent in dialogueEventsWithStateGroups)
            {
                DialogueEventsWithStateGroupsWithQualifiers[dialogueEvent.Key] = new List<Tuple<string, string>>();
                var stateGroups = DialogueEventsWithStateGroupsWithQualifiers[dialogueEvent.Key];

                var voActorCount = 0;
                var voCultureCount = 0;

                foreach (var stateGroup in dialogueEvent.Value)
                {
                    if (stateGroup == "VO_Actor")
                    {
                        voActorCount++;

                        if (voActorCount > 1)
                            stateGroups.Add(new Tuple<string, string>("VO_Actor", "VO_Actor (Reference)"));
                        else
                            stateGroups.Add(new Tuple<string, string>("VO_Actor", "VO_Actor (Source)"));
                    }
                    else if (stateGroup == "VO_Culture")
                    {
                        voCultureCount++;

                        if (voCultureCount > 1)
                            stateGroups.Add(new Tuple<string, string>("VO_Culture", "VO_Culture (Reference)"));
                        else
                            stateGroups.Add(new Tuple<string, string>("VO_Culture", "VO_Culture (Source)"));
                    }
                    else
                    {
                        // No qualifier needed, add the same state group as both original and qualified
                        stateGroups.Add(new Tuple<string, string>(stateGroup, stateGroup));
                    }
                }
            }
        }

        public void ResetAudioEditorData()
        {
            AudioProject = null;
            StateGroupsWithCustomStates.Clear();
        }
    }
}
