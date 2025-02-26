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
using Shared.GameFormats.Dat;
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

            GenerateEventDatFiles(audioProject);
        }

        private void SetSoundBankData(AudioProjectDataModel audioProject)
        {
            foreach (var soundBank in audioProject.SoundBanks)
            {
                soundBank.ID = WwiseHash.Compute(soundBank.Name);
                soundBank.SoundBankSubtype = SoundBanks.GetSoundBankSubtype(soundBank.Name);
                soundBank.Language = AudioProjectCompilerHelpers.GetCorrectSoundBankLanguage(audioProject);

                var audioProjectFileName = _audioProjectService.AudioProjectFileName.Replace(" ", "_");
                soundBank.SoundBankFileName = $"{SoundBanks.GetSoundBankName(soundBank.SoundBankSubtype)}_{audioProjectFileName}.bnk";

                var basePath = $"audio\\wwise";
                if (soundBank.Language == Languages.Sfx)
                    soundBank.SoundBankFilePath = $"{basePath}\\{soundBank.SoundBankFileName}";
                else
                    soundBank.SoundBankFilePath = $"{basePath}\\{soundBank.Language}\\{soundBank.SoundBankFileName}";
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

                        if (wwiseIDService.ActorMixerIds.TryGetValue(soundBank.SoundBankSubtype, out var actorMixerID))
                            actionEvent.Sound.DirectParentID = actorMixerID;

                        if (wwiseIDService.AttenuationIds.TryGetValue(soundBank.SoundBankSubtype, out var attenuationID))
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

                        if (wwiseIDService.ActorMixerIds.TryGetValue(soundBank.SoundBankSubtype, out var actorMixerID))
                            actionEvent.RandomSequenceContainer.DirectParentID = actorMixerID;

                        foreach (var sound in actionEvent.RandomSequenceContainer.Sounds)
                        {
                            sound.Language = soundBank.Language;
                            sound.ID = AudioProjectCompilerHelpers.GenerateUnusedHircID(UsedHircIdsByLanguageIDLookup, sound.Language);
                            sound.DirectParentID = actionEvent.RandomSequenceContainer.ID;

                            if (wwiseIDService.AttenuationIds.TryGetValue(soundBank.SoundBankSubtype, out var attenuationID))
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
                {
                    actionEvent.ID = WwiseHash.Compute(actionEvent.Name);

                    if (UsedHircIdsByLanguageIDLookup[WwiseHash.Compute(soundBank.Language)].Contains(actionEvent.ID))
                        throw new NotSupportedException ($"Action Event ID {actionEvent.ID} for {actionEvent.Name} in {soundBank.Language} is already in use, the Event needs a different name.");
                }

                soundBank.ActionEvents = soundBank.ActionEvents
                    .OrderBy(actionEvent => actionEvent.ID)
                    .ToList();
            }
        }

        private void GenerateWems(AudioProjectDataModel audioProject)
        {
            DeleteAudioFilesInTempAudioFolder();

            var soundsWithUniqueSourceIds = audioProject.SoundBanks
                .SelectMany(soundBank => soundBank.ActionEvents)
                .SelectMany<ActionEvent, Sound>(actionEvent => actionEvent.Sound != null ? new[] { actionEvent.Sound } : actionEvent.RandomSequenceContainer.Sounds)
                .DistinctBy(sound => sound.SourceID)
                .ToList();

            foreach (var sound in soundsWithUniqueSourceIds)
                _soundPlayer.ExportWavFileWithWemID(sound);

            _wemGenerator.GenerateWems(soundsWithUniqueSourceIds);
        }

        private static void DeleteAudioFilesInTempAudioFolder()
        {
            var audioFolder = $"{DirectoryHelper.Temp}\\Audio";
            if (Directory.Exists(audioFolder))
            {
                foreach (var file in Directory.GetFiles(audioFolder, "*.wav"))
                    File.Delete(file);

                foreach (var file in Directory.GetFiles(audioFolder, "*.wem"))
                    File.Delete(file);
            }
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

                var headerBytes = BkhdParser.WriteData(bkhdChunk);
                var hircBytes = new HircParser().WriteData(hircChunk, bankGeneratorVersion);

                // Write
                using var memStream = new MemoryStream();
                memStream.Write(headerBytes);
                memStream.Write(hircBytes);
                var bytes = memStream.ToArray();

                // Convert to output and parse for sanity
                var bnkPackFile = new PackFile(soundBank.SoundBankFileName, new MemorySource(bytes));
                var parser = new BnkParser();
                var reparsedSanityFile = parser.Parse(bnkPackFile, "test\\fakefilename.bnk", true);

                _fileSaveService.Save(soundBank.SoundBankFilePath, bnkPackFile.DataSource.ReadData(), true);
            }
        }

        private static void GenerateActionEventSoundBanks(WwiseHircGeneratorServiceFactory wwiseHircGeneratorServiceFactory, SoundBank soundBank, List<HircItem> hircItems)
        {
            var sourceHircs = new Dictionary<HircItem, List<HircItem>>();

            foreach (var actionEvent in soundBank.ActionEvents)
            {
                if (actionEvent.Sound != null)
                {
                    var soundHirc = wwiseHircGeneratorServiceFactory.GenerateHirc(actionEvent.Sound, soundBank);
                    sourceHircs.Add(soundHirc, []);
                }
                else
                {
                    var soundHircs = actionEvent.RandomSequenceContainer.Sounds
                        .Select(sound => wwiseHircGeneratorServiceFactory.GenerateHirc(sound, soundBank))
                        .ToList();

                    var randomSequenceContainerHirc = wwiseHircGeneratorServiceFactory.GenerateHirc(actionEvent.RandomSequenceContainer, soundBank);
                    sourceHircs.Add(randomSequenceContainerHirc, soundHircs);
                }
            }

            foreach (var sourceHirc in sourceHircs.OrderBy(sourceHirc => sourceHirc.Key.ID))
            {
                foreach (var soundHirc in sourceHirc.Value)
                    hircItems.Add(soundHirc);

                hircItems.Add(sourceHirc.Key);
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

        private void GenerateEventDatFiles(AudioProjectDataModel audioProject)
        {
            foreach (var soundBank in audioProject.SoundBanks)
            {
                // TODO: Need to impelement something so it only makes dat files for the soundbanks that need it, so far I think that's at least movies.
                var datFile = new SoundDatFile();

                foreach (var actionEvent in soundBank.ActionEvents)
                    datFile.EventWithStateGroup.Add(new SoundDatFile.DatEventWithStateGroup() { EventName = actionEvent.Name, Value = 400 });

                var datFileName = $"event_data__{soundBank.SoundBankFileName.Replace(".bnk", ".dat")}";
                var datFilePath = $"audio\\wwise\\{datFileName}";

                var bytes = DatFileParser.WriteData(datFile);
                var packFile = new PackFile(datFileName, new MemorySource(bytes));
                var reparsedSanityFile = DatFileParser.Parse(packFile, false);

                _fileSaveService.Save(datFilePath, packFile.DataSource.ReadData(), true);
            }
        }
    }
}
