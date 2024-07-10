using System.Collections.Generic;
using System.Diagnostics;
using Editors.Audio.BnkCompiler;
using Editors.Audio.Presentation.AudioEditor.ViewModels;
using Editors.Audio.Storage;
using static Editors.Audio.Presentation.AudioEditor.DynamicDataGrid;
using static Editors.Audio.Presentation.AudioEditor.ViewModels.AudioEditorViewModel;

namespace Editors.Audio.Presentation.AudioEditor
{
    public class AudioEditorViewModelHelpers
    {
        public static Dictionary<string, List<string>> DialogueEventsWithStateGroupsWithQualifiers { get; set; } = [];

        public AudioEditorViewModelHelpers()
        {
        }

        public static void InitialiseEventData(AudioEditorViewModel viewModel)
        {
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

        public static void LoadEvent(AudioEditorViewModel viewModel, bool isLoadingAudioProject, IAudioRepository audioRepository)
        {
            if (string.IsNullOrEmpty(AudioEditorData.Instance.SelectedAudioProjectEvent))
                return;

            Debug.WriteLine($"Loading event: {AudioEditorData.Instance.SelectedAudioProjectEvent}");

            if (!isLoadingAudioProject)
                UpdateEventDataWithPreviousEvent(viewModel);

            ConfigureDataGrid(viewModel, audioRepository);

            if (EventsData.ContainsKey(AudioEditorData.Instance.SelectedAudioProjectEvent))

                foreach (var statePath in EventsData[AudioEditorData.Instance.SelectedAudioProjectEvent])
                    viewModel.AudioEditorDataGridItems.Add(statePath);
        }

        public static void UpdateEventDataWithPreviousEvent(AudioEditorViewModel viewModel)
        {
            if (viewModel.AudioEditorDataGridItems == null)
                return;

            if (viewModel._previousSelectedAudioProjectEvent != null)
                EventsData[viewModel._previousSelectedAudioProjectEvent] = new List<Dictionary<string, object>>(viewModel.AudioEditorDataGridItems);
        }

        public static void UpdateEventDataWithCurrentEvent(AudioEditorViewModel viewModel)
        {
            if (viewModel.AudioEditorDataGridItems == null)
                return;

            if (viewModel._selectedAudioProjectEvent != null)
                EventsData[viewModel._selectedAudioProjectEvent] = new List<Dictionary<string, object>>(viewModel.AudioEditorDataGridItems);
        }

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

        public static string AddExtraUnderScoresToStateGroup(string stateGroupWithQualifier)
        {
            return stateGroupWithQualifier.Replace("_", "__"); // Apparently WPF doesn't_like_underscores.
        }

        public static void UpdateAudioProjectEventSubType(AudioEditorViewModel viewModel)
        {
            var audioProjectSettings = new AudioEditorSettings();
            viewModel.AudioProjectSubTypes.Clear();

            if (viewModel.SelectedAudioProjectEventType == "Non-VO")
                foreach (var item in audioProjectSettings.NonVO)
                    viewModel.AudioProjectSubTypes.Add(item);

            else if (viewModel.SelectedAudioProjectEventType == "Frontend VO")
                foreach (var item in audioProjectSettings.FrontendVO)
                    viewModel.AudioProjectSubTypes.Add(item);

            else if (viewModel.SelectedAudioProjectEventType == "Campaign VO")
                foreach (var item in audioProjectSettings.CampaignVO)
                    viewModel.AudioProjectSubTypes.Add(item);

            else if (viewModel.SelectedAudioProjectEventType == "Battle VO")
                foreach (var item in audioProjectSettings.BattleVO)
                    viewModel.AudioProjectSubTypes.Add(item);

            Debug.WriteLine($"AudioProjectSubTypes changed to: {string.Join(", ", viewModel.AudioProjectSubTypes)}");
        }

        public static void CreateAudioProjectDialogueEventsListFromAudioProject(AudioEditorViewModel viewModel, Dictionary<string, List<Dictionary<string, object>>> eventData)
        {
            viewModel.AudioProjectDialogueEvents.Clear();

            foreach (var dialogueEvent in eventData.Keys)
                viewModel.AudioProjectDialogueEvents.Add(dialogueEvent);
        }
















        // STILL NEED TO FINISH THIS
        public static void CreateAudioProjectDialogueEventsList(AudioEditorViewModel viewModel)
        {
            viewModel.AudioProjectDialogueEvents.Clear();

            if (viewModel.SelectedAudioProjectEventType == "Frontend VO"
                && viewModel.SelectedAudioProjectEventSubtype == "Lord"
                && (viewModel.SelectedDialogueEventsPreset == DialogueEventsPreset.All || viewModel.SelectedDialogueEventsPreset == DialogueEventsPreset.Essential))
            {
                AddDialogueEventAudioProjectDialogueEvents(viewModel, AudioEditorSettings.FrontendVODialogueEventsAll);
            }


            if (viewModel.SelectedAudioProjectEventType == "Campaign VO" && viewModel.SelectedAudioProjectEventSubtype == "Lord")
            {
                if (viewModel.SelectedDialogueEventsPreset == DialogueEventsPreset.All)
                    AddDialogueEventAudioProjectDialogueEvents(viewModel, AudioEditorSettings.CampaignVODialogueEventsAll);

                else
                {

                }
            }

            if (viewModel.SelectedAudioProjectEventType == "Campaign VO" && viewModel.SelectedAudioProjectEventSubtype == "Hero")
            {
                if (viewModel.SelectedDialogueEventsPreset == DialogueEventsPreset.All)
                    AddDialogueEventAudioProjectDialogueEvents(viewModel, AudioEditorSettings.CampaignVODialogueEventsAll);

                else
                {

                }
            }
        }

        public static void AddDialogueEventAudioProjectDialogueEvents(AudioEditorViewModel viewModel, List<string> displayData)
        {
            foreach (var dialogueEvent in displayData)
                viewModel.AudioProjectDialogueEvents.Add(dialogueEvent);
        }

        public class DictionaryEqualityComparer<TKey, TValue> : IEqualityComparer<Dictionary<TKey, TValue>>
        {
            public static readonly DictionaryEqualityComparer<TKey, TValue> Default = new DictionaryEqualityComparer<TKey, TValue>();

            public bool Equals(Dictionary<TKey, TValue> x, Dictionary<TKey, TValue> y)
            {
                // Check if dictionaries have the same number of elements
                if (x.Count != y.Count)
                    return false;

                // Check each key-value pair
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
