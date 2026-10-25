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

                if (Wh3MusicHierarchyInformation.IsAmsPulseStateGroup(StateGroupName))
                    return "An adaptive music pulse - a rhythmic layer played over the campaign theme, not a theme itself. " +
                           "The game has nine slots for it: one short loop is used for all nine, or up to nine vary it " +
                           "across the piece. Anything longer than a slot is trimmed to fit.";

                if (Wh3MusicHierarchyInformation.GetAmsFragmentContainerId(StateGroupName) != null)
                    return "Ambient fragments - short sounds laid over the campaign music rather than music itself. " +
                           "Vanilla varies them by the musical key the score is in; anything added here plays in every key.";

                if (StateGroupName == BattleCultureStateGroup)
                    return "The same music plays whether the battle is won, lost or drawn - vanilla splits those three, " +
                           "but a single event cannot tell them apart.";

                return "The wavs below play, one picked at random each time, whenever the scripts post this event.";
            }
        }

        const string BattleCultureStateGroup = "Battle_Music_WH3_Culture";

        public MusicEventViewModel(MusicCultureSlot slot, string musicalCulture, string? title, IEnumerable<AudioFile> audioFiles)
        {
            Slot = slot;
            Title = title ?? slot.EventPrefix;

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

            SetAudioFiles(audioFiles);
        }

        public void SetAudioFiles(IEnumerable<AudioFile> audioFiles)
        {
            AudioFiles.Clear();
            foreach (var audioFile in audioFiles)
                AudioFiles.Add(audioFile.WavPackFilePath);

            AudioSummary =
                IsBorrowed ? "borrowed" :
                !CanCarryOwnAudio ? "-" :
                AudioFiles.Count == 0 ? "none yet" :
                AudioFiles.Count == 1 ? "1 wav" :
                $"{AudioFiles.Count} wavs";
        }
    }
}
