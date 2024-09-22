using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using static Editors.Audio.AudioEditor.AudioEditorSettings;

namespace Editors.Audio.AudioEditor
{
    public abstract class IAudioProjectItem
    {
        public string Name { get; set; }
    }

    public class SoundBank : IAudioProjectItem
    {
        public string Type { get; set; }
        public ObservableCollection<ActionEvent> ActionEvents { get; set; }

        public ObservableCollection<DialogueEvent> DialogueEvents { get; set; }

        public ObservableCollection<MusicEvent> MusicEvents { get; set; }

        [JsonIgnore] public ObservableCollection<object> SoundBankTreeViewItems
        {
            get
            {
                var treeViewItems = new ObservableCollection<object>();

                // Other SounBank objects e.g. ActionEvents and MusicEvents are not present as they don't currently need to be displayed in the TreeView, only their SoundBank does.
                if (DialogueEvents != null)
                {
                    foreach (var dialogueEvent in DialogueEvents)
                        treeViewItems.Add(dialogueEvent);
                }    

                return treeViewItems;
            }
        }
    }

    public class ActionEvent : IAudioProjectItem
    {
        public List<string> AudioFiles { get; set; } = [];
        public string AudioFilesDisplay { get; set; }
    }

    public class DialogueEvent : IAudioProjectItem
    {
        public List<DecisionNode> DecisionTree { get; set; } = [];
    }

    public class MusicEvent : IAudioProjectItem
    {
        public List<string> AudioFiles { get; set; } = [];
        public string AudioFilesDisplay { get; set; }
    }

    public class StateGroup : IAudioProjectItem 
    {
        public List<State> States { get; set; } = [];
    }

    public class State : IAudioProjectItem { }

    public class DecisionNode
    {
        public StatePath StatePath { get; set; }
        public List<string> AudioFiles { get; set; } = [];
        public string AudioFilesDisplay { get; set; }
    }

    public class StatePath
    {
        public List<StatePathNode> Nodes { get; set; } = [];
    }

    public class StatePathNode
    {
        public StateGroup StateGroup { get; set; }
        public State State { get; set; }
    }

    public class AudioProjectData
    {
        public string FileName { get; set; }
        public string Directory { get; set; }
        public string Language { get; set; }
        public ObservableCollection<SoundBank> SoundBanks { get; set; } = [];
        public ObservableCollection<StateGroup> ModdedStates { get; set; } = [];
        [JsonIgnore] public ObservableCollection<object> AudioProjectTreeViewItems { get; set; } = [];

        public static void InitialiseModdedStatesGroups(ObservableCollection<StateGroup> moddedStates, ObservableCollection<object> treeViewItems)
        {
            foreach (var moddedStateGroup in ModdedStateGroups)
            {
                var stateGroup = new StateGroup { Name = moddedStateGroup };
                moddedStates.Add(stateGroup);
            }
        }
    }        
}
