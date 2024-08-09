using System.Collections.Generic;
using System.Linq;
using Editors.Audio.AudioEditor.ViewModels;
using Editors.Audio.Storage;
using Serilog;
using Shared.Core.ErrorHandling;
using static Editors.Audio.AudioEditor.DynamicDataGrid;
using static Editors.Audio.AudioEditor.ViewModels.AudioEditorViewModel;

namespace Editors.Audio.AudioEditor
{
    public static class AudioEditorViewModelHelpers
    {
        public static Dictionary<string, List<string>> DialogueEventsWithStateGroupsWithQualifiers { get; set; } = [];

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

        public static void PrepareCustomStatesForComboBox(AudioEditorViewModel viewModel)
        {
            if (viewModel.CustomStatesDataGridItems == null)
                return;

            var stateGroupsWithCustomStates = AudioEditorData.Instance.StateGroupsWithCustomStates;
            stateGroupsWithCustomStates.Clear();

            stateGroupsWithCustomStates["VO_Actor"] = new List<string>();
            stateGroupsWithCustomStates["VO_Culture"] = new List<string>();
            stateGroupsWithCustomStates["VO_Battle_Selection"] = new List<string>();
            stateGroupsWithCustomStates["VO_Battle_Special_Ability"] = new List<string>();
            stateGroupsWithCustomStates["VO_Faction_Leader"] = new List<string>();

            foreach (var item in viewModel.CustomStatesDataGridItems)
            {
                stateGroupsWithCustomStates["VO_Actor"].Add(item.CustomVOActor);
                stateGroupsWithCustomStates["VO_Culture"].Add(item.CustomVOCulture);
                stateGroupsWithCustomStates["VO_Battle_Selection"].Add(item.CustomVOBattleSelection);
                stateGroupsWithCustomStates["VO_Battle_Special_Ability"].Add(item.CustomVOBattleSpecialAbility);
                stateGroupsWithCustomStates["VO_Faction_Leader"].Add(item.CustomVOFactionLeader);
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
