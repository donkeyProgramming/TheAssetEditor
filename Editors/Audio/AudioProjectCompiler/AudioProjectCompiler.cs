using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Editors.Audio.AudioEditor.AudioProjectData;
using Editors.Audio.AudioEditor.AudioProjectData.AudioProjectService;
using Editors.Audio.AudioProjectCompiler.WwiseGeneratorService;
using Editors.Audio.AudioProjectCompiler.WwiseGeneratorService.WwiseGenerators.Bkhd;
using Editors.Audio.AudioProjectCompiler.WwiseIDService;
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
using static Editors.Audio.GameSettings.Warhammer3.SoundBanks;
using Action = Editors.Audio.AudioEditor.AudioProjectData.Action;

namespace Editors.Audio.AudioProjectCompiler
{
    // TODO: encapsulate audio project compiler into appropriate classes
    public class AudioProjectCompiler
    {
        private readonly IPackFileService _packFileService;
        private readonly IAudioRepository _audioRepository;
        private readonly ApplicationSettingsService _applicationSettingsService;
        private readonly IFileSaveService _fileSaveService;
        private readonly SoundPlayer _soundPlayer;
        private readonly WemGenerator _wemGenerator;

        private IAudioProjectService _audioProjectService;

        private Dictionary<uint, List<uint>> UsedHircIdsByLanguageIDLookup { get; set; } = [];
        private Dictionary<uint, List<uint>> UsedSourceIdsByLanguageIDLookup { get; set; } = [];

        public AudioProjectCompiler(
            IPackFileService packFileService,
            IAudioRepository audioRepository,
            ApplicationSettingsService applicationSettingsService,
            IFileSaveService fileSaveService,
            SoundPlayer soundPlayer,
            WemGenerator wavToWemConverter)
        {
            _packFileService = packFileService;
            _audioRepository = audioRepository;
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

        // TODO: should proabably just make it generate for all languages?
        public void CompileAudioProject(IAudioProjectService audioProjectService, AudioProjectDataModel audioProject)
        {
            _audioProjectService = audioProjectService;
            _audioProjectService.SaveAudioProject();

            if (audioProject.SoundBanks == null)
                return;

            SetSoundBankData(audioProject);

            // We set the data from the bottom up, so Sounds, then Actions, then Events to ensure that IDs are generated before they're referenced.
            // For example IDs set in Sounds / Sound Containers are used in Actions, and IDs set in Actions are used in Events.
            SetInitialSourceData(audioProject);

            if (audioProject.SoundBanks.Any(soundBank => soundBank.ActionEvents != null))
            {
                SetActionData(audioProject);
                SetActionEventData(audioProject);
            }

            if (audioProject.SoundBanks.Any(soundBank => soundBank.DialogueEvents != null))
            {
                SetStatesData(audioProject);
                SetDialogueEventData(audioProject);
            }

            GenerateWems(audioProject);

            SetRemainingSourceData(audioProject);

            SaveWemsToPack(audioProject);

            GenerateSoundBanks(audioProject);

            GenerateEventDatFiles(audioProject);

            SaveGeneratedAudioProjectToPack(audioProject);
        }

        private void SetSoundBankData(AudioProjectDataModel audioProject)
        {
            foreach (var soundBank in audioProject.SoundBanks)
            {
                soundBank.SoundBankSubtype = GetSoundBankSubtype(soundBank.Name);
                soundBank.Language = AudioProjectCompilerHelpers.GetCorrectSoundBankLanguage(audioProject);

                var audioProjectFileName = _audioProjectService.AudioProjectFileName.Replace(" ", "_");
                soundBank.SoundBankFileName = $"{GetSoundBankName(soundBank.SoundBankSubtype)}_{audioProjectFileName}.bnk";

                var basePath = $"audio\\wwise";
                if (soundBank.Language == Languages.Sfx)
                    soundBank.SoundBankFilePath = $"{basePath}\\{soundBank.SoundBankFileName}";
                else
                    soundBank.SoundBankFilePath = $"{basePath}\\{soundBank.Language}\\{soundBank.SoundBankFileName}";

                soundBank.ID = WwiseHash.Compute(soundBank.SoundBankFileName.Replace(".bnk", string.Empty));
            }
        }

        private void SetInitialSourceData(AudioProjectDataModel audioProject)
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


        private void SetActionData(AudioProjectDataModel audioProject)
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

        private void SetActionEventData(AudioProjectDataModel audioProject)
        {
            foreach (var soundBank in audioProject.SoundBanks)
            {
                foreach (var actionEvent in soundBank.ActionEvents)
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

        private static void SetStatesData(AudioProjectDataModel audioProject)
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

        private static void SetDialogueEventData(AudioProjectDataModel audioProject)
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

        private void GenerateWems(AudioProjectDataModel audioProject)
        {
            DeleteAudioFilesInTempAudioFolder();

            var soundsWithUniqueSourceIds = GetAllUniqueSounds(audioProject);
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
            var soundsWithUniqueSourceIds = GetAllUniqueSounds(audioProject);
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

        private void SaveWemsToPack(AudioProjectDataModel audioProject)
        {
            var soundsWithUniqueSourceIds = GetAllUniqueSounds(audioProject);
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

                if (soundBank.DialogueEvents != null)
                    GenerateDialogueEventSoundBanks(wwiseHircGeneratorServiceFactory, soundBank, hircItems);

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

        private static void GenerateDialogueEventSoundBanks(WwiseHircGeneratorServiceFactory wwiseHircGeneratorServiceFactory, SoundBank soundBank, List<HircItem> hircItems)
        {
            var sourceHircs = new Dictionary<HircItem, List<HircItem>>();

            foreach (var dialogueEvent in soundBank.DialogueEvents)
            {
                foreach (var statePath in dialogueEvent.StatePaths)
                {
                    if (statePath.Sound != null)
                    {
                        var soundHirc = wwiseHircGeneratorServiceFactory.GenerateHirc(statePath.Sound, soundBank);
                        sourceHircs.Add(soundHirc, []);
                    }
                    else
                    {
                        var soundHircs = statePath.RandomSequenceContainer.Sounds
                            .Select(sound => wwiseHircGeneratorServiceFactory.GenerateHirc(sound, soundBank))
                            .ToList();

                        var randomSequenceContainerHirc = wwiseHircGeneratorServiceFactory.GenerateHirc(statePath.RandomSequenceContainer, soundBank);
                        sourceHircs.Add(randomSequenceContainerHirc, soundHircs);
                    }
                }
            }

            foreach (var sourceHirc in sourceHircs.OrderBy(sourceHirc => sourceHirc.Key.ID))
            {
                foreach (var soundHirc in sourceHirc.Value)
                    hircItems.Add(soundHirc);

                hircItems.Add(sourceHirc.Key);
            }

            foreach (var dialogueEvent in soundBank.DialogueEvents)
            {
                var actionHirc = wwiseHircGeneratorServiceFactory.GenerateHirc(dialogueEvent, soundBank);
                hircItems.Add(actionHirc);
            }
        }

        private void GenerateEventDatFiles(AudioProjectDataModel audioProject)
        {
            if (audioProject.SoundBanks.Any(soundBank => soundBank.ActionEvents != null))
            {
                foreach (var soundBank in audioProject.SoundBanks)
                    GenerateEventDatFile(soundBank);
            }

            if (audioProject.SoundBanks.Any(soundBank => soundBank.DialogueEvents != null))
                GenerateStatesDatFile(audioProject);
        }

        private void GenerateEventDatFile(SoundBank soundBank)
        {
            // TODO: Need to impelement something so it only makes dat files for the soundbanks that need it, so far I think that's at least movies.
            var soundDatFile = new SoundDatFile();

            foreach (var actionEvent in soundBank.ActionEvents)
                soundDatFile.EventWithStateGroup.Add(new SoundDatFile.DatEventWithStateGroup() { EventName = actionEvent.Name, Value = 400 });

            var datFileName = $"event_data__{soundBank.SoundBankFileName.Replace(".bnk", ".dat")}";
            var datFilePath = $"audio\\wwise\\{datFileName}";
            SaveDatFileToPack(soundDatFile, datFileName, datFilePath);
        }

        private void GenerateStatesDatFile(AudioProjectDataModel audioProject)
        {
            var stateDatFile = new SoundDatFile();

            foreach (var stateGroup in audioProject.StateGroups)
            {
                foreach (var state in stateGroup.States)
                    stateDatFile.EventWithStateGroup.Add(new SoundDatFile.DatEventWithStateGroup() { EventName = state.Name, Value = 400 });
            }

            var audioProjectFileName = _audioProjectService.AudioProjectFileName.Replace(" ", "_");
            var datFileName = $"states_data__{audioProjectFileName}.dat";
            var datFilePath = $"audio\\wwise\\{datFileName}";
            SaveDatFileToPack(stateDatFile, datFileName, datFilePath);
        }

        private void SaveDatFileToPack(SoundDatFile datFile, string datFileName, string datFilePath)
        {
            var bytes = DatFileParser.WriteData(datFile);
            var packFile = new PackFile(datFileName, new MemorySource(bytes));
            var reparsedSanityFile = DatFileParser.Parse(packFile, false);
            _fileSaveService.Save(datFilePath, packFile.DataSource.ReadData(), true);
        }

        private void SaveGeneratedAudioProjectToPack(AudioProjectDataModel audioProject)
        {
            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true
            };
            var audioProjectJson = JsonSerializer.Serialize(audioProject, options);
            var audioProjectFileName = $"{_audioProjectService.AudioProjectFileName}_generated.aproj";
            var audioProjectFilePath = $"{_audioProjectService.AudioProjectDirectory}\\{audioProjectFileName}";
            var packFile = PackFile.CreateFromASCII(audioProjectFileName, audioProjectJson);
            _fileSaveService.Save(audioProjectFilePath, packFile.DataSource.ReadData(), true);
        }

        public static List<Sound> GetAllUniqueSounds(AudioProjectDataModel audioProject)
        {
            // Extract sounds from ActionEvents if available.
            var actionSounds = audioProject.SoundBanks
                .SelectMany(soundBank => soundBank.ActionEvents ?? Enumerable.Empty<ActionEvent>())
                .SelectMany(actionEvent =>
                    actionEvent.Sound != null
                        ? [actionEvent.Sound]
                        : actionEvent.RandomSequenceContainer?.Sounds ?? Enumerable.Empty<Sound>());

            // Extract sounds from DialogueEvents if available.
            var dialogueSounds = audioProject.SoundBanks
                .SelectMany(soundBank => soundBank.DialogueEvents ?? Enumerable.Empty<DialogueEvent>())
                .SelectMany(dialogueEvent =>
                    dialogueEvent.StatePaths?.SelectMany(statePath =>
                        statePath.Sound != null
                            ? [statePath.Sound]
                            : statePath.RandomSequenceContainer?.Sounds ?? Enumerable.Empty<Sound>())
                    ?? Enumerable.Empty<Sound>());

            // Combine both lists and filter unique sounds based on SourceID.
            var allUniqueSounds = actionSounds
                .Concat(dialogueSounds)
                .DistinctBy(sound => sound.SourceID)
                .ToList();

            return allUniqueSounds;
        }
    }
}
