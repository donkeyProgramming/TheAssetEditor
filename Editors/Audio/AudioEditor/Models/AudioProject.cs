using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Shared.GameFormats.Wwise.Enums;
using static Editors.Audio.GameSettings.Warhammer3.DialogueEvents;
using static Editors.Audio.GameSettings.Warhammer3.SoundBanks;
using static Editors.Audio.GameSettings.Warhammer3.StateGroups;

namespace Editors.Audio.AudioEditor.Models
{
    public abstract class AudioProjectItem
    {
        public string Name { get; set; }
        public uint Id { get; set; }
    }

    public abstract class AudioProjectHircItem : AudioProjectItem
    {
        public abstract AkBkHircType HircType { get; set; }
    }

    public interface ISettings { }

    public class AudioProject
    {
        public string FileName { get; set; }
        public string DirectoryPath { get; set; }
        public string Language { get; set; }
        public List<SoundBank> SoundBanks { get; set; }
        public List<StateGroup> StateGroups { get; set; }

        public static AudioProject CreateAudioProject()
        {
            // TODO: Add abstraction for other games
            var audioProject = new AudioProject();
            InitialiseSoundBanks(audioProject);
            InitialiseModdedStatesGroups(audioProject);
            return audioProject;
        }

        private static void InitialiseSoundBanks(AudioProject audioProject)
        {
            var soundBanks = Enum.GetValues<Wh3SoundBankSubtype>()
                .Select(soundBankSubtype => new SoundBank
                {
                    Name = GetSoundBankSubTypeString(soundBankSubtype),
                    SoundBankType = GetSoundBankSubType(soundBankSubtype)
                })
                .ToList();

            audioProject.SoundBanks = [];

            foreach (var soundBankSubtype in Enum.GetValues<Wh3SoundBankSubtype>())
            {
                var soundBank = new SoundBank
                {
                    Name = GetSoundBankSubTypeString(soundBankSubtype),
                    SoundBankType = GetSoundBankSubType(soundBankSubtype)
                };

                if (soundBank.SoundBankType == Wh3SoundBankType.ActionEventSoundBank)
                    soundBank.ActionEvents = [];
                else
                {
                    soundBank.DialogueEvents = [];

                    var filteredDialogueEvents = DialogueEventData
                        .Where(dialogueEvent => dialogueEvent.SoundBank == GetSoundBankSubtype(soundBank.Name));

                    foreach (var dialogueData in filteredDialogueEvents)
                    {
                        var dialogueEvent = new DialogueEvent
                        {
                            Name = dialogueData.Name,
                            StatePaths = []
                        };
                        soundBank.DialogueEvents.Add(dialogueEvent);
                    }
                }

                audioProject.SoundBanks.Add(soundBank);
            }

            SortSoundBanksAlphabetically(audioProject);
        }

        private static void InitialiseModdedStatesGroups(AudioProject audioProject)
        {
            audioProject.StateGroups = [];

            foreach (var moddedStateGroup in ModdedStateGroups)
            {
                var stateGroup = new StateGroup { Name = moddedStateGroup, States = [] };
                audioProject.StateGroups.Add(stateGroup);
            }
        }

        private static void SortSoundBanksAlphabetically(AudioProject audioProject)
        {
            var sortedSoundBanks = audioProject.SoundBanks.OrderBy(soundBank => soundBank.Name).ToList();

            audioProject.SoundBanks.Clear();

            foreach (var soundBank in sortedSoundBanks)
                audioProject.SoundBanks.Add(soundBank);
        }

        // Gets the audio project with only the used data, unused data is removed
        public static AudioProject GetAudioProject(AudioProject audioProject)
        {
            return new AudioProject
            {
                FileName = audioProject.FileName,
                DirectoryPath = audioProject.DirectoryPath,
                Language = audioProject.Language,
                SoundBanks = FilterSoundBanks(audioProject.SoundBanks),
                StateGroups = FilterStateGroups(audioProject.StateGroups)
            };
        }

        private static List<SoundBank> FilterSoundBanks(IEnumerable<SoundBank> soundBanks)
        {
            if (soundBanks == null)
                return null;

            var processedBanks = soundBanks
                .Where(soundBank => soundBank != null)
                .Select(ProcessSoundBank)
                .Where(soundBank => 
                    soundBank.DialogueEvents != null && soundBank.DialogueEvents.Count != 0 || 
                    soundBank.ActionEvents != null && soundBank.ActionEvents.Count != 0)
                .ToList();

            return processedBanks.Count != 0 ? processedBanks : null;
        }

        private static SoundBank ProcessSoundBank(SoundBank soundBank)
        {
            var dialogueEvents = (soundBank.DialogueEvents ?? [])
                .Where(dialogueEvent => dialogueEvent.StatePaths != null && dialogueEvent.StatePaths.Count != 0)
                .ToList();

            var actionEvents = (soundBank.ActionEvents ?? [])
                .Where(actionEvent => actionEvent.Sound != null || actionEvent.RandomSequenceContainer != null)
                .ToList();

            return new SoundBank
            {
                Name = soundBank.Name,
                SoundBankType = soundBank.SoundBankType,
                DialogueEvents = dialogueEvents.Count != 0 ? dialogueEvents : null,
                ActionEvents = actionEvents.Count != 0 ? actionEvents : null
            };
        }

        private static List<StateGroup> FilterStateGroups(List<StateGroup> stateGroups)
        {
            if (stateGroups == null)
                return null;

            var filteredStateGroups = stateGroups
                .Where(stateGroup => stateGroup.States != null && stateGroup.States.Count != 0)
                .ToList();

            return filteredStateGroups.Count != 0 ? filteredStateGroups : null;
        }

        public static AudioProject MergeAudioProjects(List<AudioProject> audioProjectsToMerge)
        {
            var mergedAudioProject = CreateAudioProject();

            foreach (var audioProjectToMerge in audioProjectsToMerge)
                mergedAudioProject = MergeAudioProject(mergedAudioProject, audioProjectToMerge);

            mergedAudioProject = GetAudioProject(mergedAudioProject);
            return mergedAudioProject;
        }

        private static AudioProject MergeAudioProject(AudioProject mergedAudioProject, AudioProject audioProjectToMerge)
        {
            if (string.IsNullOrEmpty(mergedAudioProject.Language))
                mergedAudioProject.Language = audioProjectToMerge.Language;

            MergeSoundBanks(mergedAudioProject, audioProjectToMerge);
            MergeStateGroups(mergedAudioProject, audioProjectToMerge);

            return mergedAudioProject;
        }

        private static void MergeSoundBanks(AudioProject mergedAudioProject, AudioProject audioProjectToMerge)
        {
            if (audioProjectToMerge.SoundBanks == null)
                return;

            foreach (var soundBankToMerge in audioProjectToMerge.SoundBanks)
            {
                var mergedSoundBank = mergedAudioProject.SoundBanks.FirstOrDefault(mergedSoundBank => mergedSoundBank.Name == soundBankToMerge.Name);
                if (mergedSoundBank != null)
                {
                    MergeDialogueEvents(mergedSoundBank, soundBankToMerge);
                    MergeActionEvents(mergedSoundBank, soundBankToMerge);
                }
                else
                {
                    mergedAudioProject.SoundBanks.Add(soundBankToMerge);
                    mergedAudioProject.SoundBanks = mergedAudioProject.SoundBanks.OrderBy(sb => sb.Name).ToList();
                }
            }
        }

        private static void MergeDialogueEvents(SoundBank mergedSoundBank, SoundBank soundBankToMerge)
        {
            if (soundBankToMerge.DialogueEvents == null)
                return;

            foreach (var newDialogue in soundBankToMerge.DialogueEvents)
            {
                var mergedDialogueEvent = mergedSoundBank.DialogueEvents?.FirstOrDefault(mergedDialogueEvent => mergedDialogueEvent.Name == newDialogue.Name);
                if (mergedDialogueEvent != null)
                {
                    foreach (var statePathToMerge in newDialogue.StatePaths)
                        AudioProjectHelpers.InsertStatePathAlphabetically(mergedDialogueEvent, statePathToMerge);
                }
                else
                {
                    mergedSoundBank.DialogueEvents ??= [];
                    mergedSoundBank.DialogueEvents.Add(newDialogue);
                    mergedSoundBank.DialogueEvents = mergedSoundBank.DialogueEvents.OrderBy(mergedDialogueEvent => mergedDialogueEvent.Name).ToList();
                }
            }
        }

        private static void MergeActionEvents(SoundBank baseBank, SoundBank newBank)
        {
            if (newBank.ActionEvents == null)
                return;

            foreach (var newAction in newBank.ActionEvents)
                AudioProjectHelpers.InsertActionEventAlphabetically(baseBank, newAction);
        }

        private static void MergeStateGroups(AudioProject mergedAudioProject, AudioProject audioProjectToMerge)
        {
            if (audioProjectToMerge.StateGroups == null)
                return;

            foreach (var stateGroupToMerge in audioProjectToMerge.StateGroups)
            {
                var baseGroup = mergedAudioProject.StateGroups.FirstOrDefault(mergedStateGroup => mergedStateGroup.Name == stateGroupToMerge.Name);
                if (baseGroup != null)
                {
                    foreach (var newState in stateGroupToMerge.States)
                        AudioProjectHelpers.InsertStateAlphabetically(baseGroup, newState);
                }
                else
                    mergedAudioProject.StateGroups.Add(stateGroupToMerge);
            }
        }
    }
}
