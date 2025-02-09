using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using static Editors.Audio.AudioEditor.AudioSettings.AudioSettings;
using static Editors.Audio.GameSettings.Warhammer3.SoundBanks;

namespace Editors.Audio.AudioEditor.Data
{

    // TODO: Move audio files into audio settings.

    public class AudioProjectDataModel
    {
        public string Language { get; set; }
        public ObservableCollection<SoundBank> SoundBanks { get; set; }
        public ObservableCollection<StateGroup> StateGroups { get; set; }
    }

    public abstract class AudioProjectItem : ObservableObject
    {
        public string Name { get; set; }
    }

    public partial class SoundBank : AudioProjectItem
    {
        [JsonIgnore] public Wh3SoundBankType Type { get; set; }
        public ObservableCollection<ActionEvent> ActionEvents { get; set; }
        public ObservableCollection<DialogueEvent> DialogueEvents { get; set; }
    }

    public class ActionEvent : AudioProjectItem
    {
        public List<string> AudioFiles { get; set; }
        public string AudioFilesDisplay { get; set; }
        public AudioSettings AudioSettings { get; set; }
    }

    public class DialogueEvent : AudioProjectItem
    {
        public List<StatePath> DecisionTree { get; set; }
    }

    public class StateGroup : AudioProjectItem
    {
        public List<State> States { get; set; }
    }

    public class State : AudioProjectItem { }

    public class StatePath
    {
        public List<StatePathNode> Nodes { get; set; } = [];
        public List<string> AudioFiles { get; set; }
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
