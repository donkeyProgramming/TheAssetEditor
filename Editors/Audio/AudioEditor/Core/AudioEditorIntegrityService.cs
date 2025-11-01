using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Editors.Audio.Shared.AudioProject.Models;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Editors.Audio.Shared.Storage;
using Editors.Audio.Shared.Wwise;
using Shared.Core.PackFiles;
using Shared.GameFormats.Wwise.Hirc;

namespace Editors.Audio.AudioEditor.Core
{
    public interface IAudioEditorIntegrityService
    {
        void CheckDialogueEventInformationIntegrity(List<Wh3DialogueEventDefinition> dialogueEventData);
        void CheckAudioProjectDialogueEventIntegrity(AudioProjectFile audioProject);
        void CheckAudioProjectWavFilesIntegrity(AudioProjectFile audioProject);
        void CheckAudioProjectDataIntegrity(AudioProjectFile audioProject, string audioProjectFileNameWithoutExtension);
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

        public void CheckAudioProjectDialogueEventIntegrity(AudioProjectFile audioProject)
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

        public void CheckAudioProjectWavFilesIntegrity(AudioProjectFile audioProject)
        {
            var missingWavFiles = new List<string>();

            foreach (var audioFile in audioProject.AudioFiles)
            {
                if (string.IsNullOrWhiteSpace(audioFile.WavPackFilePath))
                    continue;

                var packFile = _packFileService.FindFile(audioFile.WavPackFilePath);
                if (packFile == null)
                    missingWavFiles.Add(audioFile.WavPackFilePath);
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

        public void CheckAudioProjectDataIntegrity(AudioProjectFile audioProject, string audioProjectFileNameWithoutExtension)
        {
            var usedHircIds = new HashSet<uint>();
            var usedSourceIds = new HashSet<uint>();

            var audioProjectGeneratableItemIds = audioProject.GetGeneratableItemIds();
            var audioProjectSourceIds = audioProject.GetAudioFileIds();

            var languageId = WwiseHash.Compute(audioProject.Language);
            var languageHircIds = _audioRepository.GetUsedVanillaHircIdsByLanguageId(languageId);
            var languageSourceIds = _audioRepository.GetUsedVanillaSourceIdsByLanguageId(languageId);

            // Reset and remove any AudioProjectItem IDs used in vanilla so we can handle them
            var hircIdConflicts = new List<uint>();
            var generatableItems = audioProject.GetGeneratableItems();
            foreach (var generatableItem in generatableItems)
            {
                if (generatableItem.Id == 0)
                    continue;

                if (languageHircIds.Contains(generatableItem.Id) && generatableItem is not DialogueEvent)
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

            // Reset and remove any Source IDs used in vanilla so we can handle them
            var sourceIdConflicts = new List<uint>();
            var audioProjectSounds = audioProject.GetSounds();
            foreach (var sound in audioProjectSounds)
            {
                if (sound.SourceId == 0)
                    continue;

                if (languageSourceIds.Contains(sound.SourceId))
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
            usedHircIds.UnionWith(languageHircIds);
            usedSourceIds.UnionWith(audioProjectSourceIds);
            usedSourceIds.UnionWith(languageSourceIds);

            foreach (var soundBank in audioProject.SoundBanks)
            {
                ResolveSoundBankDataIntegrity(audioProject, audioProjectFileNameWithoutExtension, soundBank);

                if (soundBank.ActionEvents != null)
                    ResolveActionEventDataIntegrity(usedHircIds, usedSourceIds, soundBank);

                if (soundBank.DialogueEvents != null)
                    ResolveDialogueEventDataIntegrity(usedHircIds, usedSourceIds, soundBank);
            }

            foreach (var audioFile in audioProject.AudioFiles)
                ResolveAudioFileDataIntegrity(audioFile);

            ResolveStateGroupDataIntegrity(audioProject);
        }

        private static void ResolveSoundBankDataIntegrity(AudioProjectFile audioProject, string audioProjectFileNameWithoutExtension, SoundBank soundBank)
        {
            if (soundBank == null)
                throw new InvalidOperationException("SoundBank should not be null.");

            if (string.IsNullOrWhiteSpace(soundBank.Name))
                throw new InvalidOperationException("SoundBank.Name should not be null or empty.");

            var gameSoundBankName = Wh3SoundBankInformation.GetSoundBankNameFromPrefix(soundBank.Name);
            var gameSoundBank = Wh3SoundBankInformation.GetSoundBank(gameSoundBankName);
            var correctSoundBankName = $"{gameSoundBankName}_{audioProjectFileNameWithoutExtension}";

            if (soundBank.Name != correctSoundBankName)
                throw new InvalidOperationException($"SoundBank.Name is incorrect. Expected '{correctSoundBankName}'.");

            if (soundBank.Id == 0)
                throw new InvalidOperationException("SoundBank.Id should not be 0.");

            if (soundBank.GameSoundBank != gameSoundBank)
                throw new InvalidOperationException($"SoundBank.GameSoundBank is incorrect for '{soundBank.Name}'.");

            if (string.IsNullOrWhiteSpace(soundBank.Language))
                throw new InvalidOperationException("SoundBank.Language should not be null or empty.");

            var requiredLanguage = Wh3SoundBankInformation.GetRequiredLanguage(soundBank.GameSoundBank);
            if (requiredLanguage != null)
            {
                var requiredLanguageAsString = Wh3LanguageInformation.GetLanguageAsString((Wh3Language)requiredLanguage);
                if (!string.Equals(soundBank.Language, requiredLanguageAsString, StringComparison.Ordinal))
                    throw new InvalidOperationException($"SoundBank.Language should be '{requiredLanguageAsString}'.");
            }

            if (soundBank.LanguageId == 0)
                throw new InvalidOperationException("SoundBank.LanguageId should not be 0.");
        }

        private static void ResolveActionEventDataIntegrity(HashSet<uint> usedHircIds, HashSet<uint> usedSourceIds, SoundBank soundBank)
        {
            foreach (var actionEvent in soundBank.ActionEvents)
            {
                if (string.IsNullOrWhiteSpace(actionEvent.Name))
                    throw new InvalidOperationException("The Action Event should have a name. Check for Action Events without names.");

                if (actionEvent.Id == 0)
                    throw new InvalidOperationException($"ActionEvent.Id should not be 0 for '{actionEvent.Name}'.");

                if (actionEvent.Actions.Count > 1)
                    throw new NotSupportedException("Multiple Actions are not supported.");

                var overrideBusId = Wh3ActionEventInformation.GetOverrideBusId(actionEvent.ActionEventType);
                var actorMixerId = Wh3ActionEventInformation.GetActorMixerId(actionEvent.ActionEventType);

                foreach (var action in actionEvent.Actions)
                {
                    if (action.Id == 0)
                        throw new InvalidOperationException($"Action.Id should not be 0.");

                    if (action.BankId == 0)
                        throw new InvalidOperationException($"Action.BankId should not be 0.");

                    if (action.TargetHircTypeIsSound())
                    {
                        var sound = soundBank.GetSound(action.TargetHircId);
                        if (sound == null)
                            throw new InvalidOperationException($"Action target Sound not found.");

                        if (action.IdExt == 0)
                            throw new InvalidOperationException($"Action.IdExt should not be 0.");

                        if (overrideBusId != 0 && sound.OverrideBusId == 0)
                            throw new InvalidOperationException($"Sound.OverrideBusId should not be 0 when OverrideBusId is required.");

                        if (actorMixerId != 0 && sound.DirectParentId == 0)
                            throw new InvalidOperationException($"Sound.DirectParentId should not be 0 when DirectParentId is required.");

                        ResolveSoundDataIntegrity(sound, soundBank, usedHircIds, usedSourceIds);
                    }
                    else if (action.TargetHircTypeIsRandomSequenceContainer())
                    {
                        var randomSequenceContainer = soundBank.GetRandomSequenceContainer(action.TargetHircId);
                        if (randomSequenceContainer == null)
                            throw new InvalidOperationException($"Action target RandomSequenceContainer not found.");

                        if (action.IdExt == 0)
                            throw new InvalidOperationException($"Action.IdExt should not be 0.");

                        if (overrideBusId != 0 && randomSequenceContainer.OverrideBusId == 0)
                            throw new InvalidOperationException("RandomSequenceContainer.OverrideBusId should not be 0 when OverrideBusId is required.");

                        if (actorMixerId != 0 && randomSequenceContainer.DirectParentId == 0)
                            throw new InvalidOperationException("RandomSequenceContainer.DirectParentId should not be 0 when DirectParentId is required.");

                        ResolveRandomSequenceContainerDataIntegrity(randomSequenceContainer, soundBank, usedHircIds, usedSourceIds);
                    }
                }
            }
        }

        private static void ResolveDialogueEventDataIntegrity(HashSet<uint> usedHircIds, HashSet<uint> usedSourceIds, SoundBank soundBank)
        {
            foreach (var dialogueEvent in soundBank.DialogueEvents)
            {
                if (string.IsNullOrWhiteSpace(dialogueEvent.Name))
                    throw new InvalidOperationException("DialogueEvent.Name should not be null or empty.");

                if (dialogueEvent.Id == 0)
                    throw new InvalidOperationException($"DialogueEvent.Id should not be 0 for '{dialogueEvent.Name}'.");

                var actorMixerId = Wh3DialogueEventInformation.GetActorMixerId(dialogueEvent.Name);

                foreach (var statePath in dialogueEvent.StatePaths)
                {
                    foreach (var statePathNode in statePath.Nodes)
                    {
                        if (string.IsNullOrWhiteSpace(statePathNode.StateGroup.Name))
                            throw new InvalidOperationException("StateGroup.Name should not be null or empty.");
                        if (statePathNode.StateGroup.Id == 0)
                            throw new InvalidOperationException($"StateGroup.Id should not be 0 for '{statePathNode.StateGroup.Name}'.");

                        if (statePathNode.State.Name == "Any" && statePathNode.State.Id != 0)
                            statePathNode.State.Id = 0;
                        else if (statePathNode.State.Name != "Any" && statePathNode.State.Id == 0)
                            throw new InvalidOperationException($"State.Id should not be 0 for '{statePathNode.State.Name}'.");
                    }

                    if (statePath.TargetHircTypeIsSound())
                    {
                        var sound = soundBank.GetSound(statePath.TargetHircId);
                        if (sound == null)
                            throw new InvalidOperationException($"StatePath target Sound not found for '{dialogueEvent.Name}'.");

                        if (actorMixerId != 0 && sound.DirectParentId == 0)
                            throw new InvalidOperationException($"Sound.DirectParentId should not be 0 for '{sound.Name}'.");

                        ResolveSoundDataIntegrity(sound, soundBank, usedHircIds, usedSourceIds);
                    }
                    else if (statePath.TargetHircTypeIsRandomSequenceContainer())
                    {
                        var randomSequenceContainer = soundBank.GetRandomSequenceContainer(statePath.TargetHircId);
                        if (randomSequenceContainer == null)
                            throw new InvalidOperationException($"StatePath target RandomSequenceContainer not found for '{dialogueEvent.Name}'.");

                        if (actorMixerId != 0 && randomSequenceContainer.DirectParentId == 0)
                            throw new InvalidOperationException("RandomSequenceContainer.DirectParentId should not be 0.");

                        ResolveRandomSequenceContainerDataIntegrity(randomSequenceContainer, soundBank, usedHircIds, usedSourceIds);
                    }
                }
            }
        }

        private static void ResolveStateGroupDataIntegrity(AudioProjectFile audioProject)
        {
            if (audioProject.StateGroups != null)
            {
                foreach (var stateGroup in audioProject.StateGroups)
                {
                    if (string.IsNullOrWhiteSpace(stateGroup.Name))
                        throw new InvalidOperationException("StateGroup.Name should not be null or empty.");
                    if (stateGroup.Id == 0)
                        throw new InvalidOperationException($"StateGroup.Id should not be 0 for '{stateGroup.Name}'.");

                    foreach (var state in stateGroup.States)
                    {
                        if (state.Name == "Any" && state.Id != 0)
                            state.Id = 0;
                        else if (state.Name != "Any" && state.Id == 0)
                            throw new InvalidOperationException($"State.Id should not be 0 for '{state.Name}'.");
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
            if (randomSequenceContainer == null)
                throw new InvalidOperationException("RandomSequenceContainer should not be null.");

            if (randomSequenceContainer.Guid == Guid.Empty)
                throw new InvalidOperationException("RandomSequenceContainer.Guid should not be empty.");

            if (randomSequenceContainer.Id == 0)
                throw new InvalidOperationException("RandomSequenceContainer.Id should not be 0.");

            if (overrideBusId != 0 && randomSequenceContainer.OverrideBusId == 0)
                throw new InvalidOperationException("RandomSequenceContainer.OverrideBusId should not be 0 when OverrideBusId is required.");

            if (directParentId != 0 && randomSequenceContainer.DirectParentId == 0)
                throw new InvalidOperationException("RandomSequenceContainer.DirectParentId should not be 0 when DirectParentId is required.");

            var playlistOrder = 0;
            var sounds = soundBank.GetSounds(randomSequenceContainer.Children);
            foreach (var sound in sounds)
            {
                playlistOrder++;
                ResolveSoundDataIntegrity(sound, soundBank, usedHircIds, usedSourceIds);

                if (sound.DirectParentId == 0)
                    throw new InvalidOperationException($"Sound.DirectParentId should not be 0 for '{sound?.Name}'.");

                if (sound.PlaylistOrder == 0)
                    throw new InvalidOperationException($"Sound.PlaylistOrder should not be 0 for '{sound?.Name}'. Expected '{playlistOrder}'.");
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
            if (sound == null)
                throw new InvalidOperationException("Sound should not be null.");

            if (sound.Guid == Guid.Empty)
                throw new InvalidOperationException("Sound.Guid should not be empty.");

            if (sound.Id == 0)
                throw new InvalidOperationException("Sound.Id should not be 0.");

            if (string.IsNullOrWhiteSpace(sound.Language))
                throw new InvalidOperationException("Sound.Language should not be null or empty.");

            if (overrideBusId != 0 && sound.OverrideBusId == 0)
                throw new InvalidOperationException($"Sound.OverrideBusId should not be 0 for '{sound.Name}'.");

            if (directParentId != 0 && sound.DirectParentId == 0)
                throw new InvalidOperationException($"Sound.DirectParentId should not be 0 for '{sound.Name}'.");

            if (sound.SourceId == 0)
                throw new InvalidOperationException("SourceId should not be 0.");

            if (playlistOrder != 0 && sound.PlaylistOrder == 0)
                throw new InvalidOperationException($"Sound.PlaylistOrder should not be 0 for '{sound.Name}'.");
        }

        private static void ResolveAudioFileDataIntegrity(AudioFile audioFile)
        {
            if (audioFile == null)
                throw new InvalidOperationException("AudioFile should not be null.");

            if (audioFile.Id == 0)
                throw new InvalidOperationException("AudioFile.Id should not be 0.");

            if (audioFile.Guid == Guid.Empty)
                throw new InvalidOperationException("AudioFile.Guid should not be empty.");

            if (string.IsNullOrWhiteSpace(audioFile.WavPackFilePath))
                throw new InvalidOperationException("AudioFile.WavPackFilePath should not be null or empty.");

            if (string.IsNullOrWhiteSpace(audioFile.WavPackFileName))
                throw new InvalidOperationException("AudioFile.WavPackFileName should not be null or empty.");

            if (audioFile.Sounds == null)
                throw new InvalidOperationException("AudioFile.SoundReferences should not be null.");

            if (audioFile.Sounds.Count == 0)
                throw new InvalidOperationException("AudioFile.SoundReferences should not be empty.");
        }

        public void CheckMergingSoundBanksIdIntegrity()
        {
            var moddedHircsByBnkByLanguage = _audioRepository.GetModdedHircsByBnkByLanguage();

            var hasClashes = false;
            var messageBuilder = new StringBuilder()
                .AppendLine("Merging SoundBanks ID Integrity Check failed.")
                .AppendLine()
                .AppendLine("The following Hirc Ids or SourceIds are used by multiple SoundBanks within the same language.")
                .AppendLine("For each source SoundBank, listed Ids also exist in the listed other SoundBanks.")
                .AppendLine();

            foreach (var languageEntry in moddedHircsByBnkByLanguage)
            {
                var languageName = languageEntry.Key;
                var hircsByBnkDictionary = languageEntry.Value;

                var idsByBnk = new Dictionary<string, HashSet<uint>>(StringComparer.OrdinalIgnoreCase);
                var bnksById = new Dictionary<uint, HashSet<string>>();

                var sourceIdsByBnk = new Dictionary<string, HashSet<uint>>(StringComparer.OrdinalIgnoreCase);
                var bnksBySourceId = new Dictionary<uint, HashSet<string>>();

                foreach (var bnkEntry in hircsByBnkDictionary)
                {
                    var bnkName = bnkEntry.Key;

                    if (!idsByBnk.TryGetValue(bnkName, out var idSet))
                    {
                        idSet = [];
                        idsByBnk[bnkName] = idSet;
                    }

                    if (!sourceIdsByBnk.TryGetValue(bnkName, out var sourceIdSet))
                    {
                        sourceIdSet = [];
                        sourceIdsByBnk[bnkName] = sourceIdSet;
                    }

                    foreach (var hirc in bnkEntry.Value)
                    {
                        if (hirc is ICAkDialogueEvent)
                            continue;

                        var id = hirc.Id;
                        if (idSet.Add(id))
                        {
                            if (!bnksById.TryGetValue(id, out var bnksContainingThisId))
                            {
                                bnksContainingThisId = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                                bnksById[id] = bnksContainingThisId;
                            }

                            bnksContainingThisId.Add(bnkName);
                        }

                        if (hirc is ICAkSound sound)
                        {
                            var sourceId = sound.GetSourceId();

                            if (sourceIdSet.Add(sourceId))
                            {
                                if (!bnksBySourceId.TryGetValue(sourceId, out var bnksContainingThisSourceId))
                                {
                                    bnksContainingThisSourceId = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                                    bnksBySourceId[sourceId] = bnksContainingThisSourceId;
                                }

                                bnksContainingThisSourceId.Add(bnkName);
                            }
                        }
                    }
                }

                var clashingIdsById = bnksById
                    .Where(pair => pair.Value.Count > 1)
                    .ToDictionary(pair => pair.Key, pair => pair.Value);

                var clashingSourceIdsById = bnksBySourceId
                    .Where(pair => pair.Value.Count > 1)
                    .ToDictionary(pair => pair.Key, pair => pair.Value);

                if (clashingIdsById.Count == 0 && clashingSourceIdsById.Count == 0)
                    continue;

                hasClashes = true;
                messageBuilder.AppendLine($"Language: {languageName}");

                foreach (var sourceBnk in idsByBnk.Keys
                             .Union(sourceIdsByBnk.Keys, StringComparer.OrdinalIgnoreCase)
                             .OrderBy(name => name, StringComparer.OrdinalIgnoreCase))
                {
                    var anyConflicts = false;

                    if (idsByBnk.TryGetValue(sourceBnk, out var idsFromThisBnk))
                    {
                        var conflictingIdsFromThisBnk = idsFromThisBnk
                            .Where(clashingIdsById.ContainsKey)
                            .OrderBy(value => value);

                        foreach (var id in conflictingIdsFromThisBnk)
                        {
                            anyConflicts = true;
                            var otherBnks = clashingIdsById[id]
                                .Where(other => !string.Equals(other, sourceBnk, StringComparison.OrdinalIgnoreCase))
                                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase);

                            messageBuilder.AppendLine($"Hirc Id {id} also in: {string.Join(", ", otherBnks)}");
                        }
                    }

                    if (sourceIdsByBnk.TryGetValue(sourceBnk, out var sourceIdsFromThisBnk))
                    {
                        var conflictingSourceIdsFromThisBnk = sourceIdsFromThisBnk
                            .Where(clashingSourceIdsById.ContainsKey)
                            .OrderBy(value => value);

                        foreach (var sourceId in conflictingSourceIdsFromThisBnk)
                        {
                            anyConflicts = true;
                            var otherBnks = clashingSourceIdsById[sourceId]
                                .Where(other => !string.Equals(other, sourceBnk, StringComparison.OrdinalIgnoreCase))
                                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase);

                            messageBuilder.AppendLine($"SourceId {sourceId} also in: {string.Join(", ", otherBnks)}");
                        }
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
