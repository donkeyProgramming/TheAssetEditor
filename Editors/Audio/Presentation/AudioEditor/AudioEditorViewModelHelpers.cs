using System.Collections.Generic;
using Editors.Audio.Presentation.AudioEditor.ViewModels;
using Editors.Audio.Storage;
using Serilog;
using Shared.Core.ErrorHandling;
using static Editors.Audio.Presentation.AudioEditor.DynamicDataGrid;
using static Editors.Audio.Presentation.AudioEditor.ViewModels.AudioEditorViewModel;

namespace Editors.Audio.Presentation.AudioEditor
{
    public static class AudioEditorViewModelHelpers
    {
        static readonly ILogger s_logger = Logging.Create<AudioEditorViewModel>();

        public static Dictionary<string, List<string>> DialogueEventsWithStateGroupsWithQualifiers { get; set; } = [];

        public static void InitialiseEventsData(AudioEditorViewModel viewModel)
        {
            CreateAudioProjectEventsList(viewModel);

            foreach (var dialogueEvent in viewModel.AudioProjectDialogueEvents)
            {
                var stateGroupsWithQualifiers = DialogueEventsWithStateGroupsWithQualifiers[dialogueEvent];

                var dataGridItems = new List<Dictionary<string, object>>();
                var dataGridItem = new Dictionary<string, object>();

                foreach (var stateGroupWithQualifier in stateGroupsWithQualifiers)
                {
                    var stateGroupKey = AddExtraUnderScoresToStateGroup(stateGroupWithQualifier);
                    dataGridItem[stateGroupKey] = "";
                    dataGridItem["AudioFilesDisplay"] = "";
                }

                dataGridItems.Add(dataGridItem);
                EventsData[dialogueEvent] = dataGridItems;
            }
        }

        public static void LoadEvent(AudioEditorViewModel viewModel, IAudioRepository audioRepository, bool showCustomStatesOnly)
        {
            if (string.IsNullOrEmpty(AudioEditorData.Instance.SelectedAudioProjectEvent))
                return;

            s_logger.Here().Information($"Loading event: {AudioEditorData.Instance.SelectedAudioProjectEvent}");

            ConfigureDataGrid(viewModel, audioRepository, showCustomStatesOnly);

            if (EventsData.ContainsKey(AudioEditorData.Instance.SelectedAudioProjectEvent))

                foreach (var statePath in EventsData[AudioEditorData.Instance.SelectedAudioProjectEvent])
                    viewModel.AudioEditorDataGridItems.Add(statePath);
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

        public static void UpdateEventDataWithCurrentEvent(AudioEditorViewModel viewModel)
        {
            if (viewModel.AudioEditorDataGridItems == null)
                return;

            if (viewModel.SelectedAudioProjectEvent != null)
                EventsData[viewModel.SelectedAudioProjectEvent] = new List<Dictionary<string, object>>(viewModel.AudioEditorDataGridItems);
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
        public static string AddExtraUnderScoresToStateGroup(string stateGroupWithQualifier)
        {
            return stateGroupWithQualifier.Replace("_", "__");
        }

        public static void UpdateAudioProjectEventSubType(AudioEditorViewModel viewModel)
        {
            viewModel.AudioProjectSubtypes.Clear();

            if (viewModel.SelectedAudioProjectEventType == "Non-VO")
                foreach (var item in AudioEditorSettings.NonVO)
                    viewModel.AudioProjectSubtypes.Add(item);

            else if (viewModel.SelectedAudioProjectEventType == "Frontend VO")
                foreach (var item in AudioEditorSettings.FrontendVO)
                    viewModel.AudioProjectSubtypes.Add(item);

            else if (viewModel.SelectedAudioProjectEventType == "Campaign VO")
                foreach (var item in AudioEditorSettings.CampaignVO)
                    viewModel.AudioProjectSubtypes.Add(item);

            else if (viewModel.SelectedAudioProjectEventType == "Battle VO")
                foreach (var item in AudioEditorSettings.BattleVO)
                    viewModel.AudioProjectSubtypes.Add(item);
        }

        public static void CreateAudioProjectEventsListFromAudioProject(AudioEditorViewModel viewModel, Dictionary<string, List<Dictionary<string, object>>> eventsData)
        {
            viewModel.AudioProjectDialogueEvents.Clear();

            foreach (var dialogueEvent in eventsData.Keys)
                viewModel.AudioProjectDialogueEvents.Add(dialogueEvent);
        }
















        // STILL NEED TO FINISH THIS
        public static void CreateAudioProjectEventsList(AudioEditorViewModel viewModel)
        {
            viewModel.AudioProjectDialogueEvents.Clear();

            if (viewModel.SelectedAudioProjectEventType == "Frontend VO"
                && viewModel.SelectedAudioProjectEventSubtype == "Lord"
                && (viewModel.SelectedAudioProjectEventsPreset == DialogueEventsPreset.All || viewModel.SelectedAudioProjectEventsPreset == DialogueEventsPreset.Essential))
            {
                AddDialogueEventAudioProjectEvents(viewModel, AudioEditorSettings.FrontendVODialogueEventsAll);
            }


            if (viewModel.SelectedAudioProjectEventType == "Campaign VO" && viewModel.SelectedAudioProjectEventSubtype == "Lord")
            {
                if (viewModel.SelectedAudioProjectEventsPreset == DialogueEventsPreset.All)
                    AddDialogueEventAudioProjectEvents(viewModel, AudioEditorSettings.CampaignVODialogueEventsAll);

                else
                {

                }
            }

            if (viewModel.SelectedAudioProjectEventType == "Campaign VO" && viewModel.SelectedAudioProjectEventSubtype == "Hero")
            {
                if (viewModel.SelectedAudioProjectEventsPreset == DialogueEventsPreset.All)
                    AddDialogueEventAudioProjectEvents(viewModel, AudioEditorSettings.CampaignVODialogueEventsAll);

                else
                {

                }
            }
        }

        public static void AddDialogueEventAudioProjectEvents(AudioEditorViewModel viewModel, List<string> displayData)
        {
            foreach (var dialogueEvent in displayData)
                viewModel.AudioProjectDialogueEvents.Add(dialogueEvent);
        }

        public class DictionaryEqualityComparer<TKey, TValue> : IEqualityComparer<Dictionary<TKey, TValue>>
        {
            public static readonly DictionaryEqualityComparer<TKey, TValue> Default = new DictionaryEqualityComparer<TKey, TValue>();

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
