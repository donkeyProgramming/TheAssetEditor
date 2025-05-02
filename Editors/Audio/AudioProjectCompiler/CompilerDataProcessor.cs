using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Editors.Audio.AudioEditor.AudioProjectData;
using Editors.Audio.AudioProjectCompiler.WwiseIDService;
using Editors.Audio.GameSettings.Warhammer3;
using Editors.Audio.Storage;
using Editors.Audio.Utility;
using Shared.Core.Misc;
using Shared.Core.Settings;
using Shared.GameFormats.Wwise.Enums;
using static Editors.Audio.GameSettings.Warhammer3.SoundBanks;
using Action = Editors.Audio.AudioEditor.AudioProjectData.Action;

namespace Editors.Audio.AudioProjectCompiler
{
    public class CompilerDataProcessor
    {
        private readonly ApplicationSettingsService _applicationSettingsService;
        private readonly IAudioRepository _audioRepository;

        private Dictionary<uint, HashSet<uint>> UsedHircIdsByLanguageIDLookup { get; set; } = [];
        private Dictionary<uint, HashSet<uint>> UsedSourceIdsByLanguageIDLookup { get; set; } = [];

        public CompilerDataProcessor(
            ApplicationSettingsService applicationSettingsService,
            IAudioRepository audioRepository)
        {
            _applicationSettingsService = applicationSettingsService;
            _audioRepository = audioRepository;

            UsedHircIdsByLanguageIDLookup = _audioRepository.HircLookupByLanguageIDByID
                .ToDictionary(
                    outer => outer.Key,
                    outer => outer.Value.Keys.ToHashSet()
                );

            UsedSourceIdsByLanguageIDLookup = _audioRepository.SoundHircLookupByLanguageIDBySourceID
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
                soundBank.Language = AudioProjectCompilerHelpers.GetCorrectSoundBankLanguage(audioProject); // TODO: Music should be SFX.

                soundBank.SoundBankFileName = $"{GetSoundBankName(soundBank.SoundBankSubtype)}_{audioProject.FileName}.bnk";

                var basePath = $"audio\\wwise";
                if (soundBank.Language == Languages.Sfx)
                    soundBank.SoundBankFilePath = $"{basePath}\\{soundBank.SoundBankFileName}";
                else
                    soundBank.SoundBankFilePath = $"{basePath}\\{soundBank.Language}\\{soundBank.SoundBankFileName}";

                soundBank.ID = WwiseHashRename.Compute(soundBank.SoundBankFileName.Replace(".bnk", string.Empty));
            }
        }

        public void SetInitialSourceData(AudioProject audioProject)
        {
            var wwiseIDService = WwiseIDServiceFactory.GetWwiseIDService(_applicationSettingsService.CurrentSettings.CurrentGame);
            var sourceIDByWavFilePathLookup = new Dictionary<string, uint>();

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
                                sourceIDByWavFilePathLookup,
                                wwiseIDService);
                        else
                        {
                            SetRandomSequenceContainerData(
                                audioProject.FileName,
                                actionEvent.RandomSequenceContainer,
                                soundBank,
                                sourceIDByWavFilePathLookup,
                                wwiseIDService,
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
                                    sourceIDByWavFilePathLookup,
                                    wwiseIDService);
                            else
                                SetRandomSequenceContainerData(
                                    audioProject.FileName,
                                    statePath.RandomSequenceContainer,
                                    soundBank,
                                    sourceIDByWavFilePathLookup,
                                    wwiseIDService,
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
                sound.WemFileName = $"{sound.SourceID}.wem";

                var basePath = $"audio\\wwise";
                if (string.IsNullOrEmpty(sound.Language))
                    sound.WemFilePath = $"{basePath}\\{sound.WemFileName}";
                else
                    sound.WemFilePath = $"{basePath}\\{audioProject.Language}\\{sound.WemFileName}";

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
            IWwiseIDService wwiseIDService)
        {
            sound.Language = soundBank.Language;

            var usedHircIds = UsedHircIdsByLanguageIDLookup[WwiseHash.Compute(soundBank.Language)];
            var soundFileNameWithoutExtension = Path.GetFileNameWithoutExtension(sound.WavFileName);
            var soundIDResult = IDGenerator.GenerateSoundHircID(usedHircIds, audioProjectFileName, soundFileNameWithoutExtension);
            sound.ID = soundIDResult.ID;

            if (wwiseIDService.OverrideBusIds.TryGetValue(soundBank.SoundBankSubtype, out var overrideBusID))
                sound.OverrideBusID = overrideBusID;
            else
                sound.OverrideBusID = 0;

            if (wwiseIDService.ActorMixerIds.TryGetValue(soundBank.SoundBankSubtype, out var actorMixerID))
                sound.DirectParentID = actorMixerID;

            if (!sourceLookup.TryGetValue(sound.WavFilePath, out var sourceID))
            {
                var usedSourceIds = UsedSourceIdsByLanguageIDLookup[WwiseHash.Compute(soundBank.Language)];
                var sourceIDResult = IDGenerator.GenerateWemID(usedSourceIds, audioProjectFileName, soundFileNameWithoutExtension);
                sourceID = sourceIDResult.ID;
                sourceLookup[sound.WavFilePath] = sourceID;
            }
            sound.SourceID = sourceID;
        }

        private void SetRandomSequenceContainerData(
            string audioProjectFileName,
            RandomSequenceContainer container,
            SoundBank soundBank,
            Dictionary<string, uint> sourceLookup,
            IWwiseIDService wwiseIDService,
            string actionEventName = null,
            string dialogueEventName = null,
            StatePath statePath = null)
        {
            container.Language = soundBank.Language;

            var usedHircIds = UsedHircIdsByLanguageIDLookup[WwiseHash.Compute(soundBank.Language)];
            IDGenerator.Result containerIDResult;

            if (actionEventName != null)
            {
                containerIDResult = IDGenerator.GenerateRanSeqCntrActionEventHircID(usedHircIds, audioProjectFileName, actionEventName);
                container.ID = containerIDResult.ID;
            }
            else if (dialogueEventName != null)
            {
                containerIDResult = IDGenerator.GenerateRanSeqCntrDialogueEventHircID(usedHircIds, audioProjectFileName, actionEventName, statePath);
                container.ID = containerIDResult.ID;
            }

            if (wwiseIDService.OverrideBusIds.TryGetValue(soundBank.SoundBankSubtype, out var overrideBusID))
                container.OverrideBusID = overrideBusID;
            else
                container.OverrideBusID = 0;

            if (wwiseIDService.ActorMixerIds.TryGetValue(soundBank.SoundBankSubtype, out var actorMixerID))
                container.DirectParentID = actorMixerID;

            foreach (var sound in container.Sounds)
            {
                SetSoundData(audioProjectFileName, sound, soundBank, sourceLookup, wwiseIDService);
                sound.DirectParentID = container.ID;
            }

            container.Sounds = container.Sounds.OrderBy(sound => sound.ID).ToList();
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
                            ID = WwiseHashRename.Compute(stopEventName),
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

                    var usedHircIds = UsedHircIdsByLanguageIDLookup[WwiseHash.Compute(soundBank.Language)];

                    if (actionEvent.Sound != null)
                    {
                        var actionIDResult = IDGenerator.GenerateActionHircID(usedHircIds, audioProject.FileName, actionEvent.Name);
                        action.ID = actionIDResult.ID;
                        action.IDExt = actionEvent.Sound.ID;
                    } 
                    else
                    {
                        var actionIDResult = IDGenerator.GenerateActionHircID(usedHircIds, audioProject.FileName, actionEvent.Name);
                        action.ID = actionIDResult.ID; 
                        action.IDExt = actionEvent.RandomSequenceContainer.ID;
                    }

                    actionEvent.Actions = [action];

                    if (actionEvent.Actions.Count > 1)
                        throw new NotSupportedException("We're not set up to handle multiple actions.");

                    actionEvent.Actions = actionEvent.Actions
                        .OrderBy(action => action.ID)
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
                    actionEvent.ID = WwiseHashRename.Compute(actionEvent.Name);

                    if (UsedHircIdsByLanguageIDLookup[WwiseHashRename.Compute(soundBank.Language)].Contains(actionEvent.ID))
                        throw new NotSupportedException($"Action Event ID {actionEvent.ID} for {actionEvent.Name} in {soundBank.Language} is already in use, the Event needs a different name.");
                }

                soundBank.ActionEvents = soundBank.ActionEvents
                    .OrderBy(actionEvent => actionEvent.ID)
                    .ToList();
            }
        }

        public void SetStatesData(AudioProject audioProject)
        {
            foreach (var stateGroup in audioProject.StateGroups)
            {
                stateGroup.ID = WwiseHashRename.Compute(stateGroup.Name);

                foreach (var state in stateGroup.States)
                {
                    if (state.Name == "Any")
                        state.ID = 0;
                    else
                        state.ID = WwiseHashRename.Compute(state.Name);
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
                        dialogueEvent.ID = WwiseHashRename.Compute(dialogueEvent.Name);
                }

                soundBank.DialogueEvents = soundBank.DialogueEvents
                    .OrderBy(dialogueEvent => dialogueEvent.ID)
                    .ToList();
            }
        }
    }
}
