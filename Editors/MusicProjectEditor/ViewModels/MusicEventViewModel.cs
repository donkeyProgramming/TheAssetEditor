using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Editors.Audio.Shared.AudioProject.Models;
using Editors.Audio.Shared.GameInformation.Warhammer3;

namespace Editors.MusicProjectEditor.ViewModels
{
    /// <summary>One slot a culture is wired into, as the music project sees it: the event the
    /// scripts will post for it, which State that event sets, and the wavs that State plays.
    ///
    /// A borrowing slot posts another culture's event, so it has nothing of its own to show
    /// beyond who it borrows from.</summary>
    public partial class MusicEventViewModel : ObservableObject
    {
        public MusicCultureSlot Slot { get; }

        /// <summary>What the slot is for, from the script's own culture chains - or the event
        /// prefix when the game's scripts could not be read to say.</summary>
        public string Title { get; }

        /// <summary>The event the project holds for this slot, or null when it borrows.</summary>
        public string? EventName { get; }

        public string? StateGroupName { get; }
        public string? StateName { get; }

        /// <summary>The State Group every culture's event for this slot sets, borrowing or not -
        /// what the vanilla reference panel lists cultures from.</summary>
        public string? SlotStateGroupName { get; }

        public bool IsBorrowed => Slot.ReferenceMatchKey != null;
        public bool CanCarryOwnAudio { get; }

        public ObservableCollection<string> AudioFiles { get; } = [];

        [ObservableProperty] string _audioSummary = "";

        public string EventText => IsBorrowed ? $"{Slot.EventPrefix}{Slot.ReferenceMatchKey} (borrowed)" : EventName ?? "";

        public string Note
        {
            get
            {
                if (IsBorrowed)
                    return $"Borrows '{Slot.ReferenceMatchKey}''s music, so it posts that culture's event and there is nothing to add here.";

                if (StateGroupName == null)
                    return "This event's name matches no vanilla music event pattern, so there is no telling which State it sets.";

                if (!CanCarryOwnAudio)
                    return "This event can only select music that already exists - nothing in the game branches on its " +
                           "State Group in a way a mod can extend, so it cannot be given wavs of its own. Edit the culture " +
                           "to borrow an existing culture's music here instead.";

                // These layer over the campaign score and follow its key, which is what to tell a modder
                // before they pick a five minute track for one.
                if (Wh3MusicHierarchyInformation.IsAmsPulseStateGroup(StateGroupName))
                    return "An adaptive music pulse - a rhythmic layer played over the campaign theme, not a theme itself. " +
                           "The game plays it in the key the score is in (C, Eb or Gb), with three 41.6 s variations per " +
                           "key, so nine places in all; a branch's wavs are handed to the places it covers in turn. " +
                           "Pitched pulses only sound right written per key - split by Key for that. Anything longer " +
                           "than a variation is trimmed to fit.";

                if (Wh3MusicHierarchyInformation.GetAmsFragmentContainerId(StateGroupName) != null)
                    return "Ambient fragments - short phrases laid over the campaign music, picked at random. Vanilla writes " +
                           "them per key the score is in (C, Eb, Gb) and as a call and a response; split by Key and by Call " +
                           "or response to do the same, or leave the wavs at the top to play everywhere.";

                return "The tree says which wavs play when. With only the top of the tree given wavs they play everywhere, one " +
                       "picked at random each time; split a branch by something - the part of the game, the result, the " +
                       "phase, the key - to give that part its own.";
            }
        }


        public MusicEventViewModel(MusicCultureSlot slot, string musicalCulture, string? title, IEnumerable<AudioFile> audioFiles, MusicTree? tree)
        {
            Slot = slot;
            Title = title ?? slot.EventPrefix;

            SlotStateGroupName = Wh3MusicEventInformation.GetStateGroupForPrefix(slot.EventPrefix);

            if (!IsBorrowed)
            {
                EventName = slot.EventPrefix + musicalCulture;
                if (Wh3MusicEventInformation.TryResolveStateTarget(EventName, out var stateGroupName, out var stateName))
                {
                    StateGroupName = stateGroupName;
                    StateName = stateName;
                    CanCarryOwnAudio = Wh3MusicHierarchyInformation.CanCarryOwnAudio(stateGroupName);
                }
            }

            HasTree = tree != null;
            foreach (var audioFile in audioFiles)
                AudioFiles.Add(audioFile.WavPackFilePath);

            // A tree's audio is spread over its branches, so the row counts all of it.
            var wavCount = tree?.GetNodes().SelectMany(node => node.AudioFileIds).Distinct().Count() ?? AudioFiles.Count;
            var branchCount = (tree?.GetNodes().Count() ?? 1) - 1;
            AudioSummary =
                IsBorrowed ? "borrowed" :
                !CanCarryOwnAudio ? "-" :
                wavCount == 0 ? "none yet" :
                (wavCount == 1 ? "1 wav" : $"{wavCount} wavs") + (branchCount == 0 ? "" : branchCount == 1 ? ", 1 branch" : $", {branchCount} branches");
        }

        /// <summary>Whether the event's audio is a decision tree - the battle theme and the campaign
        /// map theme - rather than one list.</summary>
        public bool HasTree { get; }
    }
}
