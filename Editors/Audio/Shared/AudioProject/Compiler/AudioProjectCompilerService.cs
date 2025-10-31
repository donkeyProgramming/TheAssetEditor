using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Editors.Audio.Shared.AudioProject.Models;
using Editors.Audio.Shared.Dat;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Editors.Audio.Shared.Wwise;
using Editors.Audio.Shared.Wwise.Generators;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Misc;

namespace Editors.Audio.Shared.AudioProject.Compiler
{
    public interface IAudioProjectCompilerService
    {
        void Compile(AudioProjectFile audioProject, string audioProjectFileName, string audioProjectFilePath);
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

        public void Compile(AudioProjectFile audioProject, string audioProjectFileName, string audioProjectFilePath)
        {
            if (audioProject.SoundBanks == null)
                return;

            _logger.Here().Information($"Compiling {audioProjectFileName}");

            var audioFiles = new List<AudioFile>();
            var sounds = new List<Sound>();
            var audioProjectFileNameWithoutExtension = Path.GetFileNameWithoutExtension(audioProjectFileName);

            ClearTempAudioFiles();
            SetSoundBankData(audioProject, audioProjectFileNameWithoutExtension, audioFiles, sounds);
            GenerateWems(audioProject, audioFiles, sounds);
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

        private void SetSoundBankData(AudioProjectFile audioProject, string audioProjectFileNameWithoutExtension, List<AudioFile> audioFiles, List<Sound> sounds)
        {
            _logger.Here().Information($"Setting SoundBank data");

            foreach (var soundBank in audioProject.SoundBanks)
            {
                soundBank.FileName = $"{soundBank.Name}.bnk";
                if (soundBank.Language == Wh3LanguageInformation.GetLanguageAsString(Wh3Language.Sfx))
                    soundBank.FilePath = $"audio\\wwise\\{soundBank.FileName}";
                else
                    soundBank.FilePath = $"audio\\wwise\\{soundBank.Language}\\{soundBank.FileName}";

                if (soundBank.DialogueEvents.Count != 0)
                {
                    // In WH3 .bnk files are loaded in descending name order. When a .bnk is loaded it overrides hircs with the same ID in .bnks loaded
                    // before it so the .bnk with the lowest alphanumeric name takes priority.
                    // Example load order:
                    // 1) campaign_vo__core.bnk
                    // 3) campaign_vo_1_project_name_for_testing.bnk
                    // 3) campaign_vo_0_audio_mixer.bnk
                    // So the dialogue events from campaign_vo_0_audio_mixer.bnk will be what take priority as they're loaded last.

                    var soundBankNameBase = soundBank.Name.Replace($"_{audioProjectFileNameWithoutExtension}", string.Empty);
                    soundBank.TestingFileName = $"{soundBankNameBase}_1_{audioProjectFileNameWithoutExtension}_for_testing.bnk";
                    soundBank.MergingFileName = $"{soundBank.Name}_for_merging.bnk";

                    if (soundBank.Language == Wh3LanguageInformation.GetLanguageAsString(Wh3Language.Sfx))
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

                if (soundBank.ActionEvents.Count != 0)
                {
                    var playActionEvents = soundBank.GetPlayActionEvents();
                    foreach (var playActionEvent in playActionEvents)
                    {
                        foreach (var playAction in playActionEvent.Actions)
                        {
                            if (playAction.TargetHircTypeIsSound())
                            {
                                var sound = soundBank.GetSound(playAction.TargetHircId);
                                var audioFile = audioProject.GetAudioFile(sound.SourceId);

                                SetSoundData(audioFile, soundBank);

                                audioFiles.Add(audioFile);
                                sounds.Add(sound);
                            }
                            else if (playAction.TargetHircTypeIsRandomSequenceContainer())
                            {
                                var randomSequenceContainer = soundBank.GetRandomSequenceContainer(playAction.TargetHircId);

                                SetRandomSequenceContainerData(audioProject, randomSequenceContainer, soundBank);

                                var randomSequenceContainerSounds = soundBank.GetSounds(randomSequenceContainer.Children);
                                audioFiles.AddRange(randomSequenceContainerSounds.Select(sound => audioProject.GetAudioFile(sound.SourceId)).ToList());
                                sounds.AddRange(randomSequenceContainerSounds);
                            }
                        }
                    }
                }

                if (soundBank.DialogueEvents.Count != 0)
                {
                    foreach (var dialogueEvent in soundBank.DialogueEvents)
                    {
                        foreach (var statePath in dialogueEvent.StatePaths)
                        {
                            if (statePath.TargetHircTypeIsSound())
                            {
                                var sound = soundBank.GetSound(statePath.TargetHircId);
                                var audioFile = audioProject.GetAudioFile(sound.SourceId);

                                SetSoundData(audioFile, soundBank);

                                audioFiles.Add(audioFile);
                                sounds.Add(sound);
                            }
                            else if (statePath.TargetHircTypeIsRandomSequenceContainer())
                            {
                                var randomSequenceContainer = soundBank.GetRandomSequenceContainer(statePath.TargetHircId);

                                SetRandomSequenceContainerData(audioProject, randomSequenceContainer, soundBank);

                                var randomSequenceContainerSounds = soundBank.GetSounds(randomSequenceContainer.Children);
                                audioFiles.AddRange(randomSequenceContainerSounds.Select(sound => audioProject.GetAudioFile(sound.SourceId)).ToList());
                                sounds.AddRange(randomSequenceContainerSounds);
                            }
                        }
                    }
                }
            }
        }

        private static void SetRandomSequenceContainerData(AudioProjectFile audioProject, RandomSequenceContainer randomSequenceContainer, SoundBank soundBank)
        {
            var sounds = soundBank.GetSounds(randomSequenceContainer.Children);
            foreach (var sound in sounds)
            {
                var audioFile = audioProject.GetAudioFile(sound.SourceId);
                SetSoundData(audioFile, soundBank);
            }
            randomSequenceContainer.Children = randomSequenceContainer.Children.OrderBy(soundId => soundId).ToList();
        }

        private static void SetSoundData(AudioFile audioFile, SoundBank soundBank)
        {
            audioFile.WemPackFileName = $"{audioFile.Id}.wem";
            audioFile.WemDiskFilePath = $"{DirectoryHelper.Temp}\\Audio\\{audioFile.WemPackFileName}";
            
            if (soundBank.Language == Wh3LanguageInformation.GetLanguageAsString(Wh3Language.Sfx))
                audioFile.WemPackFilePath = $"audio\\wwise\\{audioFile.WemPackFileName}";
            else
                audioFile.WemPackFilePath = $"audio\\wwise\\{soundBank.Language}\\{audioFile.WemPackFileName}";
        }

        private void GenerateWems(AudioProjectFile audioProject, List<AudioFile> audioFiles, List<Sound> sounds)
        {
            _wemGeneratorService.RemoveExistingAudioFilesAndSounds(audioFiles, sounds);

            if (audioFiles.Count > 0)
            {
                _logger.Here().Information($"Generating {audioFiles.Count} WEMs");
                _wemGeneratorService.GenerateWems(audioFiles);
                _wemGeneratorService.SaveWemsToPack(audioFiles);
                UpdateSoundInMemoryMediaSize(audioProject, sounds);
            }
        }

        private static void UpdateSoundInMemoryMediaSize(AudioProjectFile audioProject, List<Sound> sounds)
        {
            foreach (var sound in sounds)
            {
                var audioFile = audioProject.GetAudioFile(sound.SourceId);
                var wemFileInfo = new FileInfo(audioFile.WemDiskFilePath);
                var fileSizeInBytes = wemFileInfo.Length;
                sound.InMemoryMediaSize = fileSizeInBytes;
            }
        }

        private void GenerateSoundBanks(AudioProjectFile audioProject)
        {
            foreach (var soundBank in audioProject.SoundBanks)
            {
                if (soundBank.ActionEvents.Count != 0 || soundBank.DialogueEvents.Count != 0)
                {
                    _logger.Here().Information($"Generating SoundBank {soundBank.FilePath}");

                    // Create the .bnk that modders should keep
                    _soundBankGeneratorService.GenerateSoundBankWithoutDialogueEvents(soundBank);

                    if (soundBank.DialogueEvents.Count != 0)
                    {
                        // Create a .bnk of the compiled Dialogue Events merged with vanilla Dialogue Events for modders to test
                        _logger.Here().Information($"Generating SoundBank {soundBank.TestingFilePath} and {soundBank.MergingFilePath}");
                        _soundBankGeneratorService.GenerateDialogueEventsForTestingSoundBank(soundBank);

                        // Create the .bnk that modders should give to the merger
                        _soundBankGeneratorService.GenerateMergingSoundBank(soundBank);
                    }
                }

            }
        }

        private void GenerateDatFiles(AudioProjectFile audioProject, string audioProjectFileNameWithoutExtension)
        {
            // The .dat file is seems to only necessary for Action Events for movies, quest battles or anything triggered via common.trigger_soundevent()
            // but without testing all the different types of Action Event sounds it's safer to just make a .dat for all.
            // We store States in there so we can display them in the Audio Explorer.
            var actionEvents = audioProject.GetActionEvents();
            if (actionEvents.Count != 0 && audioProject.StateGroups != null && audioProject.StateGroups.Count != 0)
            {
                _logger.Here().Information($"Generating event data .dat");
                _datGeneratorService.GenerateEventDatFile(audioProjectFileNameWithoutExtension, actionEvents, audioProject.StateGroups);
            }
            else if (actionEvents.Count != 0 && audioProject.StateGroups == null)
            {
                _logger.Here().Information($"Generating event data .dat");
                _datGeneratorService.GenerateEventDatFile(audioProjectFileNameWithoutExtension, actionEvents: actionEvents);
            }
            else if (actionEvents.Count == 0 && audioProject.StateGroups != null && audioProject.StateGroups.Count != 0)
            {
                _logger.Here().Information($"Generating event data .dat");
                _datGeneratorService.GenerateEventDatFile(audioProjectFileNameWithoutExtension, stateGroups: audioProject.StateGroups);
            }
        }
    }
}
