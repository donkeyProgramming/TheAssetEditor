using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using static Editors.Audio.Shared.GameInformation.Warhammer3.Wh3StateGroupInformation;

namespace Editors.Audio.Shared.AudioProject.Models
{
    public class AudioProjectFile
    {
        public string Language { get; set; }
        public List<SoundBank> SoundBanks { get; set; } = [];
        public List<StateGroup> StateGroups { get; set; } = [];
        public List<AudioFile> AudioFiles { get; set; } = [];

        /// <summary>The cultures a music project wires into the music scripts. Null rather than
        /// empty for any other project, so it is left out of the .aproj.</summary>
        public List<MusicCulture> MusicCultures { get; set; }

        public static AudioProjectFile CreateNew(string language, string audioProjectNameWithoutExtension)
        {
            return new AudioProjectFile
            {
                Language = language,
                SoundBanks = CreateSoundBanks(audioProjectNameWithoutExtension, language),
                StateGroups = CreateStateGroups(),
                AudioFiles = []
            };
        }

        private static AudioProjectFile CreateFromExisting(string language, List<SoundBank> soundBanks, List<StateGroup> stateGroups, List<AudioFile> audioFiles)
        {
            return new AudioProjectFile
            {
                Language = language,
                SoundBanks = soundBanks,
                StateGroups = stateGroups,
                AudioFiles = audioFiles.ToList()
            };
        }

        private static List<SoundBank> CreateSoundBanks(string audioProjectNameWithoutExtension, string audioProjectLanguage)
        {
            var soundBanks = new List<SoundBank>();

            foreach (var soundBankDefinition in Wh3SoundBankInformation.Information)
            {
                var gameSoundBank = soundBankDefinition.GameSoundBank;
                var soundBankInformationName = Wh3SoundBankInformation.GetName(gameSoundBank);
                var soundBankName = $"{soundBankInformationName}_{audioProjectNameWithoutExtension}";

                var language = audioProjectLanguage;
                var requiredLanguage = Wh3SoundBankInformation.GetRequiredLanguage(gameSoundBank);
                if (requiredLanguage != null)
                    language = Wh3LanguageInformation.GetLanguageAsString((Wh3Language)requiredLanguage);

                var soundBank = new SoundBank(soundBankName, gameSoundBank, language);

                if (Wh3DialogueEventInformation.Contains(gameSoundBank))
                    CreateDialogueEvents(gameSoundBank, soundBank);

                soundBanks.TryAdd(soundBank);
            }

            var sortedSoundBanks = soundBanks
                .OrderBy(soundBank => soundBank?.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            soundBanks.Clear();
            soundBanks.AddRange(sortedSoundBanks);
            return soundBanks;
        }

        private static void CreateDialogueEvents(Wh3SoundBank gameSoundBank, SoundBank soundBank)
        {
            var filteredDialogueEvents = Wh3DialogueEventInformation.Information.Where(dialogueEvent => dialogueEvent.SoundBank == gameSoundBank);
            foreach (var filteredDialogueEvent in filteredDialogueEvents)
            {
                var dialogueEvent = new DialogueEvent(filteredDialogueEvent.Name);
                soundBank.DialogueEvents.TryAdd(dialogueEvent);
            }

            var sortedDialogueEvents = soundBank.DialogueEvents
                .OrderBy(dialogueEvent => dialogueEvent?.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            soundBank.DialogueEvents.Clear();
            soundBank.DialogueEvents.AddRange(sortedDialogueEvents);
        }

        private static List<StateGroup> CreateStateGroups()
        {
            var stateGroups = new List<StateGroup>();
            foreach (var moddableStateGroup in ModdableStateGroups)
            {
                var stateGroup = StateGroup.CreateForAudioProjectFile(moddableStateGroup, []);
                stateGroups.TryAdd(stateGroup);
            }
            return stateGroups;
        }

        public AudioProjectFile Clean()
        {
            var cleanSoundBanks = SoundBanks
                .Select(soundBank => soundBank.Clean())
                .Where(soundBank => soundBank != null)
                .ToList();

            var cleanStateGroups = StateGroups
                .Select(stateGroup => stateGroup.Clean())
                .Where(stateGroup => stateGroup != null)
                .ToList();

            var clean = CreateFromExisting(Language, cleanSoundBanks, cleanStateGroups, AudioFiles);
            clean.MusicCultures = MusicCultures;
            return clean;
        }

        public List<SoundBank> GetEditedActionEventSoundBanks()
        {
            return SoundBanks
                .Where(soundBank => Wh3ActionEventInformation.Contains(soundBank.GameSoundBank) && soundBank.ActionEvents.Count != 0)
                .ToList();
        }

        public List<SoundBank> GetEditedDialogueEventSoundBanks()
        {
            return SoundBanks
                .Where(soundBank => Wh3DialogueEventInformation.Contains(soundBank.GameSoundBank)
                    && soundBank.DialogueEvents.Any(dialogueEvent => dialogueEvent.StatePaths.Count != 0))
                .ToList();
        }

        public List<SoundBank> GetEditedSoundBanks()
        {
            var editedActionEventSoundBanks = GetEditedActionEventSoundBanks();
            var editedDialogueEventSoundBanks = GetEditedDialogueEventSoundBanks();
            var editedSoundBanks = editedActionEventSoundBanks
                .Union(editedDialogueEventSoundBanks)
                .ToList();
            return editedSoundBanks;
        }

        public List<StateGroup> GetEditedStateGroups()
        {
            return StateGroups
                .Where(state => state.States.Count != 0)
                .ToList();
        }

        public SoundBank GetSoundBank(string soundBankName) => SoundBanks.FirstOrDefault(soundBank => soundBank.Name == soundBankName);

        public ActionEvent GetActionEvent(string actionEventName)
        {
            return SoundBanks
                .Where(soundBank => Wh3ActionEventInformation.Contains(soundBank.GameSoundBank))
                .SelectMany(soundBank => soundBank.ActionEvents)
                .FirstOrDefault(actionEvent => actionEvent.Name == actionEventName);
        }

        public List<ActionEvent> GetActionEvents()
        {
            return SoundBanks
                .Where(soundBank => soundBank.ActionEvents.Count != 0)
                .SelectMany(soundBank => soundBank.ActionEvents)
                .ToList();
        }

        public DialogueEvent GetDialogueEvent(string dialogueEventName)
        {
            return SoundBanks
                .Where(soundBank => Wh3DialogueEventInformation.Contains(soundBank.GameSoundBank))
                .SelectMany(soundBank => soundBank.DialogueEvents)
                .FirstOrDefault(dialogueEvent => dialogueEvent.Name == dialogueEventName);
        }

        public StateGroup GetStateGroup(string stateGroupName) => StateGroups.FirstOrDefault(stateGroup => stateGroup.Name == stateGroupName);

        public List<Sound> GetSounds()
        {
            return SoundBanks
                .SelectMany(soundBank => soundBank.Sounds)
                .Where(sound => sound != null)
                .ToList();
        }

        public HashSet<uint> GetAudioFileIds() => AudioFiles.Select(audioFile => audioFile.Id).ToHashSet();

        public AudioFile GetAudioFile(string filePath)
        {
            return AudioFiles.FirstOrDefault(audioFile => string.Equals(audioFile.WavPackFilePath, filePath, StringComparison.OrdinalIgnoreCase));
        }

        public AudioFile GetAudioFile(uint sourceId) => AudioFiles.FirstOrDefault(audioFile => audioFile.Id == sourceId);

        public List<AudioFile> GetAudioFiles(SoundBank soundBank, RandomSequenceContainer randomSequenceContainer)
        {
            var audioFiles = new List<AudioFile>();

            var orderedSounds = soundBank.GetSounds(randomSequenceContainer.Children)
                .OrderBy(sound => sound.PlaylistOrder)
                .ToList();

            foreach (var orderedSound in orderedSounds)
            {
                var audioFile = GetAudioFile(orderedSound.SourceId);
                audioFiles.TryAdd(audioFile);
            }

            return audioFiles;
        }

        public HashSet<uint> GetGeneratableItemIds()
        {
            var generatableItemIds = GetGeneratableItems().Select(item => item.Id).ToHashSet();

            // A Music Track is generated from its segment rather than being an item in its own
            // right, so its id is not in the item list - but it is just as much in use as the
            // segment's, and reusing it would put two hircs in the bank under one id.
            generatableItemIds.UnionWith(SoundBanks
                .SelectMany(soundBank => soundBank.MusicSegments)
                .Select(musicSegment => musicSegment.TrackId));

            // The same for the fragment switches, which are built at compile time from these ids.
            foreach (var amsFragment in SoundBanks.SelectMany(soundBank => soundBank.AmsFragments))
            {
                generatableItemIds.Add(amsFragment.KeySwitchContainerId);
                generatableItemIds.UnionWith(amsFragment.CallResponseSwitches.Select(callResponse => callResponse.Id));
            }

            return generatableItemIds;
        }

        public List<AudioProjectItem> GetGeneratableItems()
        {
            var generatableItems = new List<AudioProjectItem>();
            var addedAudioProjectItemIds = new HashSet<uint>();

            foreach (var soundBank in SoundBanks)
            {
                generatableItems.AddRange(GetSoundBankGeneratableItems(soundBank, addedAudioProjectItemIds));
                generatableItems.AddRange(GetActionEventGeneratableItems(soundBank, addedAudioProjectItemIds));
                generatableItems.AddRange(GetDialogueEventGeneratableItems(soundBank, addedAudioProjectItemIds));
                generatableItems.AddRange(GetMusicGeneratableItems(soundBank, addedAudioProjectItemIds));
            }

            return generatableItems;
        }

        private static List<AudioProjectItem> GetSoundBankGeneratableItems(SoundBank soundBank, HashSet<uint> addedAudioProjectItemIds)
        {
            var audioProjectItems = new List<AudioProjectItem>();

            if (addedAudioProjectItemIds.Add(soundBank.Id))
                audioProjectItems.Add(soundBank);

            return audioProjectItems;
        }

        private static List<AudioProjectItem> GetActionEventGeneratableItems(SoundBank soundBank, HashSet<uint> addedAudioProjectItemIds)
        {
            var audioProjectItems = new List<AudioProjectItem>();

            foreach (var actionEvent in soundBank.ActionEvents)
            {
                if (addedAudioProjectItemIds.Add(actionEvent.Id))
                    audioProjectItems.Add(actionEvent);

                foreach (var action in actionEvent.Actions)
                    audioProjectItems.AddRange(GetActionGeneratableItems(soundBank, action, addedAudioProjectItemIds));
            }

            return audioProjectItems;
        }

        private static List<AudioProjectItem> GetActionGeneratableItems(SoundBank soundBank, Action action, HashSet<uint> addedAudioProjectItemIds)
        {
            var audioProjectItems = new List<AudioProjectItem>();

            if (addedAudioProjectItemIds.Add(action.Id))
                audioProjectItems.Add(action);

            if (action.TargetHircTypeIsSound())
                audioProjectItems.AddRange(GetSoundTargetGeneratableItems(soundBank, action.TargetHircId, addedAudioProjectItemIds));

            if (action.TargetHircTypeIsRandomSequenceContainer())
                audioProjectItems.AddRange(GetRandomSequenceContainerTargetGeneratableItems(soundBank, action.TargetHircId, addedAudioProjectItemIds));

            return audioProjectItems;
        }

        // Music is not reached from an Event the way everything else here is - the decision tree
        // points at the random sequence - so it is walked from the SoundBank's own lists instead.
        private static List<AudioProjectItem> GetMusicGeneratableItems(SoundBank soundBank, HashSet<uint> addedAudioProjectItemIds)
        {
            var audioProjectItems = new List<AudioProjectItem>();

            foreach (var musicRandomSequence in soundBank.MusicRandomSequences)
            {
                if (addedAudioProjectItemIds.Add(musicRandomSequence.Id))
                    audioProjectItems.Add(musicRandomSequence);
            }

            foreach (var musicSegment in soundBank.MusicSegments)
            {
                if (addedAudioProjectItemIds.Add(musicSegment.Id))
                    audioProjectItems.Add(musicSegment);
            }

            return audioProjectItems;
        }

        private static List<AudioProjectItem> GetDialogueEventGeneratableItems(SoundBank soundBank, HashSet<uint> addedAudioProjectItemIds)
        {
            var audioProjectItems = new List<AudioProjectItem>();

            foreach (var dialogueEvent in soundBank.DialogueEvents)
            {
                if (addedAudioProjectItemIds.Add(dialogueEvent.Id))
                    audioProjectItems.Add(dialogueEvent);

                foreach (var statePath in dialogueEvent.StatePaths)
                    audioProjectItems.AddRange(GetStatePathGeneratableItems(soundBank, statePath, addedAudioProjectItemIds));
            }

            return audioProjectItems;
        }

        private static List<AudioProjectItem> GetStatePathGeneratableItems(SoundBank soundBank, StatePath statePath, HashSet<uint> addedAudioProjectItemIds)
        {
            var audioProjectItems = new List<AudioProjectItem>();

            if (statePath.TargetHircTypeIsSound())
                audioProjectItems.AddRange(GetSoundTargetGeneratableItems(soundBank, statePath.TargetHircId, addedAudioProjectItemIds));

            if (statePath.TargetHircTypeIsRandomSequenceContainer())
                audioProjectItems.AddRange(GetRandomSequenceContainerTargetGeneratableItems(soundBank, statePath.TargetHircId, addedAudioProjectItemIds));

            return audioProjectItems;
        }

        private static List<AudioProjectItem> GetSoundTargetGeneratableItems(SoundBank soundBank, uint targetHircId, HashSet<uint> addedAudioProjectItemIds)
        {
            var audioProjectItems = new List<AudioProjectItem>();

            var sound = soundBank.GetSound(targetHircId);
            if (addedAudioProjectItemIds.Add(sound.Id))
                audioProjectItems.Add(sound);

            return audioProjectItems;
        }

        private static List<AudioProjectItem> GetRandomSequenceContainerTargetGeneratableItems(SoundBank soundBank, uint targetHircId, HashSet<uint> addedAudioProjectItemIds)
        {
            var audioProjectItems = new List<AudioProjectItem>();

            var randomSequenceContainer = soundBank.GetRandomSequenceContainer(targetHircId);
            if (addedAudioProjectItemIds.Add(randomSequenceContainer.Id))
                audioProjectItems.Add(randomSequenceContainer);

            var sounds = soundBank.GetSounds(randomSequenceContainer.Children);
            foreach (var sound in sounds)
            {
                if (addedAudioProjectItemIds.Add(sound.Id))
                    audioProjectItems.Add(sound);
            }

            return audioProjectItems;
        }
    }
}
