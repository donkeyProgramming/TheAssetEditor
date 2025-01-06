using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Editors.Audio.AudioEditor.AudioProject
{
    public abstract class IAudioProjectItem : ObservableObject
    {
        public string Name { get; set; }
    }

    public partial class SoundBank : IAudioProjectItem
    {
        [ObservableProperty] public string _filteredBy;
        public string Type { get; set; }
        public ObservableCollection<ActionEvent> ActionEvents { get; set; } = [];
        public ObservableCollection<DialogueEvent> DialogueEvents { get; set; } = [];
        public ObservableCollection<MusicEvent> MusicEvents { get; set; } = [];
        [JsonIgnore] public ObservableCollection<object> SoundBankTreeViewItems { get; set; } = [];
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
        public string Language { get; set; }
        public ObservableCollection<SoundBank> SoundBanks { get; set; } = [];
        public ObservableCollection<StateGroup> ModdedStates { get; set; } = [];
        [JsonIgnore] public ObservableCollection<object> AudioProjectTreeViewItems { get; set; } = [];
    }        
}
