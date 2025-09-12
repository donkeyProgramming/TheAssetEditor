using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Editors.Audio.AudioEditor;
using Editors.Audio.AudioEditor.Models;
using Editors.Audio.GameInformation.Warhammer3;
using Editors.Audio.Utility;
using Shared.Core.Misc;

namespace Editors.Audio.AudioProjectCompiler
{
    public interface IAudioProjectCompilerService
    {
        void Compile(AudioProject audioProject, string audioProjectFileName, string audioProjectFilePath);
    }

    public class AudioProjectCompilerService(
        IAudioProjectFileService audioProjectFileService,
        IIdGeneratorService idGeneratorService,
        IWemGeneratorService wemGeneratorService,
        ISoundBankGeneratorService soundBankGeneratorService,
        IDatGeneratorService datGeneratorService) : IAudioProjectCompilerService
    {
        private readonly IAudioProjectFileService _audioProjectFileService = audioProjectFileService;
        private readonly IIdGeneratorService _idGeneratorService = idGeneratorService;
        private readonly IWemGeneratorService _wemGeneratorService = wemGeneratorService;
        private readonly ISoundBankGeneratorService _soundBankGeneratorService = soundBankGeneratorService;
        private readonly IDatGeneratorService _datGeneratorService = datGeneratorService;

        public void Compile(AudioProject audioProject, string audioProjectFileName, string audioProjectFilePath)
        {
            if (audioProject.SoundBanks == null)
                return;

            var usedCompilerHircIds = new HashSet<uint>();
            var sourceIdByWavFilePath = new Dictionary<string, uint>();
            var soundsToGenerateWemsFrom = new List<Sound>();

            var audioProjectFileNameWithoutSpaces = audioProjectFileName.Replace(" ", "_");
            var audioProjectNameFileWithoutExtension = Path.GetFileNameWithoutExtension(audioProjectFileNameWithoutSpaces);

            AudioProjectCompilerHelpers.ClearTempAudioFiles();

            // We set the data from the bottom up, so Sounds, then Actions, then Events to ensure that IDs are generated before
            // they're referenced e.g. Sounds / Random Sequence Container IDs are used in Actions, and Action IDs are used in Events.
            SetSoundBankData(audioProject, audioProjectNameFileWithoutExtension);

            if (audioProject.SoundBanks.Any(soundBank => soundBank.ActionEvents != null))
            {
                SetPlayActionEventData(audioProject, usedCompilerHircIds);
                SetPlayActionTargetData(audioProject, usedCompilerHircIds, sourceIdByWavFilePath, soundsToGenerateWemsFrom);
                SetPlayActionData(audioProject, usedCompilerHircIds);
                SetStopActionEventData(audioProject, usedCompilerHircIds);
                SetStopActionData(audioProject, usedCompilerHircIds);
            }

            if (audioProject.SoundBanks.Any(soundBank => soundBank.DialogueEvents != null))
            {
                SetDialogueEventData(audioProject);
                SetDialogueEventSourceData(audioProject, usedCompilerHircIds, sourceIdByWavFilePath, soundsToGenerateWemsFrom);
            }

            if (audioProject.StateGroups != null)
                SetStateGroupData(audioProject);

            foreach (var soundBank in audioProject.SoundBanks)
            {
                _soundBankGeneratorService.GenerateSoundBank(soundBank);

                if (audioProject.SoundBanks.Any(soundBank => soundBank.DialogueEvents != null))
                    _soundBankGeneratorService.GenerateDialogueEventSplitSoundBanks(soundBank);
            }

            var wemsToGenerate = soundsToGenerateWemsFrom.DistinctBy(sound => sound.SourceId).ToList();
            _wemGeneratorService.GenerateWems(wemsToGenerate);
            SaveWemsToPack(soundsToGenerateWemsFrom);
            UpdateSoundMemoryMediaSize(soundsToGenerateWemsFrom);

            // The .dat file is seems to only necessary for playing movie Action Events and any triggered via common.trigger_soundevent()
            // but without testing all the different types of Action Event sounds it's safer to just make a .dat for all as it's little overhead.
            if (audioProject.SoundBanks.Any(soundBank => soundBank.ActionEvents != null))
                _datGeneratorService.GenerateEventDatFile(audioProject, audioProjectNameFileWithoutExtension);

            // We create the states .dat file so we can see the modded states in the Audio Explorer, it isn't necessary for the game.
            if (audioProject.StateGroups != null)
                _datGeneratorService.GenerateStatesDatFile(audioProject, audioProjectFileNameWithoutSpaces);

            var compiledAudioProjectFileName = audioProjectFileName.Replace(".aproj", "_compiled.json");
            var compiledAudioProjectFilePath = audioProjectFilePath.Replace(".aproj", "_compiled.json");
            _audioProjectFileService.Save(audioProject, compiledAudioProjectFileName, compiledAudioProjectFilePath);

            MemoryOptimiser.Optimise();
        }

        private static void SetSoundBankData(AudioProject audioProject, string audioProjectNameFileWithoutExtension)
        {
            foreach (var soundBank in audioProject.SoundBanks)
            {
                soundBank.GameSoundBank = Wh3SoundBankInformation.GetSoundBank(soundBank.Name);
                soundBank.Language = AudioProjectCompilerHelpers.GetCorrectSoundBankLanguage(audioProject.Language, soundBank.GameSoundBank);
                soundBank.LanguageId = WwiseHash.Compute(soundBank.Language);

                soundBank.FileName = $"{soundBank.Name}_{audioProjectNameFileWithoutExtension}.bnk";
                if (soundBank.Language == Wh3LanguageInformation.GetGameLanguageAsString(Wh3GameLanguage.Sfx))
                    soundBank.FilePath = $"audio\\wwise\\{soundBank.FileName}";
                else
                    soundBank.FilePath = $"audio\\wwise\\{soundBank.Language}\\{soundBank.FileName}";
                soundBank.Id = WwiseHash.Compute(soundBank.FileName.Replace(".bnk", string.Empty));

                if (soundBank.DialogueEvents != null)
                {
                    // In WH3 .bnk files are loaded in descending name order. When a .bnk is loaded it overrides hircs with the same ID in .bnks loaded
                    // before it so the .bnk with the lowest alphanumeric name takes priority.
                    // Example load order:
                    // 1) campaign_vo__core.bnk
                    // 2) campaign_vo_1_project_dialogue_events_for_merging.bnk
                    // 3) campaign_vo_0_project_dialogue_events_for_testing.bnk
                    // So the dialogue events from campaign_vo_0_project_dialogue_events_for_testing.bnk will be what take priority as they're loaded last.
                    soundBank.DialogueEventsSplitTestingFileName = $"{soundBank.Name}_0_{audioProjectNameFileWithoutExtension}_dialogue_events_for_testing.bnk";
                    soundBank.DialogueEventsSplitMergingFileName = $"{soundBank.Name}_{audioProjectNameFileWithoutExtension}_dialogue_events_for_merging.bnk";
                    if (soundBank.Language == Wh3LanguageInformation.GetGameLanguageAsString(Wh3GameLanguage.Sfx))
                    {
                        soundBank.DialogueEventsSplitTestingFilePath = $"audio\\wwise\\{soundBank.DialogueEventsSplitTestingFileName}";
                        soundBank.DialogueEventsSplitMergingFilePath = $"audio\\wwise\\{soundBank.DialogueEventsSplitMergingFileName}";
                    }
                    else
                    {
                        soundBank.DialogueEventsSplitTestingFilePath = $"audio\\wwise\\{soundBank.Language}\\{soundBank.DialogueEventsSplitTestingFileName}";
                        soundBank.DialogueEventsSplitMergingFilePath = $"audio\\wwise\\{soundBank.Language}\\{soundBank.DialogueEventsSplitMergingFileName}";
                    }
                    soundBank.DialogueEventsSplitTestingId = WwiseHash.Compute(soundBank.DialogueEventsSplitTestingFileName.Replace(".bnk", string.Empty));
                    soundBank.DialogueEventsSplitMergingId = WwiseHash.Compute(soundBank.DialogueEventsSplitMergingFileName.Replace(".bnk", string.Empty));
                }
            }
        }

        private void SetPlayActionEventData(AudioProject audioProject, HashSet<uint> usedCompilerHircIds)
        {
            foreach (var soundBank in audioProject.SoundBanks)
            {
                var playActionEvents = soundBank.GetPlayActionEvents();
                foreach (var playActionEvent in playActionEvents)
                {
                    var actionEventIdResult = _idGeneratorService.GenerateActionEventHircId(soundBank.LanguageId, soundBank.FilePath, usedCompilerHircIds, playActionEvent.Name);
                    AudioProjectCompilerHelpers.StoreUsedId(usedCompilerHircIds, actionEventIdResult.Id);
                    playActionEvent.Id = actionEventIdResult.Id;
                }
            }
        }

        private void SetPlayActionTargetData(AudioProject audioProject, HashSet<uint> usedCompilerHircIds, Dictionary<string, uint> sourceIdByWavFilePath, List<Sound> soundsToGenerateWemsFrom)
        {
            foreach (var soundBank in audioProject.SoundBanks)
            {
                var playActionEvents = soundBank.GetPlayActionEvents();
                foreach (var playActionEvent in playActionEvents)
                {
                    if (playActionEvent.Actions.Count > 1)
                        throw new NotSupportedException("Multiple Actions are not supported.");

                    var actorMixerId = Wh3ActionEventInformation.GetActorMixerId(playActionEvent.ActionEventType);
                    var overrideBusId = Wh3ActionEventInformation.GetOverrideBusId(playActionEvent.ActionEventType);

                    foreach (var playAction in playActionEvent.Actions)
                    {
                        if (playAction.Sound != null)
                        {
                            SetSoundData(
                                playAction.Sound,
                                soundBank,
                                usedCompilerHircIds,
                                sourceIdByWavFilePath,
                                soundsToGenerateWemsFrom,
                                actorMixerId,
                                overrideBusId,
                                isSource: true);
                        }
                        else
                        {
                            SetRandomSequenceContainerData(
                                playAction.RandomSequenceContainer,
                                soundBank,
                                usedCompilerHircIds,
                                sourceIdByWavFilePath,
                                soundsToGenerateWemsFrom,
                                actionEventName: playActionEvent.Name,
                                directParentId: actorMixerId,
                                overrideBusId: overrideBusId);
                        }
                    }
                }
            }
        }

        private void SetPlayActionData(AudioProject audioProject, HashSet<uint> usedCompilerHircIds)
        {
            foreach (var soundBank in audioProject.SoundBanks)
            {
                var playActionEvents = soundBank.GetPlayActionEvents();
                foreach (var playActionEvent in playActionEvents)
                {
                    if (playActionEvent.Actions.Count > 1)
                        throw new NotSupportedException("Multiple Actions are not supported.");

                    var actionIndex = 0;
                    var actorMixerId = Wh3ActionEventInformation.GetActorMixerId(playActionEvent.ActionEventType);
                    var overrideBusId = Wh3ActionEventInformation.GetOverrideBusId(playActionEvent.ActionEventType);

                    foreach (var playAction in playActionEvent.Actions)
                    {
                        actionIndex++;

                        if (playAction.Sound != null)
                        {
                            var actionIdResult = _idGeneratorService.GenerateActionHircId(soundBank.LanguageId, soundBank.FilePath, usedCompilerHircIds, playActionEvent.Name, actionIndex);
                            AudioProjectCompilerHelpers.StoreUsedId(usedCompilerHircIds, actionIdResult.Id);
                            playAction.Name = actionIdResult.FinalKey;
                            playAction.Id = actionIdResult.Id;
                            playAction.IdExt = playAction.Sound.Id;
                        }
                        else
                        {
                            var actionIdResult = _idGeneratorService.GenerateActionHircId(soundBank.LanguageId, soundBank.FilePath, usedCompilerHircIds, playActionEvent.Name, actionIndex);
                            AudioProjectCompilerHelpers.StoreUsedId(usedCompilerHircIds, actionIdResult.Id);
                            playAction.Name = actionIdResult.FinalKey;
                            playAction.Id = actionIdResult.Id;
                            playAction.IdExt = playAction.RandomSequenceContainer.Id;
                        }
                    }
                }
            }
        }

        private void SetStopActionEventData(AudioProject audioProject, HashSet<uint> usedCompilerHircIds)
        {
            foreach (var soundBank in audioProject.SoundBanks)
            {
                var stopActionEvents = soundBank.GetStopActionEvents();
                foreach (var stopActionEvent in stopActionEvents)
                {
                    var actionEventIdResult = _idGeneratorService.GenerateActionEventHircId(soundBank.LanguageId, soundBank.FilePath, usedCompilerHircIds, stopActionEvent.Name);
                    AudioProjectCompilerHelpers.StoreUsedId(usedCompilerHircIds, actionEventIdResult.Id);
                    stopActionEvent.Id = actionEventIdResult.Id;
                }
            }
        }

        private void SetStopActionData(AudioProject audioProject, HashSet<uint> usedCompilerHircIds)
        {
            foreach (var soundBank in audioProject.SoundBanks)
            {
                var stopActionEvents = soundBank.GetStopActionEvents();
                foreach (var stopActionEvent in stopActionEvents)
                {
                    if (stopActionEvent.Actions.Count > 1)
                        throw new NotSupportedException("Multiple Actions are not supported.");

                    var actionIndex = 0;
                    var actorMixerId = Wh3ActionEventInformation.GetActorMixerId(stopActionEvent.ActionEventType);
                    var overrideBusId = Wh3ActionEventInformation.GetOverrideBusId(stopActionEvent.ActionEventType);

                    var stopActions = stopActionEvent.GetStopActions();
                    foreach (var stopAction in stopActions)
                    {
                        actionIndex++;

                        if (stopAction.Sound != null)
                        {
                            var actionIdResult = _idGeneratorService.GenerateActionHircId(soundBank.LanguageId, soundBank.FilePath, usedCompilerHircIds, stopActionEvent.Name, actionIndex);
                            AudioProjectCompilerHelpers.StoreUsedId(usedCompilerHircIds, actionIdResult.Id);
                            stopAction.Name = actionIdResult.FinalKey;
                            stopAction.Id = actionIdResult.Id;

                            var playActionEvent = AudioProjectCompilerHelpers.GetPlayActionEventFromStopActionEventName(soundBank, stopActionEvent.Name);
                            var actionNameStart = $"action_hirc_{playActionEvent.Name}";
                            var playAction = playActionEvent.GetAction(actionNameStart);
                            stopAction.IdExt = playAction.Sound.Id;
                            stopAction.Sound = playAction.Sound;
                        }
                        else
                        {
                            var actionIdResult = _idGeneratorService.GenerateActionHircId(soundBank.LanguageId, soundBank.FilePath, usedCompilerHircIds, stopActionEvent.Name, actionIndex);
                            AudioProjectCompilerHelpers.StoreUsedId(usedCompilerHircIds, actionIdResult.Id);
                            stopAction.Name = actionIdResult.FinalKey;
                            stopAction.Id = actionIdResult.Id;

                            var playActionEvent = AudioProjectCompilerHelpers.GetPlayActionEventFromStopActionEventName(soundBank, stopActionEvent.Name);
                            var actionNameStart = $"action_hirc_{playActionEvent.Name}";
                            var playAction = playActionEvent.GetAction(actionNameStart);
                            stopAction.IdExt = playAction.RandomSequenceContainer.Id;
                            stopAction.RandomSequenceContainer = playAction.RandomSequenceContainer;
                        }
                    }
                }
            }
        }

        private static void SetDialogueEventData(AudioProject audioProject)
        {
            foreach (var soundBank in audioProject.SoundBanks)
            {
                foreach (var dialogueEvent in soundBank.DialogueEvents)
                {
                    foreach (var statePath in dialogueEvent.StatePaths)
                    {
                        foreach (var statePathNode in statePath.Nodes)
                        {
                            statePathNode.StateGroup.Id = WwiseHash.Compute(statePathNode.StateGroup.Name);

                            if (statePathNode.State.Name == "Any")
                                statePathNode.State.Id = 0;
                            else
                                statePathNode.State.Id = WwiseHash.Compute(statePathNode.State.Name);
                        }

                        dialogueEvent.Id = WwiseHash.Compute(dialogueEvent.Name);
                    }
                }
            }
        }

        private void SetDialogueEventSourceData(
            AudioProject audioProject,
            HashSet<uint> usedCompilerHircIds,
            Dictionary<string, uint> sourceIdByWavFilePath,
            List<Sound> soundsToGenerateWemsFrom)
        {
            foreach (var soundBank in audioProject.SoundBanks)
            {
                foreach (var dialogueEvent in soundBank.DialogueEvents)
                {
                    var actorMixerId = Wh3DialogueEventInformation.GetActorMixerId(dialogueEvent.Name);

                    foreach (var statePath in dialogueEvent.StatePaths)
                    {
                        if (statePath.Sound != null)
                            SetSoundData(
                                statePath.Sound,
                                soundBank,
                                usedCompilerHircIds,
                                sourceIdByWavFilePath,
                                soundsToGenerateWemsFrom,
                                directParentId: actorMixerId,
                                isSource: true);
                        else
                            SetRandomSequenceContainerData(
                                statePath.RandomSequenceContainer,
                                soundBank,
                                usedCompilerHircIds,
                                sourceIdByWavFilePath,
                                soundsToGenerateWemsFrom,
                                dialogueEventName: dialogueEvent.Name,
                                statePath: statePath,
                                directParentId: actorMixerId);
                    }
                }
            }
        }

        private static void SetStateGroupData(AudioProject audioProject)
        {
            foreach (var stateGroup in audioProject.StateGroups)
            {
                stateGroup.Id = WwiseHash.Compute(stateGroup.Name);

                foreach (var state in stateGroup.States)
                {
                    if (state.Name == "Any")
                        state.Id = 0;
                    else
                        state.Id = WwiseHash.Compute(state.Name);
                }
            }
        }

        private void SetRandomSequenceContainerData(
            RandomSequenceContainer container,
            SoundBank soundBank,
            HashSet<uint> usedCompilerHircIds,
            Dictionary<string, uint> sourceIdByWavFilePath,
            List<Sound> soundsToGenerateWemsFrom,
            string actionEventName = null,
            string dialogueEventName = null,
            StatePath statePath = null,
            uint directParentId = 0,
            uint overrideBusId = 0)
        {
            if (actionEventName != null)
            {
                var generatedIdResult = _idGeneratorService.GenerateRanSeqCntrActionEventHircId(soundBank.LanguageId, soundBank.FilePath, usedCompilerHircIds, actionEventName);
                AudioProjectCompilerHelpers.StoreUsedId(usedCompilerHircIds, generatedIdResult.Id);
                container.Name = generatedIdResult.FinalKey;
                container.Id = generatedIdResult.Id;
            }
            else if (dialogueEventName != null)
            {
                var generatedIdResult = _idGeneratorService.GenerateRanSeqCntrDialogueEventHircId(soundBank.LanguageId, soundBank.FilePath, usedCompilerHircIds, dialogueEventName, statePath);
                AudioProjectCompilerHelpers.StoreUsedId(usedCompilerHircIds, generatedIdResult.Id);
                container.Name = generatedIdResult.FinalKey;
                container.Id = generatedIdResult.Id;
            }

            container.OverrideBusId = overrideBusId;
            container.DirectParentId = directParentId;

            foreach (var sound in container.Sounds)
                SetSoundData(sound, soundBank, usedCompilerHircIds, sourceIdByWavFilePath, soundsToGenerateWemsFrom, directParentId: container.Id);

            container.Sounds = container.Sounds.OrderBy(sound => sound.Id).ToList();
        }

        private void SetSoundData(
            Sound sound,
            SoundBank soundBank,
            HashSet<uint> usedCompilerHircIds,
            Dictionary<string, uint> sourceIdByWavFilePath,
            List<Sound> soundsToGenerateWemsFrom,
            uint directParentId = 0,
            uint overrideBusId = 0,
            bool isSource = false)
        {
            sound.Language = soundBank.Language;
            var soundFileNameWithoutExtension = Path.GetFileNameWithoutExtension(sound.WavPackFileName);

            // Set these before we get the ID as these form part of the soundKey.
            sound.OverrideBusId = overrideBusId;
            sound.DirectParentId = directParentId;

            var soundKey = $"{soundFileNameWithoutExtension}_{sound.GetAsString()}";
            var generatedSoundIdResult = _idGeneratorService.GenerateSoundHircId(soundBank.LanguageId, soundBank.FilePath, usedCompilerHircIds, soundKey);
            AudioProjectCompilerHelpers.StoreUsedId(usedCompilerHircIds, generatedSoundIdResult.Id);
            sound.Name = generatedSoundIdResult.FinalKey;
            sound.Id = generatedSoundIdResult.Id;

            if (!sourceIdByWavFilePath.TryGetValue(sound.WavPackFilePath, out var sourceId))
            {
                var usedCompilerSourceIds = sourceIdByWavFilePath.Values.ToHashSet();
                var generatedWemIdResult = _idGeneratorService.GenerateWemId(soundBank.LanguageId, soundBank.FilePath, usedCompilerSourceIds, soundFileNameWithoutExtension);
                sourceId = generatedWemIdResult.Id;
                sourceIdByWavFilePath[sound.WavPackFilePath] = sourceId;
            }

            sound.SourceId = sourceId;
            sound.WemPackFileName = $"{sound.SourceId}.wem";
            sound.WemDiskFilePath = $"{DirectoryHelper.Temp}\\Audio\\{sound.WemPackFileName}";
            if (soundBank.Language == Wh3LanguageInformation.GetGameLanguageAsString(Wh3GameLanguage.Sfx))
                sound.WemPackFilePath = $"audio\\wwise\\{sound.WemPackFileName}";
            else
                sound.WemPackFilePath = $"audio\\wwise\\{sound.Language}\\{sound.WemPackFileName}";

            // Store sounds for batch .wem generation.
            soundsToGenerateWemsFrom.Add(sound);
        }

        public void SaveWemsToPack(List<Sound> soundsToGenerateWemsFrom)
        {
            foreach (var sound in soundsToGenerateWemsFrom)
                _wemGeneratorService.SaveWemToPack(sound.WemDiskFilePath, sound.WemPackFilePath);
        }

        public static void UpdateSoundMemoryMediaSize(List<Sound> soundsToGenerateWemsFrom)
        {
            foreach (var sound in soundsToGenerateWemsFrom)
            {
                var wemFileInfo = new FileInfo(sound.WemDiskFilePath);
                var fileSizeInBytes = wemFileInfo.Length;
                sound.InMemoryMediaSize = fileSizeInBytes;
            }
        }
    }
}
