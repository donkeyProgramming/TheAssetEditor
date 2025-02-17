using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Editors.Audio.AudioEditor.AudioProjectCompiler.Wwise.Bkhd;
using Editors.Audio.AudioEditor.AudioProjectCompiler.WwiseGeneratorService;
using Editors.Audio.AudioEditor.AudioProjectCompiler.WwiseIDService;
using Editors.Audio.AudioEditor.Data;
using Editors.Audio.AudioEditor.Data.AudioProjectService;
using Editors.Audio.GameSettings.Warhammer3;
using Editors.Audio.Storage;
using Editors.Audio.Utility;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Settings;
using Shared.GameFormats.Wwise;
using Shared.GameFormats.Wwise.Bkhd;
using Shared.GameFormats.Wwise.Hirc;

namespace Editors.Audio.AudioEditor.AudioProjectCompiler
{
    public class SoundBankGenerator
    {
        private readonly IPackFileService _packFileService;
        private readonly IAudioRepository _audioRepository;
        private readonly IAudioProjectService _audioProjectService;
        private readonly ApplicationSettingsService _applicationSettingsService;
        private readonly IFileSaveService _fileSaveService;

        private List<uint> UsedHircIds { get; set; } = [];
        private List<uint> UsedWemIds { get; set; } = [];

        public SoundBankGenerator(IPackFileService packFileService, IAudioRepository audioRepository, IAudioProjectService audioProjectService, ApplicationSettingsService applicationSettingsService, IFileSaveService fileSaveService)
        {
            _packFileService = packFileService;
            _audioRepository = audioRepository;
            _audioProjectService = audioProjectService;
            _applicationSettingsService = applicationSettingsService;
            _fileSaveService = fileSaveService;

            UsedHircIds = _audioRepository.HircLookupByLanguageByID
                .SelectMany(language => language.Value.Keys)
                .Distinct()
                .OrderBy(id => id)
                .ToList();

            UsedWemIds = _audioRepository.SoundHircLookupByLanguageBySourceID
                .SelectMany(language => language.Value.Keys)
                .Distinct()
                .OrderBy(id => id)
                .ToList();
        }

        public void CompileSoundBanksFromAudioProject(AudioProjectDataModel audioProject)
        {
            _audioProjectService.SaveAudioProject(_packFileService);

            var wwiseIDService = WwiseIDServiceFactory.GetWwiseIDService(_applicationSettingsService.CurrentSettings.CurrentGame);

            var soundBanks = audioProject.SoundBanks;
            foreach (var soundBank in soundBanks)
            {
                SetSoundBankData(soundBank);
                SetActionEventData(soundBank);
                SetSoundOrSoundContainerData(wwiseIDService, soundBank);
            }

            var bankGeneratorVersion = (uint)GameInformationDatabase.GetGameById(_applicationSettingsService.CurrentSettings.CurrentGame).BankGeneratorVersion;
            var wwiseHircGeneratorFactory = WwiseHircGeneratorFactory.CreateFactory(bankGeneratorVersion);

            GenerateSoundBanks(audioProject, soundBanks, bankGeneratorVersion, wwiseHircGeneratorFactory);
        }

        private void GenerateSoundBanks(AudioProjectDataModel audioProject, List<SoundBank> soundBanks, uint bankGeneratorVersion, WwiseHircGeneratorFactory wwiseHircGeneratorFactory)
        {
            foreach (var soundBank in soundBanks)
            {
                var bkhdChunk = BkhdChunkGenerator.GenerateBkhdChunk(audioProject, bankGeneratorVersion, soundBank);

                var hircItems = new List<HircItem>();

                if (soundBank.ActionEvents != null)
                    GenerateActionEventSoundBanks(wwiseHircGeneratorFactory, soundBank, hircItems);

                var hircChunk = new HircChunk();
                hircChunk.WriteData(hircItems);

                var outputName = $"{soundBank.Name}.bnk";
                var headerBytes = BkhdParser.WriteData(bkhdChunk);
                var hircBytes = new HircParser().WriteData(hircChunk, bankGeneratorVersion);

                // Write
                using var memStream = new MemoryStream();
                memStream.Write(headerBytes);
                memStream.Write(hircBytes);
                var bytes = memStream.ToArray();

                // Convert to output and parse for sanity
                var bnkPackFile = new PackFile(outputName, new MemorySource(bytes));
                var parser = new BnkParser();
                var reparsedSanityFile = parser.Parse(bnkPackFile, "test\\fakefilename.bnk", true);

                var language = audioProject.Language;
                var bnkOutputPath = string.IsNullOrWhiteSpace(language) ? $"audio\\wwise\\{outputName}" : $"audio\\wwise\\{language}\\{outputName}";
                _fileSaveService.Save(bnkOutputPath, bnkPackFile.DataSource.ReadData(), true);
            }
        }

        private static void GenerateActionEventSoundBanks(WwiseHircGeneratorFactory wwiseHircGeneratorFactory, SoundBank soundBank, List<HircItem> hircItems)
        {
            foreach (var actionEvent in soundBank.ActionEvents)
            {
                var actionEventHirc = wwiseHircGeneratorFactory.GenerateHirc(actionEvent, soundBank);
                hircItems.Add(actionEventHirc);

                foreach (var action in actionEvent.Actions)
                {
                    var actionHirc = wwiseHircGeneratorFactory.GenerateHirc(action, soundBank);
                    hircItems.Add(actionHirc);
                }
            }
        }

        private static void SetSoundBankData(SoundBank soundBank)
        {
            soundBank.ID = WwiseHash.Compute(soundBank.Name);
            soundBank.SoundBankSubType = SoundBanks.GetSoundBankEnum(soundBank.Name);
        }

        private void SetActionEventData(SoundBank soundBank)
        {
            var actionEvents = soundBank.ActionEvents;
            foreach (var actionEvent in actionEvents)
            {
                actionEvent.ID = GenerateUnusedHircID();
                SetActionData(soundBank);
            }

            // Sort the action events in ascending order so they're processed correctly later on.
            soundBank.ActionEvents = soundBank.ActionEvents
                .OrderBy(actionEvent => actionEvent.ID)
                .ToList();
        }

        private void SetActionData(SoundBank soundBank)
        {
            var actionEvents = soundBank.ActionEvents;
            foreach (var actionEvent in actionEvents)
            {
                var action = new Data.Action
                {
                    ID = GenerateUnusedHircID()
                };

                actionEvent.Actions = [action];
                actionEvent.Actions = actionEvent.Actions
                    .OrderBy(action => action.ID)
                    .ToList();
            }
        }

        private void SetSoundOrSoundContainerData(IWwiseIDService wwiseIDService, SoundBank soundBank)
        {
            var actorMixerIds = wwiseIDService.ActorMixerIds;

            //var soundContainers = new List<SoundContainer>();

            var actionEvents = soundBank.ActionEvents;
            foreach (var actionEvent in actionEvents)
            {
                if (actionEvent.Sound != null)
                {
                    actionEvent.Sound.ID = GenerateUnusedHircID();
                    actionEvent.Sound.SourceID = GenerateUnusedWemID();
                }
                else
                {
                    //soundContainers.Add(actionEvent.SoundContainer);

                    actionEvent.SoundContainer.ID = GenerateUnusedHircID();
                    actionEvent.SoundContainer.DirectParentID = actorMixerIds[soundBank.SoundBankSubType];

                    foreach (var sound in actionEvent.SoundContainer.Sounds)
                    {
                        sound.ID = GenerateUnusedHircID();
                        sound.SourceID = GenerateUnusedWemID();
                    }

                    actionEvent.SoundContainer.Sounds = actionEvent.SoundContainer.Sounds
                        .OrderBy(sound => sound.ID)
                        .ToList();
                }
            }

            //soundContainers = soundContainers
            //.OrderBy(soundContainer => soundContainer.ID)
            //.ToList();
        }

        private uint GenerateUnusedHircID()
        {
            var unusedHircID = GenerateUnusedID(UsedHircIds);
            var index = UsedHircIds.BinarySearch(unusedHircID);
            if (index < 0)
                index = ~index;
            UsedHircIds.Insert(index, unusedHircID);
            return unusedHircID;
        }

        private uint GenerateUnusedWemID()
        {
            var unusedWemID = GenerateUnusedID(UsedWemIds);
            var index = UsedWemIds.BinarySearch(unusedWemID);
            if (index < 0)
                index = ~index;
            UsedWemIds.Insert(index, unusedWemID);
            return unusedWemID;
        }


        public static uint GenerateUnusedID(List<uint> usedIds)
        {
            uint minID = 1;
            uint maxID = 99999999;

            var usedIdSet = new HashSet<uint>(usedIds);

            for (var candidateID = minID; candidateID <= maxID; candidateID++)
            {
                if (!usedIdSet.Contains(candidateID))
                    return candidateID;
            }

            throw new InvalidOperationException("Houston we have a problem - no unused IDs available.");
        }
    }
}
