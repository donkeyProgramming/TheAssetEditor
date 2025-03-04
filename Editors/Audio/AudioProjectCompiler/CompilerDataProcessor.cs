using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Editors.Audio.AudioEditor.Data;
using Editors.Audio.AudioProjectCompiler.WwiseIDService;
using Editors.Audio.GameSettings.Warhammer3;
using Editors.Audio.Storage;
using Editors.Audio.Utility;
using Shared.Core.Misc;
using Shared.Core.Settings;
using static Editors.Audio.GameSettings.Warhammer3.SoundBanks;
using Action = Editors.Audio.AudioEditor.Data.Action;

namespace Editors.Audio.AudioProjectCompiler
{
    public class CompilerDataProcessor
    {
        private readonly ApplicationSettingsService _applicationSettingsService;
        private readonly IAudioRepository _audioRepository;

        private Dictionary<uint, List<uint>> UsedHircIdsByLanguageIDLookup { get; set; } = [];
        private Dictionary<uint, List<uint>> UsedSourceIdsByLanguageIDLookup { get; set; } = [];

        public CompilerDataProcessor(
            ApplicationSettingsService applicationSettingsService,
            IAudioRepository audioRepository)
        {
            _applicationSettingsService = applicationSettingsService;
            _audioRepository = audioRepository;

            UsedHircIdsByLanguageIDLookup = _audioRepository.HircLookupByLanguageIDByID
                .ToDictionary(
                    outer => outer.Key,
                    outer => outer.Value.Keys.ToList()
                );

            UsedSourceIdsByLanguageIDLookup = _audioRepository.SoundHircLookupByLanguageIDBySourceID
                .ToDictionary(
                    outer => outer.Key,
                    outer => outer.Value.Keys.ToList()
                );
        }

        public void SetSoundBankData(AudioProject audioProject, string audioProjectFileName)
        {
            foreach (var soundBank in audioProject.SoundBanks)
            {
                soundBank.SoundBankSubtype = GetSoundBankSubtype(soundBank.Name);
                soundBank.Language = AudioProjectCompilerHelpers.GetCorrectSoundBankLanguage(audioProject);

                soundBank.SoundBankFileName = $"{GetSoundBankName(soundBank.SoundBankSubtype)}_{audioProjectFileName}.bnk";

                var basePath = $"audio\\wwise";
                if (soundBank.Language == Languages.Sfx)
                    soundBank.SoundBankFilePath = $"{basePath}\\{soundBank.SoundBankFileName}";
                else
                    soundBank.SoundBankFilePath = $"{basePath}\\{soundBank.Language}\\{soundBank.SoundBankFileName}";

                soundBank.ID = WwiseHash.Compute(soundBank.SoundBankFileName.Replace(".bnk", string.Empty));
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
                            SetSoundData(actionEvent.Sound, soundBank.Language, soundBank.SoundBankSubtype, sourceIDByWavFilePathLookup, wwiseIDService);
                        else
                            SetRandomSequenceContainerData(actionEvent.RandomSequenceContainer, soundBank.Language, soundBank.SoundBankSubtype, sourceIDByWavFilePathLookup, wwiseIDService);
                    }
                }

                if (soundBank.DialogueEvents != null)
                {
                    foreach (var dialogueEvent in soundBank.DialogueEvents)
                    {
                        foreach (var statePath in dialogueEvent.StatePaths)
                        {
                            if (statePath.Sound != null)
                                SetSoundData(statePath.Sound, soundBank.Language, soundBank.SoundBankSubtype, sourceIDByWavFilePathLookup, wwiseIDService);
                            else
                                SetRandomSequenceContainerData(statePath.RandomSequenceContainer, soundBank.Language, soundBank.SoundBankSubtype, sourceIDByWavFilePathLookup, wwiseIDService);
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

        private void SetSoundData(Sound sound, string language, Wh3SoundBankSubtype soundBankSubtype, Dictionary<string, uint> sourceLookup, IWwiseIDService wwiseIDService)
        {
            sound.Language = language;
            sound.ID = AudioProjectCompilerHelpers.GenerateUnusedHircID(UsedHircIdsByLanguageIDLookup, language);

            if (wwiseIDService.ActorMixerIds.TryGetValue(soundBankSubtype, out var actorMixerID))
                sound.DirectParentID = actorMixerID;

            if (!sourceLookup.TryGetValue(sound.WavFilePath, out var sourceId))
            {
                sourceId = AudioProjectCompilerHelpers.GenerateUnusedSourceID(UsedSourceIdsByLanguageIDLookup, language);
                sourceLookup[sound.WavFilePath] = sourceId;
            }
            sound.SourceID = sourceId;
        }

        private void SetRandomSequenceContainerData(RandomSequenceContainer container, string language, Wh3SoundBankSubtype soundBankSubtype, Dictionary<string, uint> sourceLookup, IWwiseIDService wwiseIDService)
        {
            container.Language = language;
            container.ID = AudioProjectCompilerHelpers.GenerateUnusedHircID(UsedHircIdsByLanguageIDLookup, language);

            if (wwiseIDService.ActorMixerIds.TryGetValue(soundBankSubtype, out var actorMixerID))
                container.DirectParentID = actorMixerID;

            foreach (var sound in container.Sounds)
            {
                SetSoundData(sound, language, soundBankSubtype, sourceLookup, wwiseIDService);
                sound.DirectParentID = container.ID;
            }

            container.Sounds = container.Sounds.OrderBy(sound => sound.ID).ToList();
        }


        public void SetActionData(AudioProject audioProject)
        {
            foreach (var soundBank in audioProject.SoundBanks)
            {
                foreach (var actionEvent in soundBank.ActionEvents)
                {
                    if (actionEvent.Sound != null)
                    {
                        var action = new Action
                        {
                            ID = AudioProjectCompilerHelpers.GenerateUnusedHircID(UsedHircIdsByLanguageIDLookup, actionEvent.Sound.Language),
                            IDExt = actionEvent.Sound.ID
                        };

                        actionEvent.Actions = [action];
                    }
                    else
                    {
                        var action = new Action
                        {
                            ID = AudioProjectCompilerHelpers.GenerateUnusedHircID(UsedHircIdsByLanguageIDLookup, actionEvent.RandomSequenceContainer.Language),
                            IDExt = actionEvent.RandomSequenceContainer.ID
                        };

                        actionEvent.Actions = [action];
                    }

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
                    actionEvent.ID = WwiseHash.Compute(actionEvent.Name);

                    if (UsedHircIdsByLanguageIDLookup[WwiseHash.Compute(soundBank.Language)].Contains(actionEvent.ID))
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
                stateGroup.ID = WwiseHash.Compute(stateGroup.Name);

                foreach (var state in stateGroup.States)
                {
                    if (state.Name == "Any")
                        state.ID = 0;
                    else
                        state.ID = WwiseHash.Compute(state.Name);
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
                        dialogueEvent.ID = WwiseHash.Compute(dialogueEvent.Name);
                }

                soundBank.DialogueEvents = soundBank.DialogueEvents
                    .OrderBy(dialogueEvent => dialogueEvent.ID)
                    .ToList();
            }
        }
    }
}
