using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Editors.Audio.AudioEditor.Models;
using Editors.Audio.AudioProjectCompiler;
using Editors.Audio.GameInformation.Warhammer3;
using Editors.Audio.Storage;
using Editors.Audio.Utility;
using Shared.Core.PackFiles;
using Shared.GameFormats.Wwise.Hirc;

namespace Editors.Audio.AudioEditor
{
    public interface IAudioEditorIntegrityService
    {
        void CheckDialogueEventInformationIntegrity(List<Wh3DialogueEventDefinition> dialogueEventData);
        void CheckAudioProjectDialogueEventIntegrity(AudioProject audioProject);
        void CheckAudioProjectWavFilesIntegrity(AudioProject audioProject);
        void CheckAudioProjectDataIntegrity(AudioProject audioProject, string audioProjectFileNameWithoutExtension);
        void CheckMergingSoundBanksIdIntegrity();
    }

    public class AudioEditorIntegrityService(IPackFileService packFileService, IAudioRepository audioRepository) : IAudioEditorIntegrityService
    {
        private readonly IPackFileService _packFileService = packFileService;
        private readonly IAudioRepository _audioRepository = audioRepository;

        public void CheckDialogueEventInformationIntegrity(List<Wh3DialogueEventDefinition> information)
        {
            var exclusions = new List<string> { "New_Dialogue_Event", "Battle_Individual_Melee_Weapon_Hit" };
            var gameDialogueEvents = _audioRepository.StateGroupsByDialogueEvent.Keys.Except(exclusions).ToList();
            var audioEditorDialogueEvents = information.Select(item => item.Name).ToList();
            var areGameAndAudioEditorDialogueEventsMatching = new HashSet<string>(gameDialogueEvents).SetEquals(audioEditorDialogueEvents);
            if (!areGameAndAudioEditorDialogueEventsMatching)
            {
                var dialogueEventsOnlyInGame = gameDialogueEvents.Except(audioEditorDialogueEvents).ToList();
                var dialogueEventsOnlyInAudioEditor = audioEditorDialogueEvents.Except(gameDialogueEvents).ToList();

                var dialogueEventsOnlyInGameText = string.Join("\n - ", dialogueEventsOnlyInGame);
                var dialogueEventsOnlyInAudioEditorText = string.Join("\n - ", dialogueEventsOnlyInAudioEditor);

                var message = $"Dialogue Event integrity check failed." +
                    $"\n\nThis is due to a change in the game's Dialogue Events by CA." +
                    $"\n\nPlease report this error to the AssetEditor development team.";
                var dialogueEventsOnlyInGameMessage = $"\n\nGame Dialogue Events not in the Audio Editor:\n - {dialogueEventsOnlyInGameText}";
                var dialogueEventsOnlyInAudioEditorMessage = $"\n\nAudio Editor Dialogue Events not in the game:\n - {dialogueEventsOnlyInAudioEditorText}";

                if (dialogueEventsOnlyInGame.Count > 0)
                    message += dialogueEventsOnlyInGameMessage;

                if (dialogueEventsOnlyInAudioEditor.Count > 0)
                    message += dialogueEventsOnlyInAudioEditorMessage;

                MessageBox.Show(message, "Error");
            }
        }

        public void CheckAudioProjectDialogueEventIntegrity(AudioProject audioProject)
        {
            var audioProjectDialogueEventsWithStateGroups = new Dictionary<string, List<string>>();
            var dialogueEventsWithStateGroupsWithIntegrityError = new Dictionary<string, List<string>>();
            var hasIntegrityError = false;

            var message = $"Dialogue Events State Groups integrity check failed." +
                $"\n\nThis is likely due to a change in the State Groups by a recent update to the game or you've done something silly with the file." +
                $"\n\nWhen browsing the affected Dialogue Events you will see the rows have been updated to accommodate for the new State Groups, and if any of the old State Groups are no longer used they will have been removed." +
                $" The new State Group(s) will have no State set in them so you need to click Update Row and add the State(s). " +
                $"\n\nAffected Dialogue Events:";

            foreach (var soundBank in audioProject.SoundBanks)
            {
                if (Wh3DialogueEventInformation.Contains(soundBank.GameSoundBank))
                {
                    foreach (var dialogueEvent in soundBank.DialogueEvents)
                    {
                        var firstStatePath = dialogueEvent.StatePaths.FirstOrDefault();
                        if (firstStatePath != null)
                        {
                            var gameDialogueEventStateGroups = _audioRepository.StateGroupsByDialogueEvent[dialogueEvent.Name];
                            var audioProjectDialogueEventStateGroups = firstStatePath.Nodes.Select(node => node.StateGroup.Name).ToList();
                            if (!gameDialogueEventStateGroups.SequenceEqual(audioProjectDialogueEventStateGroups))
                            {
                                if (!hasIntegrityError)
                                    hasIntegrityError = true;

                                message += $"\n\nDialogue Event: {dialogueEvent.Name}";

                                var audioProjectDialogueEventStateGroupsText = string.Join("-->", audioProjectDialogueEventStateGroups);
                                message += $"\n\nOld State Groups: {audioProjectDialogueEventStateGroupsText}";

                                var gameDialogueEventStateGroupsText = string.Join("-->", gameDialogueEventStateGroups);
                                message += $"\n\nNew State Groups: {gameDialogueEventStateGroupsText}";
                            }
                        }
                    }
                }
            }

            if (hasIntegrityError)
                MessageBox.Show(message, "Error");
        }

        public void CheckAudioProjectWavFilesIntegrity(AudioProject audioProject)
        {
            var missingWavFiles = new List<string>();
            var sounds = audioProject.GetSounds();
            var distinctSounds = sounds.DistinctBy(sound => sound.WavPackFilePath);
            foreach (var sound in distinctSounds)
            {
                var wavPath = sound.WavPackFilePath;
                if (string.IsNullOrWhiteSpace(wavPath))
                    continue;

                var packFile = _packFileService.FindFile(wavPath);
                if (packFile == null)
                    missingWavFiles.Add(wavPath);
            }

            if (missingWavFiles.Count > 0)
            {
                var sortedMissingWavFiles = missingWavFiles.OrderBy(wavFilePath => wavFilePath).ToList();
                var missingWavFilesText = string.Join("\n - ", sortedMissingWavFiles);

                var message =
                    $"Wav files integrity check failed." +
                    $"\n\nThe following wav files could not be found:" +
                    $"\n - {missingWavFilesText}" +
                    $"\n\nEnsure all wav files are in the correct location or update their usage in the Audio Project to the correct path.";

                MessageBox.Show(message, "Error");
            }
        }

        public void CheckAudioProjectDataIntegrity(AudioProject audioProject, string audioProjectFileNameWithoutExtension)
        {
            var usedHircIds = new HashSet<uint>();
            var usedSourceIds = new HashSet<uint>();

            var audioProjectGeneratableItemIds = audioProject.GetGeneratableItemIds();
            var audioProjectSourceIds = audioProject.GetSourceIds();

            var languageId = WwiseHash.Compute(audioProject.Language);
            var gameLanguageHircIds = _audioRepository.GetUsedHircIdsByLanguageId(languageId);
            var gameLanguageSourceIds = _audioRepository.GetUsedSourceIdsByLanguageId(languageId);

            // Reset and remove any AudioProjectItem IDs used in vanilla so we can update them
            var hircIdConflicts = new List<uint>();
            var generatableItems = audioProject.GetGeneratableItems();
            foreach (var generatableItem in generatableItems)
            {
                if (generatableItem.Id == 0)
                    continue;

                if (gameLanguageHircIds.Contains(generatableItem.Id) && generatableItem is not DialogueEvent)
                {
                    hircIdConflicts.Add(generatableItem.Id);
                    audioProjectGeneratableItemIds.Remove(generatableItem.Id);
                    generatableItem.Guid = Guid.Empty;
                    generatableItem.Id = 0;
                }
            }

            if (hircIdConflicts.Count > 0)
            {
                MessageBox.Show(
                    "Detected ID conflicts with vanilla for the following Hirc IDs:\n" + 
                    string.Join(", ", hircIdConflicts),
                    "Error"
                );
            }

            // Reset and remove any Source IDs used in vanilla so we can update them
            var sourceIdConflicts = new List<uint>();
            var audioProjectSounds = audioProject.GetSounds();
            foreach (var sound in audioProjectSounds)
            {
                if (sound.SourceId == 0)
                    continue;

                if (gameLanguageSourceIds.Contains(sound.SourceId))
                {
                    sourceIdConflicts.Add(sound.Id);
                    audioProjectSourceIds.Remove(sound.SourceId);
                    sound.SourceId = 0;
                }
            }

            if (sourceIdConflicts.Count > 0)
            {
                MessageBox.Show(
                    "Detected Source ID conflicts with vanilla for the following .wem files:\n" +
                    string.Join(", ", sourceIdConflicts.Select(id => id + ".wem")),
                    "Error"
                );
            }

            usedHircIds.UnionWith(audioProjectGeneratableItemIds);
            usedHircIds.UnionWith(gameLanguageHircIds);
            usedSourceIds.UnionWith(audioProjectSourceIds);
            usedSourceIds.UnionWith(gameLanguageSourceIds);

            foreach (var soundBank in audioProject.SoundBanks)
            {
                ResolveSoundBankDataIntegrity(audioProject, audioProjectFileNameWithoutExtension, soundBank);

                if (soundBank.ActionEvents != null)
                {
                    ResolvePlayActionEventDataIntegrity(usedHircIds, usedSourceIds, soundBank);
                    ResolvePauseActionEventDataIntegrity(usedHircIds, soundBank);
                    ResolveResumeActionEventDataIntegrity(usedHircIds, soundBank);
                    ResolveStopActionEventDataIntegrity(usedHircIds, soundBank);
                }

                if (soundBank.DialogueEvents != null)
                    ResolveDialogueEventDataIntegrity(usedHircIds, usedSourceIds, soundBank);
            }

            ResolveStateGroupDataIntegrity(audioProject);
        }

        private static void ResolveSoundBankDataIntegrity(AudioProject audioProject, string audioProjectFileNameWithoutExtension, SoundBank soundBank)
        {
            var gameSoundBankName = Wh3SoundBankInformation.GetSoundBankNameFromPrefix(soundBank.Name);
            var gameSoundBank = Wh3SoundBankInformation.GetSoundBank(gameSoundBankName);
            var correctSoundBankName = $"{gameSoundBankName}_{audioProjectFileNameWithoutExtension}";

            if (soundBank.Name != correctSoundBankName)
                soundBank.Name = correctSoundBankName;

            if (soundBank.Id == 0)
                soundBank.Id = WwiseHash.Compute(soundBank.Name);

            if (soundBank.GameSoundBank != gameSoundBank)
                soundBank.GameSoundBank = gameSoundBank;

            if (soundBank.Language == null)
            {
                var language = audioProject.Language;
                var requiredLanguage = Wh3SoundBankInformation.GetRequiredLanguage(soundBank.GameSoundBank);
                if (requiredLanguage != null)
                    language = Wh3LanguageInformation.GetGameLanguageAsString((Wh3GameLanguage)requiredLanguage);
                soundBank.Language = language;
            }

            if (soundBank.LanguageId == 0)
                soundBank.LanguageId = WwiseHash.Compute(soundBank.Language);
        }

        private static void ResolvePlayActionEventDataIntegrity(HashSet<uint> usedHircIds, HashSet<uint> usedSourceIds, SoundBank soundBank)
        {
            var playActionEvents = soundBank.GetPlayActionEvents();
            foreach (var playActionEvent in playActionEvents)
            {
                if (playActionEvent.Name != playActionEvent.Name)
                    throw new InvalidOperationException("The Play Action Event should have a name. Check for Action Events without names.");

                if (playActionEvent.Id == 0)
                    playActionEvent.Id = IdGenerator.GenerateActionEventId(usedHircIds, playActionEvent.Name);

                if (playActionEvent.Actions.Count > 1)
                    throw new NotSupportedException("Multiple Actions are not supported.");

                var overrideBusId = Wh3ActionEventInformation.GetOverrideBusId(playActionEvent.ActionEventType);
                var actorMixerId = Wh3ActionEventInformation.GetActorMixerId(playActionEvent.ActionEventType);

                foreach (var playAction in playActionEvent.Actions)
                {
                    var playActionName = $"{playActionEvent.Name}_action";
                    if (playAction.Name != playActionName)
                        playAction.Name = playActionName;

                    if (playAction.Id == 0)
                        playAction.Id = IdGenerator.GenerateActionId(usedHircIds, playAction.Name, playActionEvent.Name);

                    if (playAction.Sound != null)
                    {
                        ResolveSoundDataIntegrity(
                            playAction.Sound,
                            soundBank,
                            usedHircIds,
                            usedSourceIds,
                            overrideBusId: overrideBusId,
                            directParentId: actorMixerId);
                    }
                    else
                    {
                        ResolveRandomSequenceContainerDataIntegrity(
                            playAction.RandomSequenceContainer,
                            soundBank,
                            usedHircIds,
                            usedSourceIds,
                            overrideBusId: overrideBusId,
                            directParentId: actorMixerId);
                    }
                }

                foreach (var playAction in playActionEvent.Actions)
                {
                    var idGeneratorResult = IdGenerator.GenerateAudioProjectGeneratableItemIds(usedHircIds);
                    if (playAction.Guid == Guid.Empty || playAction.Id == 0)
                    {
                        playAction.Guid = idGeneratorResult.Guid;
                        playAction.Id = idGeneratorResult.Id;
                    }

                    if (playAction.Sound != null)
                    {
                        if (playAction.IdExt == 0)
                            playAction.IdExt = playAction.Sound.Id;
                    }
                    else if (playAction.IdExt == 0)
                        playAction.IdExt = playAction.RandomSequenceContainer.Id;

                    if (playAction.GameSoundBank == Wh3SoundBank.None)
                        playAction.GameSoundBank = soundBank.GameSoundBank;

                    if (playAction.BankId == 0)
                        playAction.BankId = soundBank.Id;
                }
            }
        }

        private static void ResolvePauseActionEventDataIntegrity(HashSet<uint> usedHircIds, SoundBank soundBank)
        {
            var pauseActionEvents = soundBank.GetPauseActionEvents();
            foreach (var pauseActionEvent in pauseActionEvents)
            {
                var playActionEvent = soundBank.GetPlayActionEventFromPauseActionEventName(pauseActionEvent.Name);
                var playActionName = $"{playActionEvent.Name}_action";
                var playAction = playActionEvent.GetAction(playActionName);

                if (pauseActionEvent.Name == null)
                    pauseActionEvent.Name = string.Concat("Pause_", playActionEvent.Name.AsSpan("Play_".Length));

                if (pauseActionEvent.Id == 0)
                    pauseActionEvent.Id = IdGenerator.GenerateActionEventId(usedHircIds, pauseActionEvent.Name);

                if (pauseActionEvent.Actions.Count > 1)
                    throw new NotSupportedException("Multiple Actions are not supported.");

                var pauseActions = pauseActionEvent.GetPauseActions();
                foreach (var pauseAction in pauseActions)
                {
                    var idGeneratorResult = IdGenerator.GenerateAudioProjectGeneratableItemIds(usedHircIds);
                    if (pauseAction.Guid == Guid.Empty || pauseAction.Id == 0)
                    {
                        pauseAction.Guid = idGeneratorResult.Guid;
                        pauseAction.Id = idGeneratorResult.Id;
                    }

                    if (pauseAction.Name != playActionName)
                        pauseAction.Name = string.Concat("Pause_", playActionName.AsSpan("Play_".Length));

                    if (pauseAction.Id == 0)
                        playAction.Id = IdGenerator.GenerateActionId(usedHircIds, pauseAction.Name, playActionEvent.Name);

                    if (pauseAction.Sound != null)
                    {
                        if (pauseAction.IdExt == 0)
                            pauseAction.IdExt = playAction.Sound.Id;

                        if (pauseAction.Sound == null)
                            pauseAction.Sound = playAction.Sound;
                    }
                    else
                    {
                        if (pauseAction.IdExt == 0)
                            pauseAction.IdExt = playAction.RandomSequenceContainer.Id;

                        if (pauseAction.RandomSequenceContainer == null)
                            pauseAction.RandomSequenceContainer = playAction.RandomSequenceContainer;
                    }

                    if (pauseAction.GameSoundBank == Wh3SoundBank.None)
                        pauseAction.GameSoundBank = soundBank.GameSoundBank;
                }
            }
        }

        private static void ResolveResumeActionEventDataIntegrity(HashSet<uint> usedHircIds, SoundBank soundBank)
        {
            var resumeActionEvents = soundBank.GetResumeActionEvents();
            foreach (var resumeActionEvent in resumeActionEvents)
            {
                var playActionEvent = soundBank.GetPlayActionEventFromResumeActionEventName(resumeActionEvent.Name);
                var playActionName = $"{playActionEvent.Name}_action";
                var playAction = playActionEvent.GetAction(playActionName);

                if (resumeActionEvent.Name == null)
                    resumeActionEvent.Name = string.Concat("Resume_", playActionEvent.Name.AsSpan("Play_".Length));

                if (resumeActionEvent.Id == 0)
                    resumeActionEvent.Id = IdGenerator.GenerateActionEventId(usedHircIds, resumeActionEvent.Name);

                if (resumeActionEvent.Actions.Count > 1)
                    throw new NotSupportedException("Multiple Actions are not supported.");

                var resumeActions = resumeActionEvent.GetResumeActions();
                foreach (var resumeAction in resumeActions)
                {
                    var idGeneratorResult = IdGenerator.GenerateAudioProjectGeneratableItemIds(usedHircIds);
                    if (resumeAction.Guid == Guid.Empty || resumeAction.Id == 0)
                    {
                        resumeAction.Guid = idGeneratorResult.Guid;
                        resumeAction.Id = idGeneratorResult.Id;
                    }

                    if (resumeAction.Name != playActionName)
                        resumeAction.Name = string.Concat("Resume_", playActionName.AsSpan("Play_".Length));

                    if (resumeAction.Id == 0)
                        playAction.Id = IdGenerator.GenerateActionId(usedHircIds, resumeAction.Name, playActionEvent.Name);

                    if (resumeAction.Sound != null)
                    {
                        if (resumeAction.IdExt == 0)
                            resumeAction.IdExt = playAction.Sound.Id;

                        if (resumeAction.Sound == null)
                            resumeAction.Sound = playAction.Sound;
                    }
                    else
                    {
                        if (resumeAction.IdExt == 0)
                            resumeAction.IdExt = playAction.RandomSequenceContainer.Id;

                        if (resumeAction.RandomSequenceContainer == null)
                            resumeAction.RandomSequenceContainer = playAction.RandomSequenceContainer;
                    }

                    if (resumeAction.GameSoundBank == Wh3SoundBank.None)
                        resumeAction.GameSoundBank = soundBank.GameSoundBank;
                }
            }
        }

        private static void ResolveStopActionEventDataIntegrity(HashSet<uint> usedHircIds, SoundBank soundBank)
        {
            var stopActionEvents = soundBank.GetStopActionEvents();
            foreach (var stopActionEvent in stopActionEvents)
            {
                var playActionEvent = soundBank.GetPlayActionEventFromStopActionEventName(stopActionEvent.Name);
                var playActionName = $"{playActionEvent.Name}_action";
                var playAction = playActionEvent.GetAction(playActionName);

                if (stopActionEvent.Name == null)
                    stopActionEvent.Name = string.Concat("Stop_", playActionEvent.Name.AsSpan("Play_".Length));

                if (stopActionEvent.Id == 0)
                    stopActionEvent.Id = IdGenerator.GenerateActionEventId(usedHircIds, stopActionEvent.Name);

                if (stopActionEvent.Actions.Count > 1)
                    throw new NotSupportedException("Multiple Actions are not supported.");

                var stopActions = stopActionEvent.GetStopActions();
                foreach (var stopAction in stopActions)
                {
                    var idGeneratorResult = IdGenerator.GenerateAudioProjectGeneratableItemIds(usedHircIds);
                    if (stopAction.Guid == Guid.Empty || stopAction.Id == 0)
                    {
                        stopAction.Guid = idGeneratorResult.Guid;
                        stopAction.Id = idGeneratorResult.Id;
                    }

                    if (stopAction.Name != playActionName)
                        stopAction.Name = string.Concat("Stop_", playActionName.AsSpan("Play_".Length));

                    if (stopAction.Id == 0)
                        playAction.Id = IdGenerator.GenerateActionId(usedHircIds, stopAction.Name, playActionEvent.Name);

                    if (stopAction.Sound != null)
                    {
                        if (stopAction.IdExt == 0)
                            stopAction.IdExt = playAction.Sound.Id;

                        if (stopAction.Sound == null)
                            stopAction.Sound = playAction.Sound;
                    }
                    else
                    {
                        if (stopAction.IdExt == 0)
                            stopAction.IdExt = playAction.RandomSequenceContainer.Id;

                        if (stopAction.RandomSequenceContainer == null)
                            stopAction.RandomSequenceContainer = playAction.RandomSequenceContainer;
                    }

                    if (stopAction.GameSoundBank == Wh3SoundBank.None)
                        stopAction.GameSoundBank = soundBank.GameSoundBank;
                }
            }
        }

        private static void ResolveDialogueEventDataIntegrity(HashSet<uint> usedHircIds, HashSet<uint> usedSourceIds, SoundBank soundBank)
        {
            foreach (var dialogueEvent in soundBank.DialogueEvents)
            {
                if (dialogueEvent.Id == 0)
                    dialogueEvent.Id = WwiseHash.Compute(dialogueEvent.Name);

                var actorMixerId = Wh3DialogueEventInformation.GetActorMixerId(dialogueEvent.Name);

                foreach (var statePath in dialogueEvent.StatePaths)
                {
                    foreach (var statePathNode in statePath.Nodes)
                    {
                        statePathNode.StateGroup.Id = WwiseHash.Compute(statePathNode.StateGroup.Name);

                        if (statePathNode.State.Name == "Any" && statePathNode.State.Id != 0)
                            statePathNode.State.Id = 0;
                        else if (statePathNode.State.Name != "Any" && statePathNode.State.Id == 0)
                            statePathNode.State.Id = WwiseHash.Compute(statePathNode.State.Name);
                    }

                    if (statePath.Sound != null)
                        ResolveSoundDataIntegrity(
                            statePath.Sound,
                            soundBank,
                            usedHircIds,
                            usedSourceIds,
                            directParentId: actorMixerId);
                    else
                        ResolveRandomSequenceContainerDataIntegrity(
                            statePath.RandomSequenceContainer,
                            soundBank,
                            usedHircIds,
                            usedSourceIds,
                            statePath: statePath,
                            directParentId: actorMixerId);
                }
            }
        }

        private static void ResolveStateGroupDataIntegrity(AudioProject audioProject)
        {
            if (audioProject.StateGroups != null)
            {
                foreach (var stateGroup in audioProject.StateGroups)
                {
                    if (stateGroup.Id == 0)
                        stateGroup.Id = WwiseHash.Compute(stateGroup.Name);

                    foreach (var state in stateGroup.States)
                    {
                        if (state.Name == "Any" && state.Id != 0)
                            state.Id = 0;
                        else if (state.Name != "Any" && state.Id == 0)
                            state.Id = WwiseHash.Compute(state.Name);
                    }
                }
            }
        }

        private static void ResolveRandomSequenceContainerDataIntegrity(
            RandomSequenceContainer randomSequenceContainer,
            SoundBank soundBank,
            HashSet<uint> usedHircIds,
            HashSet<uint> usedSourceIds,
            StatePath statePath = null,
            uint overrideBusId = 0,
            uint directParentId = 0)
        {
            var idGeneratorResult = IdGenerator.GenerateAudioProjectGeneratableItemIds(usedHircIds);
            if (randomSequenceContainer.Guid == Guid.Empty || randomSequenceContainer.Id == 0)
            {
                randomSequenceContainer.Guid = idGeneratorResult.Guid;
                randomSequenceContainer.Id = idGeneratorResult.Id;
            }

            if (randomSequenceContainer.OverrideBusId == 0 && overrideBusId != 0)
                randomSequenceContainer.OverrideBusId = overrideBusId;

            if (randomSequenceContainer.DirectParentId == 0 && directParentId != 0)
                randomSequenceContainer.DirectParentId = directParentId;

            var playlistOrder = 0;
            foreach (var sound in randomSequenceContainer.Sounds)
            {
                playlistOrder++;
                ResolveSoundDataIntegrity(sound, soundBank, usedHircIds, usedSourceIds, directParentId: randomSequenceContainer.Id, playlistOrder: playlistOrder);
            }
        }

        private static void ResolveSoundDataIntegrity(
            Sound sound,
            SoundBank soundBank,
            HashSet<uint> usedHircIds,
            HashSet<uint> usedSourceIds,
            uint overrideBusId = 0,
            uint directParentId = 0,
            int playlistOrder = 0)
        {
            var idGeneratorResult = IdGenerator.GenerateAudioProjectGeneratableItemIds(usedHircIds);
            if (sound.Guid == Guid.Empty || sound.Id == 0)
            {
                sound.Guid = idGeneratorResult.Guid;
                sound.Id = idGeneratorResult.Id;
            }

            if (sound.Language == null)
                sound.Language = soundBank.Language;

            if (sound.OverrideBusId == 0 && overrideBusId != 0)
                sound.OverrideBusId = overrideBusId;

            if (sound.DirectParentId == 0 && directParentId != 0)
                sound.DirectParentId = directParentId;

            if (sound.SourceId == 0)
            {
                var sourceId = WwiseHash.Compute(sound.WavPackFilePath);
                if (usedSourceIds.Contains(sourceId))
                    throw new InvalidOperationException($"SourceId is already used. Change the name of {sound.WavPackFilePath}.");
                else
                    sound.SourceId = sourceId;
            }

            if (sound.PlaylistOrder == 0)
                sound.PlaylistOrder = playlistOrder;
        }

        public void CheckMergingSoundBanksIdIntegrity()
        {
            var moddedHircsByBnkByLanguage = _audioRepository.GetModdedHircsByBnkByLanguage();

            var hasClashes = false;
            var messageBuilder = new StringBuilder()
                .AppendLine("Merging SoundBanks ID Integrity Check failed.")
                .AppendLine()
                .AppendLine("The following Hirc IDs are used by multiple SoundBanks within the same language.")
                .AppendLine("For each source SoundBank, the IDs listed also exist in the listed other SounBanks.")
                .AppendLine();

            foreach (var languageEntry in moddedHircsByBnkByLanguage)
            {
                var languageName = languageEntry.Key;
                var hircsByBnkDictionary = languageEntry.Value;

                var idsByBnk = new Dictionary<string, HashSet<uint>>(StringComparer.OrdinalIgnoreCase);
                var bnksById = new Dictionary<uint, HashSet<string>>();

                foreach (var bnkEntry in hircsByBnkDictionary)
                {
                    var bnkName = bnkEntry.Key;
                    if (!idsByBnk.TryGetValue(bnkName, out var idSet))
                    {
                        idSet = [];
                        idsByBnk[bnkName] = idSet;
                    }

                    foreach (var hirc in bnkEntry.Value)
                    {
                        if (hirc is ICAkDialogueEvent)
                            continue;

                        var id = hirc.Id;
                        if (!idSet.Add(id))
                            continue;

                        if (!bnksById.TryGetValue(id, out var bnksContainingThisId))
                        {
                            bnksContainingThisId = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            bnksById[id] = bnksContainingThisId;
                        }

                        bnksContainingThisId.Add(bnkName);
                    }
                }

                var clashingIdsById = bnksById
                    .Where(pair => pair.Value.Count > 1)
                    .ToDictionary(pair => pair.Key, pair => pair.Value);

                if (clashingIdsById.Count == 0)
                    continue;

                hasClashes = true;
                messageBuilder.AppendLine($"Language: {languageName}");

                foreach (var sourceBnk in idsByBnk.Keys.OrderBy(name => name, StringComparer.OrdinalIgnoreCase))
                {
                    var conflictingIdsFromThisBnk = idsByBnk[sourceBnk]
                        .Where(clashingIdsById.ContainsKey)
                        .OrderBy(id => id);

                    var anyConflicts = false;
                    foreach (var id in conflictingIdsFromThisBnk)
                    {
                        anyConflicts = true;
                        var otherBnks = clashingIdsById[id]
                            .Where(other => !string.Equals(other, sourceBnk, StringComparison.OrdinalIgnoreCase))
                            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase);

                        messageBuilder.AppendLine($"Id {id} also in: {string.Join(", ", otherBnks)}");
                    }

                    if (anyConflicts)
                        messageBuilder.AppendLine();
                }

                messageBuilder.AppendLine();
            }

            if (hasClashes)
                MessageBox.Show(messageBuilder.ToString(), "Error");
        }
    }
}
