using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Editors.Audio.AudioEditor.Models;
using Editors.Audio.AudioProjectCompiler.WwiseIdService;
using Editors.Audio.GameSettings.Warhammer3;
using Editors.Audio.Storage;
using Editors.Audio.Utility;
using Shared.Core.Misc;
using Shared.Core.Settings;
using Shared.GameFormats.Wwise.Enums;
using static Editors.Audio.GameSettings.Warhammer3.SoundBanks;
using Action = Editors.Audio.AudioEditor.Models.Action;

namespace Editors.Audio.AudioProjectCompiler
{
    public class CompilerDataProcessor
    {
        private readonly ApplicationSettingsService _applicationSettingsService;
        private readonly IAudioRepository _audioRepository;

        private Dictionary<uint, HashSet<uint>> UsedHircIdsByLanguageIdLookup { get; set; } = [];
        private Dictionary<uint, HashSet<uint>> UsedSourceIdsByLanguageIdLookup { get; set; } = [];

        public CompilerDataProcessor(ApplicationSettingsService applicationSettingsService, IAudioRepository audioRepository)
        {
            _applicationSettingsService = applicationSettingsService;
            _audioRepository = audioRepository;

            UsedHircIdsByLanguageIdLookup = _audioRepository.HircLookupByLanguageIdById
                .ToDictionary(
                    outer => outer.Key,
                    outer => outer.Value.Keys.ToHashSet()
                );

            UsedSourceIdsByLanguageIdLookup = _audioRepository.SoundHircLookupByLanguageIdBySourceId
                .ToDictionary(
                    outer => outer.Key,
                    outer => outer.Value.Keys.ToHashSet()
                );
        }

        public void SetSoundBankData(AudioProject audioProject)
        {
            foreach (var soundBank in audioProject.SoundBanks)
            {
                soundBank.SoundBankSubtype = GetSoundBankSubtype(soundBank.Name);
                soundBank.Language = AudioProjectCompilerHelpers.GetCorrectSoundBankLanguage(audioProject);

                soundBank.SoundBankFileName = $"{GetSoundBankName(soundBank.SoundBankSubtype)}_{audioProject.FileName}.bnk";

                var basePath = $"audio\\wwise";
                if (soundBank.Language == Languages.Sfx)
                    soundBank.SoundBankFilePath = $"{basePath}\\{soundBank.SoundBankFileName}";
                else
                    soundBank.SoundBankFilePath = $"{basePath}\\{soundBank.Language}\\{soundBank.SoundBankFileName}";

                soundBank.Id = WwiseHash.Compute(soundBank.SoundBankFileName.Replace(".bnk", string.Empty));
            }
        }

        public void SetInitialSourceData(AudioProject audioProject)
        {
            var wwiseIdService = WwiseIdServiceFactory.GetWwiseIdService(_applicationSettingsService.CurrentSettings.CurrentGame);
            var sourceIdByWavFilePathLookup = new Dictionary<string, uint>();

            foreach (var soundBank in audioProject.SoundBanks)
            {
                if (soundBank.ActionEvents != null)
                {
                    foreach (var actionEvent in soundBank.ActionEvents)
                    {
                        if (actionEvent.Sound != null)
                            SetSoundData(
                                audioProject.FileName,
                                actionEvent.Sound,
                                soundBank,
                                sourceIdByWavFilePathLookup,
                                wwiseIdService);
                        else
                        {
                            SetRandomSequenceContainerData(
                                audioProject.FileName,
                                actionEvent.RandomSequenceContainer,
                                soundBank,
                                sourceIdByWavFilePathLookup,
                                wwiseIdService,
                                actionEventName: actionEvent.Name);
                        }

                    }
                }

                if (soundBank.DialogueEvents != null)
                {
                    foreach (var dialogueEvent in soundBank.DialogueEvents)
                    {
                        foreach (var statePath in dialogueEvent.StatePaths)
                        {
                            if (statePath.Sound != null)
                                SetSoundData(
                                    audioProject.FileName,
                                    statePath.Sound,
                                    soundBank,
                                    sourceIdByWavFilePathLookup,
                                    wwiseIdService);
                            else
                                SetRandomSequenceContainerData(
                                    audioProject.FileName,
                                    statePath.RandomSequenceContainer,
                                    soundBank,
                                    sourceIdByWavFilePathLookup,
                                    wwiseIdService,
                                    dialogueEventName: dialogueEvent.Name,
                                    statePath: statePath);
                        }
                    }
                }
            }
        }

        public void SetRemainingSourceData(AudioProject audioProject)
        {
            var soundsWithUniqueSourceIds = AudioProjectCompilerHelpers.GetAllUniqueSounds(audioProject);
            foreach (var sound in soundsWithUniqueSourceIds)
            {
                sound.WemFileName = $"{sound.SourceId}.wem";

                var basePath = $"audio\\wwise";
                if (string.IsNullOrEmpty(sound.Language))
                    sound.WemFilePath = $"{basePath}\\{sound.WemFileName}";
                else if (sound.Language == Languages.Sfx)
                    sound.WemFilePath = $"{basePath}\\{sound.WemFileName}";
                else
                    sound.WemFilePath = $"{basePath}\\{sound.Language}\\{sound.WemFileName}";

                sound.WemDiskFilePath = $"{DirectoryHelper.Temp}\\Audio\\{sound.WemFileName}";

                var wemFileInfo = new FileInfo(sound.WemDiskFilePath);
                var fileSizeInBytes = wemFileInfo.Length;
                sound.InMemoryMediaSize = fileSizeInBytes;
            }
        }

        private void SetSoundData(
            string audioProjectFileName,
            Sound sound,
            SoundBank soundBank,
            Dictionary<string, uint> sourceLookup,
            IWwiseIdService wwiseIdService)
        {
            sound.Language = soundBank.Language;

            var usedHircIds = UsedHircIdsByLanguageIdLookup[WwiseHash.Compute(soundBank.Language)];
            var soundFileNameWithoutExtension = Path.GetFileNameWithoutExtension(sound.WavFileName);
            var soundIdResult = IdGenerator.GenerateSoundHircId(usedHircIds, audioProjectFileName, soundFileNameWithoutExtension);
            sound.Id = soundIdResult.Id;

            if (wwiseIdService.OverrideBusIds.TryGetValue(soundBank.SoundBankSubtype, out var overrideBusId))
                sound.OverrideBusId = overrideBusId;
            else
                sound.OverrideBusId = 0;

            if (wwiseIdService.ActorMixerIds.TryGetValue(soundBank.SoundBankSubtype, out var actorMixerId))
                sound.DirectParentId = actorMixerId;

            if (!sourceLookup.TryGetValue(sound.WavFilePath, out var sourceId))
            {
                var usedSourceIds = UsedSourceIdsByLanguageIdLookup[WwiseHash.Compute(soundBank.Language)];
                var sourceIdResult = IdGenerator.GenerateWemId(usedSourceIds, audioProjectFileName, soundFileNameWithoutExtension);
                sourceId = sourceIdResult.Id;
                sourceLookup[sound.WavFilePath] = sourceId;
            }
            sound.SourceId = sourceId;
        }

        private void SetRandomSequenceContainerData(
            string audioProjectFileName,
            RandomSequenceContainer container,
            SoundBank soundBank,
            Dictionary<string, uint> sourceLookup,
            IWwiseIdService wwiseIdService,
            string actionEventName = null,
            string dialogueEventName = null,
            StatePath statePath = null)
        {
            container.Language = soundBank.Language;

            var usedHircIds = UsedHircIdsByLanguageIdLookup[WwiseHash.Compute(soundBank.Language)];
            IdGenerator.Result containerIdResult;

            if (actionEventName != null)
            {
                containerIdResult = IdGenerator.GenerateRanSeqCntrActionEventHircId(usedHircIds, audioProjectFileName, actionEventName);
                container.Id = containerIdResult.Id;
            }
            else if (dialogueEventName != null)
            {
                // TODO are we using the right name here we check dialogueeventname but use actioneventname
                containerIdResult = IdGenerator.GenerateRanSeqCntrDialogueEventHircId(usedHircIds, audioProjectFileName, actionEventName, statePath);
                container.Id = containerIdResult.Id;
            }

            if (wwiseIdService.OverrideBusIds.TryGetValue(soundBank.SoundBankSubtype, out var overrideBusId))
                container.OverrideBusId = overrideBusId;
            else
                container.OverrideBusId = 0;

            if (wwiseIdService.ActorMixerIds.TryGetValue(soundBank.SoundBankSubtype, out var actorMixerId))
                container.DirectParentId = actorMixerId;

            foreach (var sound in container.Sounds)
            {
                SetSoundData(audioProjectFileName, sound, soundBank, sourceLookup, wwiseIdService);
                sound.DirectParentId = container.Id;
            }

            container.Sounds = container.Sounds.OrderBy(sound => sound.Id).ToList();
        }

        public void CreateStopActionEvents(AudioProject audioProject)
        {
            foreach (var soundBank in audioProject.SoundBanks)
            {
                if (soundBank.SoundBankSubtype == Wh3SoundBankSubtype.FrontendMusic)
                {
                    for (var i = 0; i < soundBank.ActionEvents.Count; i++)
                    {
                        var actionEvent = soundBank.ActionEvents[i];

                        var stopEventName = string.Concat("Stop_", actionEvent.Name.AsSpan("Play_".Length));
                        var stopEvent = new ActionEvent
                        {
                            Name = stopEventName,
                            Id = WwiseHash.Compute(stopEventName),
                            HircType = actionEvent.HircType,
                            RandomSequenceContainer = actionEvent.RandomSequenceContainer,
                            Sound = actionEvent.Sound
                        };

                        soundBank.ActionEvents.Insert(i + 1, stopEvent);
                        i++;
                    }
                }
            }
        }

        public void SetActionData(AudioProject audioProject)
        {
            foreach (var soundBank in audioProject.SoundBanks)
            {
                foreach (var actionEvent in soundBank.ActionEvents)
                {
                    var action = new Action();

                    if (actionEvent.Name.StartsWith("Stop_"))
                        action.ActionType = AkActionType.Stop_E_O;

                    var usedHircIds = UsedHircIdsByLanguageIdLookup[WwiseHash.Compute(soundBank.Language)];

                    if (actionEvent.Sound != null)
                    {
                        var actionIdResult = IdGenerator.GenerateActionHircId(usedHircIds, audioProject.FileName, actionEvent.Name);
                        action.Id = actionIdResult.Id;
                        action.IdExt = actionEvent.Sound.Id;
                    } 
                    else
                    {
                        var actionIdResult = IdGenerator.GenerateActionHircId(usedHircIds, audioProject.FileName, actionEvent.Name);
                        action.Id = actionIdResult.Id; 
                        action.IdExt = actionEvent.RandomSequenceContainer.Id;
                    }

                    actionEvent.Actions = [action];

                    if (actionEvent.Actions.Count > 1)
                        throw new NotSupportedException("We're not set up to handle multiple actions.");

                    actionEvent.Actions = actionEvent.Actions
                        .OrderBy(action => action.Id)
                        .ToList();
                }
            }
        }

        public void SetActionEventData(AudioProject audioProject)
        {
            foreach (var soundBank in audioProject.SoundBanks)
            {
                foreach (var actionEvent in soundBank.ActionEvents)
                {
                    actionEvent.Id = WwiseHash.Compute(actionEvent.Name);

                    if (UsedHircIdsByLanguageIdLookup[WwiseHash.Compute(soundBank.Language)].Contains(actionEvent.Id))
                        throw new NotSupportedException($"Action Event Id {actionEvent.Id} for {actionEvent.Name} in {soundBank.Language} is already in use, the Event needs a different name.");
                }

                soundBank.ActionEvents = soundBank.ActionEvents
                    .OrderBy(actionEvent => actionEvent.Id)
                    .ToList();
            }
        }

        public void SetStatesData(AudioProject audioProject)
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

        public void SetDialogueEventData(AudioProject audioProject)
        {
            foreach (var soundBank in audioProject.SoundBanks)
            {
                foreach (var dialogueEvent in soundBank.DialogueEvents)
                {
                    foreach (var statePath in dialogueEvent.StatePaths)
                        dialogueEvent.Id = WwiseHash.Compute(dialogueEvent.Name);
                }

                soundBank.DialogueEvents = soundBank.DialogueEvents
                    .OrderBy(dialogueEvent => dialogueEvent.Id)
                    .ToList();
            }
        }
    }
}
