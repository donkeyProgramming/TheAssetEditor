using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Editors.Audio.AudioEditor.AudioProjectCompiler.WwiseGeneratorService;
using Editors.Audio.AudioEditor.AudioProjectCompiler.WwiseGeneratorService.WwiseGenerators.Bkhd;
using Editors.Audio.AudioEditor.AudioProjectCompiler.WwiseIDService;
using Editors.Audio.AudioEditor.AudioProjectData;
using Editors.Audio.AudioEditor.AudioProjectData.AudioProjectService;
using Editors.Audio.GameSettings.Warhammer3;
using Editors.Audio.Storage;
using Editors.Audio.Utility;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Settings;
using Shared.GameFormats.Wwise;
using Shared.GameFormats.Wwise.Bkhd;
using Shared.GameFormats.Wwise.Hirc;
using Action = Editors.Audio.AudioEditor.AudioProjectData.Action;

namespace Editors.Audio.AudioEditor.AudioProjectCompiler
{
    public class AudioProjectCompiler
    {
        private readonly IPackFileService _packFileService;
        private readonly IAudioRepository _audioRepository;
        private readonly IAudioProjectService _audioProjectService;
        private readonly ApplicationSettingsService _applicationSettingsService;
        private readonly IFileSaveService _fileSaveService;
        private readonly SoundPlayer _soundPlayer;
        private readonly WemGenerator _wemGenerator;

        private Dictionary<uint, List<uint>> UsedHircIdsByLanguageIDLookup { get; set; } = [];
        private Dictionary<uint, List<uint>> UsedSourceIdsByLanguageIDLookup { get; set; } = [];

        public AudioProjectCompiler(
            IPackFileService packFileService,
            IAudioRepository audioRepository,
            IAudioProjectService audioProjectService,
            ApplicationSettingsService applicationSettingsService,
            IFileSaveService fileSaveService,
            SoundPlayer soundPlayer,
            WemGenerator wavToWemConverter)
        {
            _packFileService = packFileService;
            _audioRepository = audioRepository;
            _audioProjectService = audioProjectService;
            _applicationSettingsService = applicationSettingsService;
            _fileSaveService = fileSaveService;
            _soundPlayer = soundPlayer;
            _wemGenerator = wavToWemConverter;

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

        // TODO: should proabably just make it generate for all languages
        public void CompileAudioProject(AudioProjectDataModel audioProject)
        {
            _audioProjectService.SaveAudioProject(_packFileService);

            SetSoundBankData(audioProject);

            // We set the data from the bottom up, so Sounds, then Actions, then Events to ensure that IDs are generated before they're referenced.
            // For example IDs set in Sounds / Sound Containers are used in Actions, and IDs set in Actions are used in Events.
            SetInitialSourceData(audioProject);
            SetActionData(audioProject);
            SetActionEventData(audioProject);

            GenerateWems(audioProject);

            SetRemainingSourceData(audioProject);

            AddWemsToPack(audioProject);

            GenerateSoundBanks(audioProject);
        }

        private void SetSoundBankData(AudioProjectDataModel audioProject)
        {
            foreach (var soundBank in audioProject.SoundBanks)
            {
                soundBank.ID = WwiseHash.Compute(soundBank.Name);
                soundBank.SoundBankSubType = SoundBanks.GetSoundBankEnum(soundBank.Name);
                soundBank.Language = AudioProjectCompilerHelpers.GetCorrectSoundBankLanguage(audioProject);
            }
        }

        private void SetInitialSourceData(AudioProjectDataModel audioProject)
        {
            var wwiseIDService = WwiseIDServiceFactory.GetWwiseIDService(_applicationSettingsService.CurrentSettings.CurrentGame);
            var sourceIDByWavFilePathLookup = new Dictionary<string, uint>(); // TODO: This should probably somehow also account for language given sounds are contained within language folder

            foreach (var soundBank in audioProject.SoundBanks)
            {
                foreach (var actionEvent in soundBank.ActionEvents)
                {
                    if (actionEvent.Sound != null)
                    {
                        actionEvent.Sound.Language = soundBank.Language;
                        actionEvent.Sound.ID = AudioProjectCompilerHelpers.GenerateUnusedHircID(UsedHircIdsByLanguageIDLookup, actionEvent.Sound.Language);

                        if (wwiseIDService.ActorMixerIds.TryGetValue(soundBank.SoundBankSubType, out var actorMixerID))
                            actionEvent.Sound.DirectParentID = actorMixerID;

                        if (wwiseIDService.AttenuationIds.TryGetValue(soundBank.SoundBankSubType, out var attenuationID))
                            actionEvent.Sound.AttenuationID = attenuationID;

                        if (sourceIDByWavFilePathLookup.TryGetValue(actionEvent.Sound.WavFilePath, out var sourceId))
                            actionEvent.Sound.SourceID = sourceId;
                        else
                        {
                            sourceId = AudioProjectCompilerHelpers.GenerateUnusedSourceID(UsedSourceIdsByLanguageIDLookup, actionEvent.Sound.Language);
                            sourceIDByWavFilePathLookup[actionEvent.Sound.WavFilePath] = sourceId;
                            actionEvent.Sound.SourceID = sourceId;
                        }
                    }
                    else
                    {
                        actionEvent.RandomSequenceContainer.Language = soundBank.Language;
                        actionEvent.RandomSequenceContainer.ID = AudioProjectCompilerHelpers.GenerateUnusedHircID(UsedHircIdsByLanguageIDLookup, actionEvent.RandomSequenceContainer.Language);

                        if (wwiseIDService.ActorMixerIds.TryGetValue(soundBank.SoundBankSubType, out var actorMixerID))
                            actionEvent.RandomSequenceContainer.DirectParentID = actorMixerID;

                        foreach (var sound in actionEvent.RandomSequenceContainer.Sounds)
                        {
                            sound.Language = soundBank.Language;
                            sound.ID = AudioProjectCompilerHelpers.GenerateUnusedHircID(UsedHircIdsByLanguageIDLookup, sound.Language);
                            sound.DirectParentID = actionEvent.RandomSequenceContainer.ID;

                            if (wwiseIDService.AttenuationIds.TryGetValue(soundBank.SoundBankSubType, out var attenuationID))
                                sound.AttenuationID = attenuationID;
                            
                            sound.Language = soundBank.Language;

                            if (sourceIDByWavFilePathLookup.TryGetValue(sound.WavFilePath, out var sourceId))
                                sound.SourceID = sourceId;
                            else
                            {
                                sourceId = AudioProjectCompilerHelpers.GenerateUnusedSourceID(UsedSourceIdsByLanguageIDLookup, sound.Language);
                                sourceIDByWavFilePathLookup[sound.WavFilePath] = sourceId;
                                sound.SourceID = sourceId;
                            }
                        }

                        actionEvent.RandomSequenceContainer.Sounds = actionEvent.RandomSequenceContainer.Sounds
                            .OrderBy(sound => sound.ID)
                            .ToList();
                    }
                }
            }
        }

        private void SetActionData(AudioProjectDataModel audioProject)
        {
            foreach (var soundBank in audioProject.SoundBanks)
            {
                var actionEvents = soundBank.ActionEvents;
                foreach (var actionEvent in actionEvents)
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

        private void SetActionEventData(AudioProjectDataModel audioProject)
        {
            foreach (var soundBank in audioProject.SoundBanks)
            {
                var actionEvents = soundBank.ActionEvents;
                foreach (var actionEvent in actionEvents)
                    actionEvent.ID = AudioProjectCompilerHelpers.GenerateUnusedHircID(UsedHircIdsByLanguageIDLookup, soundBank.Language);

                soundBank.ActionEvents = soundBank.ActionEvents
                    .OrderBy(actionEvent => actionEvent.ID)
                    .ToList();
            }
        }

        private void GenerateWems(AudioProjectDataModel audioProject)
        {
            var soundsWithUniqueSourceIds = audioProject.SoundBanks
                .SelectMany(soundBank => soundBank.ActionEvents)
                .SelectMany<ActionEvent, Sound>(actionEvent => actionEvent.Sound != null ? new[] { actionEvent.Sound } : actionEvent.RandomSequenceContainer.Sounds)
                .DistinctBy(sound => sound.SourceID)
                .ToList();

            foreach (var sound in soundsWithUniqueSourceIds)
                _soundPlayer.ExportWavFileWithWemID(sound);

            _wemGenerator.GenerateWems(soundsWithUniqueSourceIds);
        }

        private static void SetRemainingSourceData(AudioProjectDataModel audioProject)
        {
            var soundsWithUniqueSourceIds = audioProject.SoundBanks
                .SelectMany(soundBank => soundBank.ActionEvents)
                .SelectMany<ActionEvent, Sound>(actionEvent => actionEvent.Sound != null ? new[] { actionEvent.Sound } : actionEvent.RandomSequenceContainer.Sounds)
                .DistinctBy(sound => sound.SourceID)
                .ToList();

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

        private void AddWemsToPack(AudioProjectDataModel audioProject)
        {
            var soundsWithUniqueSourceIds = audioProject.SoundBanks
                .SelectMany(soundBank => soundBank.ActionEvents)
                .SelectMany<ActionEvent, Sound>(actionEvent => actionEvent.Sound != null ? new[] { actionEvent.Sound } : actionEvent.RandomSequenceContainer.Sounds)
                .DistinctBy(sound => sound.SourceID)
                .ToList();

            foreach (var sound in soundsWithUniqueSourceIds)
                PackFileUtil.LoadFileFromDisk(_packFileService, new PackFileUtil.FileRef(sound.WemDiskFilePath, Path.GetDirectoryName(sound.WemFilePath)));
        }

        private void GenerateSoundBanks(AudioProjectDataModel audioProject)
        {
            var bankGeneratorVersion = (uint)GameInformationDatabase.GetGameById(_applicationSettingsService.CurrentSettings.CurrentGame).BankGeneratorVersion;
            var wwiseHircGeneratorServiceFactory = WwiseHircGeneratorServiceFactory.CreateFactory(bankGeneratorVersion);

            foreach (var soundBank in audioProject.SoundBanks)
            {
                var bkhdChunk = BkhdChunkGenerator.GenerateBkhdChunk(audioProject, bankGeneratorVersion, soundBank);

                var hircItems = new List<HircItem>();

                if (soundBank.ActionEvents != null)
                    GenerateActionEventSoundBanks(wwiseHircGeneratorServiceFactory, soundBank, hircItems);

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

        private static void GenerateActionEventSoundBanks(WwiseHircGeneratorServiceFactory wwiseHircGeneratorServiceFactory, SoundBank soundBank, List<HircItem> hircItems)
        {
            foreach (var actionEvent in soundBank.ActionEvents)
            {
                if (actionEvent.Sound != null)
                {
                    var soundHirc = wwiseHircGeneratorServiceFactory.GenerateHirc(actionEvent.Sound, soundBank);
                    hircItems.Add(soundHirc);
                }
                else
                {
                    foreach (var sound in actionEvent.RandomSequenceContainer.Sounds)
                    {
                        var soundHirc = wwiseHircGeneratorServiceFactory.GenerateHirc(sound, soundBank);
                        hircItems.Add(soundHirc);
                    }

                    var randomSequenceContainerHirc = wwiseHircGeneratorServiceFactory.GenerateHirc(actionEvent.RandomSequenceContainer, soundBank);
                    hircItems.Add(randomSequenceContainerHirc);
                }
            }

            foreach (var actionEvent in soundBank.ActionEvents)
            {
                foreach (var action in actionEvent.Actions)
                {
                    var actionHirc = wwiseHircGeneratorServiceFactory.GenerateHirc(action, soundBank);
                    hircItems.Add(actionHirc);
                }

                var actionEventHirc = wwiseHircGeneratorServiceFactory.GenerateHirc(actionEvent, soundBank);
                hircItems.Add(actionEventHirc);
            }
        }
    }
}
