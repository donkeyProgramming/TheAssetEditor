using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Shared.GameFormats.Wwise;

namespace Editors.Audio.Shared.AudioProject.Models
{
    public class SoundBank : AudioProjectItem
    {
        public string Language { get; set; }
        [JsonIgnore] public uint LanguageId { get; set; }
        [JsonIgnore] public string FileName { get; set; }
        [JsonIgnore] public string FilePath { get; set; }
        [JsonIgnore] public uint TestingId { get; set; }
        [JsonIgnore] public string TestingFileName { get; set; }
        [JsonIgnore] public string TestingFilePath { get; set; }
        [JsonIgnore] public uint MergingId { get; set; }
        [JsonIgnore] public string MergingFileName { get; set; }
        [JsonIgnore] public string MergingFilePath { get; set; }

        /// <summary>
        /// The audio project this bank was compiled from, without its extension. Testing .bnks are
        /// named after the vanilla .bnk they override rather than after this bank, so they need the
        /// project name separately to stay distinguishable between mods.
        /// </summary>
        [JsonIgnore] public string AudioProjectName { get; set; }
        public Wh3SoundBank GameSoundBank { get; set; }
        public List<DialogueEvent> DialogueEvents { get; set; } = [];
        public List<ActionEvent> ActionEvents { get; set; } = [];
        public List<Sound> Sounds { get; set; } = [];
        public List<RandomSequenceContainer> RandomSequenceContainers { get; set; } = [];

        /// <summary>
        /// The music hierarchy this bank contributes. Music is not reached through a Play action
        /// like everything else here - an Action Event sets a State, and these are what the vanilla
        /// decision tree selects once a branch for that State has been merged in.
        /// </summary>
        public List<MusicRandomSequence> MusicRandomSequences { get; set; } = [];
        public List<MusicSegment> MusicSegments { get; set; } = [];

        /// <summary>
        /// Audio for the adaptive music system's pulse layers. Separate from the list above because
        /// it produces no hirc of its own - it is added as sub-tracks to vanilla Music Tracks - so a
        /// bank holding only these still has something to compile.
        /// </summary>
        public List<AmsPulse> AmsPulses { get; set; } = [];

        /// <summary>
        /// Audio for the adaptive music system's ambient fragments. Unlike the pulses these do
        /// generate hircs of their own - a Switch container on the musical key Group and the Sounds
        /// under it - but the vanilla container above them still has to be merged.
        /// </summary>
        public List<AmsFragment> AmsFragments { get; set; } = [];

        /// <summary>
        /// Which audio each music event plays when, one tree per State the bank's events set. The
        /// music hierarchy above is generated from these - see <see cref="MusicTree"/>.
        /// </summary>
        public List<MusicTree> MusicTrees { get; set; } = [];

        public SoundBank(string name, Wh3SoundBank gameSoundBank, string language)
        {
            Id = WwiseHash.Compute(name);
            Name = name;
            GameSoundBank = gameSoundBank;
            Language = language;
            LanguageId = WwiseHash.Compute(language);
        }

        public SoundBank Clean()
        {
            var cleanedDialogueEvents = DialogueEvents
                .Where(dialogueEvent =>
                    dialogueEvent.StatePaths != null && dialogueEvent.StatePaths.Count != 0)
                .ToList();

            var cleanedActionEvents = ActionEvents
                .Where(actionEvent => actionEvent.Actions.Count != 0)
                .ToList();

            // A branch with no segments has nothing to play, so it is dropped the same way an empty
            // Action Event is.
            var cleanedMusicRandomSequences = MusicRandomSequences
                .Where(musicRandomSequence => musicRandomSequence.Segments.Count != 0)
                .ToList();

            var cleanedAmsPulses = AmsPulses
                .Where(amsPulse => amsPulse.Clips.Count != 0)
                .ToList();

            var cleanedAmsFragments = AmsFragments
                .Where(amsFragment => amsFragment.SoundIds.Count != 0)
                .ToList();

            if (cleanedDialogueEvents.Count == 0 && cleanedActionEvents.Count == 0
                && cleanedMusicRandomSequences.Count == 0 && cleanedAmsPulses.Count == 0
                && cleanedAmsFragments.Count == 0)
                return null;

            return new SoundBank(Name, GameSoundBank, Language)
            {
                FileName = FileName,
                FilePath = FilePath,
                AudioProjectName = AudioProjectName,
                TestingId = TestingId,
                TestingFileName = TestingFileName,
                TestingFilePath = TestingFilePath,
                MergingId = MergingId,
                MergingFileName = MergingFileName,
                MergingFilePath = MergingFilePath,
                DialogueEvents = cleanedDialogueEvents,
                ActionEvents = cleanedActionEvents,
                Sounds = Sounds.ToList(),
                RandomSequenceContainers = RandomSequenceContainers.ToList(),
                MusicRandomSequences = cleanedMusicRandomSequences,
                MusicSegments = MusicSegments.ToList(),
                AmsPulses = cleanedAmsPulses,
                AmsFragments = cleanedAmsFragments,
                MusicTrees = MusicTrees.ToList()
            };
        }

        public ActionEvent GetActionEvent(string actionEventName) => ActionEvents.FirstOrDefault(actionEvent => actionEvent.Name == actionEventName);

        public List<Wh3ActionEventType> GetUsedActionEventTypes()
        {
            var usedActionEventGroups = ActionEvents
                .Select(actionEventGroup => actionEventGroup.ActionEventType)
                .Distinct();

            var allowedActionEventGroups = Wh3ActionEventInformation
                .GetSoundBankActionEventTypes(GameSoundBank);

            return usedActionEventGroups
                .Where(allowedActionEventGroups.Contains)
                .OrderBy(allowedActionEventGroups.IndexOf)
                .ToList();
        }

        public List<DialogueEvent> GetEditedDialogueEvents()
        {
            return DialogueEvents
                .Where(dialogueEvent => dialogueEvent.StatePaths.Count != 0)
                .ToList();
        }

        public List<ActionEvent> GetPlayActionEvents()
        {
            return ActionEvents
                .Where(actionEvent => actionEvent.GetPlayActions().Count != 0)
                .ToList();
        }

        public Sound GetSound(uint id) => Sounds.FirstOrDefault(sound => sound.Id == id);

        public List<Sound> GetSounds(List<uint> soundReferences)
        {
            var sounds = new List<Sound>();
            foreach (var soundReference in soundReferences)
                sounds.Add(GetSound(soundReference));
            return sounds;
        }

        public RandomSequenceContainer GetRandomSequenceContainer(uint id)
        {
            return RandomSequenceContainers.FirstOrDefault(randomSequenceContainer => randomSequenceContainer.Id == id);
        }

        public MusicSegment GetMusicSegment(uint id) => MusicSegments.FirstOrDefault(musicSegment => musicSegment.Id == id);

        public List<MusicSegment> GetMusicSegments(MusicRandomSequence musicRandomSequence)
        {
            return musicRandomSequence.Segments
                .Select(entry => GetMusicSegment(entry.SegmentId))
                .Where(musicSegment => musicSegment != null)
                .ToList();
        }
    }

    public static class SoundBankListExtensions
    {
        public static void TryAdd(this List<SoundBank> existingSoundBanks, SoundBank soundBank)
        {
            ArgumentNullException.ThrowIfNull(existingSoundBanks);
            ArgumentNullException.ThrowIfNull(soundBank);

            if (existingSoundBanks.Any(existingSoundBank => existingSoundBank.Id == soundBank.Id))
                throw new ArgumentException($"Cannot add SoundBank with Id {soundBank.Id} as it already exists.");

            var index = existingSoundBanks.BinarySearch(soundBank, AudioProjectItem.IdComparer);
            if (index < 0)
                index = ~index;

            existingSoundBanks.Insert(index, soundBank);
        }
    }
}
