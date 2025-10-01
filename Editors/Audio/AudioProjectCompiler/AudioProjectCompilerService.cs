using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Editors.Audio.AudioEditor.Models;
using Editors.Audio.GameInformation.Warhammer3;
using Editors.Audio.Utility;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Misc;

namespace Editors.Audio.AudioProjectCompiler
{
    public interface IAudioProjectCompilerService
    {
        void Compile(AudioProject audioProject, string audioProjectFileName, string audioProjectFilePath);
    }

    public class AudioProjectCompilerService(
        ISoundBankGeneratorService soundBankGeneratorService,
        IWemGeneratorService wemGeneratorService,
        IDatGeneratorService datGeneratorService) : IAudioProjectCompilerService
    {
        private readonly ISoundBankGeneratorService _soundBankGeneratorService = soundBankGeneratorService;
        private readonly IWemGeneratorService _wemGeneratorService = wemGeneratorService;
        private readonly IDatGeneratorService _datGeneratorService = datGeneratorService;

        private readonly ILogger _logger = Logging.Create<AudioProjectCompilerService>();

        public void Compile(AudioProject audioProject, string audioProjectFileName, string audioProjectFilePath)
        {
            if (audioProject.SoundBanks == null)
                return;

            _logger.Here().Information($"Compiling {audioProjectFileName}");

            var audioProjectFileNameWithoutExtension = Path.GetFileNameWithoutExtension(audioProjectFileName);

            ClearTempAudioFiles();

            SetSoundBankData(audioProject, audioProjectFileNameWithoutExtension);

            GenerateWems(audioProject);

            GenerateSoundBanks(audioProject);

            GenerateDatFiles(audioProject, audioProjectFileNameWithoutExtension);

            MemoryOptimiser.Optimise();
        }

        private static void ClearTempAudioFiles()
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

        private void SetSoundBankData(AudioProject audioProject, string audioProjectFileNameWithoutExtension)
        {
            _logger.Here().Information($"Setting SoundBank data");

            foreach (var soundBank in audioProject.SoundBanks)
            {
                soundBank.FileName = $"{soundBank.Name}.bnk";
                if (soundBank.Language == Wh3LanguageInformation.GetGameLanguageAsString(Wh3GameLanguage.Sfx))
                    soundBank.FilePath = $"audio\\wwise\\{soundBank.FileName}";
                else
                    soundBank.FilePath = $"audio\\wwise\\{soundBank.Language}\\{soundBank.FileName}";

                if (soundBank.DialogueEvents != null)
                {
                    // In WH3 .bnk files are loaded in descending name order. When a .bnk is loaded it overrides hircs with the same ID in .bnks loaded
                    // before it so the .bnk with the lowest alphanumeric name takes priority.
                    // Example load order:
                    // 1) campaign_vo__core.bnk
                    // 3) campaign_vo_01_project_name_for_testing.bnk
                    // 3) campaign_vo_0_audio_mixer.bnk
                    // So the dialogue events from campaign_vo_0_audio_mixer.bnk will be what take priority as they're loaded last.

                    var soundBankNameBase = soundBank.Name.Replace($"_{audioProjectFileNameWithoutExtension}", string.Empty);
                    soundBank.TestingFileName = $"{soundBankNameBase}_01_{audioProjectFileNameWithoutExtension}_for_testing.bnk";
                    soundBank.MergingFileName = $"{soundBank.Name}_for_merging.bnk";

                    if (soundBank.Language == Wh3LanguageInformation.GetGameLanguageAsString(Wh3GameLanguage.Sfx))
                    {
                        soundBank.TestingFilePath = $"audio\\wwise\\{soundBank.TestingFileName}";
                        soundBank.MergingFilePath = $"audio\\wwise\\{soundBank.MergingFileName}";
                    }
                    else
                    {
                        soundBank.TestingFilePath = $"audio\\wwise\\{soundBank.Language}\\{soundBank.TestingFileName}";
                        soundBank.MergingFilePath = $"audio\\wwise\\{soundBank.Language}\\{soundBank.MergingFileName}";
                    }

                    soundBank.TestingId = WwiseHash.Compute(soundBank.TestingFileName.Replace(".bnk", string.Empty));
                    soundBank.MergingId = WwiseHash.Compute(soundBank.MergingFileName.Replace(".bnk", string.Empty));
                }

                if (soundBank.ActionEvents != null)
                {
                    var playActionEvents = soundBank.GetPlayActionEvents();
                    foreach (var playActionEvent in playActionEvents)
                    {
                        foreach (var playAction in playActionEvent.Actions)
                        {
                            if (playAction.Sound != null)
                                SetSoundData(playAction.Sound, soundBank);
                            else
                                SetRandomSequenceContainerData(playAction.RandomSequenceContainer, soundBank);
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
                                SetSoundData(statePath.Sound, soundBank);
                            else
                                SetRandomSequenceContainerData(statePath.RandomSequenceContainer, soundBank);
                        }
                    }
                }
            }
        }

        private static void SetRandomSequenceContainerData(RandomSequenceContainer randomSequenceContainer, SoundBank soundBank)
        {
            foreach (var sound in randomSequenceContainer.Sounds)
                SetSoundData(sound, soundBank);

            randomSequenceContainer.Sounds = randomSequenceContainer.Sounds.OrderBy(sound => sound.Id).ToList();
        }

        private static void SetSoundData(Sound sound, SoundBank soundBank)
        {
            sound.WemPackFileName = $"{sound.SourceId}.wem";
            sound.WemDiskFilePath = $"{DirectoryHelper.Temp}\\Audio\\{sound.WemPackFileName}";
            if (soundBank.Language == Wh3LanguageInformation.GetGameLanguageAsString(Wh3GameLanguage.Sfx))
                sound.WemPackFilePath = $"audio\\wwise\\{sound.WemPackFileName}";
            else
                sound.WemPackFilePath = $"audio\\wwise\\{sound.Language}\\{sound.WemPackFileName}";
        }

        private void GenerateWems(AudioProject audioProject)
        {
            _logger.Here().Information($"Generating WEMs");

            var sounds = audioProject.GetSounds();
            var distinctSounds = audioProject.GetSounds().DistinctBy(sound => sound.SourceId).ToList();

            _wemGeneratorService.GenerateWems(distinctSounds);

            _wemGeneratorService.SaveWemsToPack(distinctSounds);

            UpdateSoundInMemoryMediaSize(sounds);
        }

        private static void UpdateSoundInMemoryMediaSize(List<Sound> sounds)
        {
            foreach (var sound in sounds)
            {
                var wemFileInfo = new FileInfo(sound.WemDiskFilePath);
                var fileSizeInBytes = wemFileInfo.Length;
                sound.InMemoryMediaSize = fileSizeInBytes;
            }
        }

        private void GenerateSoundBanks(AudioProject audioProject)
        {
            foreach (var soundBank in audioProject.SoundBanks)
            {
                if (soundBank.ActionEvents != null || soundBank.DialogueEvents != null)
                {
                    _logger.Here().Information($"Generating SoundBank {soundBank.FilePath}");

                    // Create the .bnk that modders should keep
                    _soundBankGeneratorService.GenerateSoundBankWithoutDialogueEvents(soundBank);

                    // Create the .bnk that modders should give to the merger
                    _soundBankGeneratorService.GenerateMergingSoundBank(soundBank);

                    if (soundBank.DialogueEvents != null)
                    {
                        // Create a .bnk of the compiled Dialogue Events merged with vanilla Dialogue Events for modders to test
                        _logger.Here().Information($"Generating SoundBank {soundBank.TestingFilePath} and {soundBank.MergingFilePath}");
                        _soundBankGeneratorService.GenerateDialogueEventsForTestingSoundBank(soundBank);
                    }
                }

            }
        }

        private void GenerateDatFiles(AudioProject audioProject, string audioProjectFileNameWithoutExtension)
        {
            // The .dat file is seems to only necessary for playing movie Action Events and any triggered via common.trigger_soundevent()
            // but without testing all the different types of Action Event sounds it's safer to just make a .dat for all as it's little overhead.
            var actionEvents = audioProject.GetActionEvents();
            if (actionEvents != null && actionEvents.Count != 0)
            {
                _logger.Here().Information($"Generating Event .dat");
                _datGeneratorService.GenerateEventDatFile(actionEvents, audioProjectFileNameWithoutExtension);
            }

            // We create the states .dat file so we can see the modded states in the Audio Explorer, it isn't necessary for the game
            if (audioProject.StateGroups != null && audioProject.StateGroups.Count != 0)
            {
                _logger.Here().Information($"Generating States .dat");
                _datGeneratorService.GenerateStatesDatFile(audioProject.StateGroups, audioProjectFileNameWithoutExtension);
            }
        }
    }
}
