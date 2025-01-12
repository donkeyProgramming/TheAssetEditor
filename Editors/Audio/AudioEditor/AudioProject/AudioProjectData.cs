using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using static Editors.Audio.AudioEditor.AudioSettings.AudioSettings;

namespace Editors.Audio.AudioEditor.AudioProject
{
    public class AudioProjectData
    {
        public string Language { get; set; }
        public ObservableCollection<SoundBank> SoundBanks { get; set; } = [];
        public ObservableCollection<StateGroup> ModdedStates { get; set; } = [];
        [JsonIgnore] public ObservableCollection<object> AudioProjectTreeViewItems { get; set; } = [];
    }

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
        [JsonIgnore] public ObservableCollection<object> SoundBankTreeViewItems { get; set; } = [];
    }

    public class ActionEvent : IAudioProjectItem
    {
        public List<string> AudioFiles { get; set; } = [];
        public string AudioFilesDisplay { get; set; }
        public AudioSettings AudioSettings { get; set; }
    }

    public class DialogueEvent : IAudioProjectItem
    {
        public List<StatePath> DecisionTree { get; set; } = [];
    }

    public class StateGroup : IAudioProjectItem 
    {
        public List<State> States { get; set; } = [];
    }

    public class State : IAudioProjectItem { }

    public class StatePath
    {
        public List<StatePathNode> Nodes { get; set; } = [];
        public List<string> AudioFiles { get; set; } = [];
        public string AudioFilesDisplay { get; set; }
        public AudioSettings AudioSettings { get; set; }
    }

    public class StatePathNode
    {
        public StateGroup StateGroup { get; set; }
        public State State { get; set; }
    }

    public class AudioSettings
    {
        public PlaylistType PlaylistType { get; set; }
        public decimal Volume { get; set; }
        public decimal InitialDelay { get; set; }
        public PlaylistMode PlaylistMode { get; set; }
        public bool EnableRepetitionInterval { get; set; }
        public uint RepetitionInterval { get; set; }
        public EndBehaviour EndBehaviour { get; set; }
        public bool EnableLooping { get; set; }
        public bool ILoopInfinitely { get; set; }
        public uint NumberOfLoops { get; set; }
        public bool EnableTransitions { get; set; }
        public TransitionType Transition { get; set; }
        public decimal Duration { get; set; }
    }
}
