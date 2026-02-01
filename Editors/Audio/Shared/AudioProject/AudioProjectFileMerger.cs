using System;
using System.Collections.Generic;
using System.Linq;
using Editors.Audio.Shared.AudioProject.Models;

namespace Editors.Audio.Shared.AudioProject
{
    public static class AudioProjectFileMerger
    {
        public static void Merge(AudioProjectFile baseAudioProject, AudioProjectFile mergingAudioProject, string baseFileName, string mergingFileName)
        {
            ArgumentNullException.ThrowIfNull(baseAudioProject);
            ArgumentNullException.ThrowIfNull(mergingAudioProject);

            MergeSoundBanks(baseAudioProject.SoundBanks, mergingAudioProject.SoundBanks, baseFileName, mergingFileName);
            MergeStateGroups(baseAudioProject.StateGroups, mergingAudioProject.StateGroups);
            MergeAudioFiles(baseAudioProject.AudioFiles, mergingAudioProject.AudioFiles);
        }

        private static void MergeSoundBanks(List<SoundBank> baseSoundBanks, List<SoundBank> mergingSoundBanks, string baseFileName, string mergingFileName)
        {
            foreach (var mergingSoundBank in mergingSoundBanks)
            {
                var mergingSoundBankBaseName = RemoveSoundBankProjectSuffix(mergingSoundBank.Name, mergingFileName);
                var baseSoundBank = FindSoundBankByBaseName(baseSoundBanks, baseFileName, mergingSoundBankBaseName);

                if (baseSoundBank == null)
                {
                    NormaliseSoundBankName(mergingSoundBank, baseFileName, mergingFileName);
                    baseSoundBanks.TryAdd(mergingSoundBank);
                    SortAudioProjectItems(mergingSoundBank);
                    continue;
                }

                MergeDialogueEvents(baseSoundBank, mergingSoundBank);
                MergeActionEvents(baseSoundBank, mergingSoundBank);

                foreach (var sound in mergingSoundBank.Sounds)
                    baseSoundBank.Sounds.TryAdd(sound);

                foreach (var randomSequenceContainer in mergingSoundBank.RandomSequenceContainers)
                    baseSoundBank.RandomSequenceContainers.TryAdd(randomSequenceContainer);

                SortAudioProjectItems(baseSoundBank);
            }

            SortSoundBanks(baseSoundBanks);
        }

        private static SoundBank FindSoundBankByBaseName(List<SoundBank> baseSoundBanks, string baseAudioProjectNameWithoutExtension, string mergingSoundBankBaseName)
        {
            foreach (var baseSoundBank in baseSoundBanks)
            {
                var baseSoundBankBaseName = RemoveSoundBankProjectSuffix(baseSoundBank.Name, baseAudioProjectNameWithoutExtension);
                if (string.Equals(baseSoundBankBaseName, mergingSoundBankBaseName, StringComparison.OrdinalIgnoreCase))
                    return baseSoundBank;
            }

            return null;
        }

        private static void MergeDialogueEvents(SoundBank baseSoundBank, SoundBank mergingSoundBank)
        {
            foreach (var mergingDialogueEvent in mergingSoundBank.DialogueEvents)
            {
                var baseDialogueEvent = baseSoundBank.DialogueEvents.FirstOrDefault(dialogueEvent => dialogueEvent.Id == mergingDialogueEvent.Id);
                if (baseDialogueEvent == null)
                {
                    SortStatePaths(mergingDialogueEvent);
                    baseSoundBank.DialogueEvents.TryAdd(mergingDialogueEvent);
                    continue;
                }

                foreach (var mergingStatePath in mergingDialogueEvent.StatePaths)
                    baseDialogueEvent.StatePaths.TryAdd(mergingStatePath);

                SortStatePaths(baseDialogueEvent);
            }

            SortDialogueEvents(baseSoundBank);
        }

        private static void MergeActionEvents(SoundBank baseSoundBank, SoundBank mergingSoundBank)
        {
            foreach (var mergingActionEvent in mergingSoundBank.ActionEvents)
            {
                if (!baseSoundBank.ActionEvents.Any(actionEvent => actionEvent.Id == mergingActionEvent.Id))
                    baseSoundBank.ActionEvents.TryAdd(mergingActionEvent);
            }

            SortActionEvents(baseSoundBank);
        }

        private static void MergeStateGroups(List<StateGroup> baseStateGroups, List<StateGroup> mergingStateGroups)
        {
            foreach (var mergingStateGroup in mergingStateGroups)
            {
                var baseStateGroup = baseStateGroups.FirstOrDefault(stateGroup => stateGroup.Id == mergingStateGroup.Id);
                if (baseStateGroup == null)
                {
                    baseStateGroups.TryAdd(mergingStateGroup);
                    continue;
                }

                foreach (var mergingState in mergingStateGroup.States)
                    baseStateGroup.States.TryAdd(mergingState);
            }
        }

        private static void NormaliseSoundBankName(SoundBank soundBankToNormalise, string baseFileName, string mergingFileName)
        {
            var soundBankBaseName = RemoveSoundBankProjectSuffix(soundBankToNormalise.Name, mergingFileName);
            soundBankToNormalise.Name = $"{soundBankBaseName}_{baseFileName}";
        }

        private static string RemoveSoundBankProjectSuffix(string soundBankName, string audioProjectNameWithoutExtension)
        {
            if (string.IsNullOrWhiteSpace(soundBankName))
                return string.Empty;

            if (string.IsNullOrWhiteSpace(audioProjectNameWithoutExtension))
                return soundBankName;

            var suffix = $"_{audioProjectNameWithoutExtension}";
            if (soundBankName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return soundBankName.Substring(0, soundBankName.Length - suffix.Length);

            return soundBankName;
        }

        private static void SortAudioProjectItems(SoundBank soundBank)
        {
            SortDialogueEvents(soundBank);
            SortActionEvents(soundBank);
        }

        private static void SortSoundBanks(List<SoundBank> soundBanks)
        {
            var sortedSoundBanks = soundBanks
                .OrderBy(soundBank => soundBank?.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            soundBanks.Clear();
            soundBanks.AddRange(sortedSoundBanks);
        }

        private static void SortDialogueEvents(SoundBank soundBank)
        {
            var sortedDialogueEvents = soundBank.DialogueEvents
                .OrderBy(dialogueEvent => dialogueEvent?.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            soundBank.DialogueEvents.Clear();
            soundBank.DialogueEvents.AddRange(sortedDialogueEvents);
        }

        private static void SortActionEvents(SoundBank soundBank)
        {
            var sortedActionEvents = soundBank.ActionEvents
                .OrderBy(actionEvent => actionEvent?.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            soundBank.ActionEvents.Clear();
            soundBank.ActionEvents.AddRange(sortedActionEvents);
        }

        private static void SortStatePaths(DialogueEvent dialogueEvent)
        {
            var sortedStatePaths = dialogueEvent.StatePaths
                .OrderBy(statePath => statePath?.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            dialogueEvent.StatePaths.Clear();
            dialogueEvent.StatePaths.AddRange(sortedStatePaths);
        }

        private static void MergeAudioFiles(List<AudioFile> baseAudioFiles, List<AudioFile> mergingAudioFiles)
        {
            if (baseAudioFiles == null || mergingAudioFiles == null)
                return;

            foreach (var mergingAudioFile in mergingAudioFiles)
                baseAudioFiles.TryAdd(mergingAudioFile);
        }
    }
}
