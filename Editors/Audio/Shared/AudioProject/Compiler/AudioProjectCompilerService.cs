using System.Data;
using System.IO;
using Editors.Audio.Shared.AudioProject.Models;
using Editors.Audio.Shared.Dat;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Editors.Audio.Shared.Storage;
using Editors.Audio.Shared.Wwise.Generators;
using Shared.Core.Misc;
using Shared.GameFormats.Wwise;
using Shared.GameFormats.Wwise.Wem.V132;

namespace Editors.Audio.Shared.AudioProject.Compiler
{
    public interface IAudioProjectCompilerService
    {
        void Compile(AudioProjectFile audioProject, string audioProjectFileName, string audioProjectFilePath);
    }

    public class AudioProjectCompilerService(
        ISoundBankGeneratorService soundBankGeneratorService,
        IWemGeneratorService wemGeneratorService,
        IDatGeneratorService datGeneratorService,
        IAudioRepository audioRepository,
        IMusicScriptBuildService musicScriptBuildService) : IAudioProjectCompilerService
    {
        private readonly ISoundBankGeneratorService _soundBankGeneratorService = soundBankGeneratorService;
        private readonly IWemGeneratorService _wemGeneratorService = wemGeneratorService;
        private readonly IDatGeneratorService _datGeneratorService = datGeneratorService;
        private readonly IAudioRepository _audioRepository = audioRepository;
        private readonly IMusicScriptBuildService _musicScriptBuildService = musicScriptBuildService;

        private readonly ILogger _logger = Logging.Create<AudioProjectCompilerService>();

        public void Compile(AudioProjectFile audioProject, string audioProjectFileName, string audioProjectFilePath)
        {
            // Ahead of the check below: a culture that only borrows existing audio has no
            // SoundBank of its own but still has to be wired into the scripts.
            if (audioProject.MusicCultures is { Count: > 0 })
            {
                var problems = _musicScriptBuildService.BuildAndSave(audioProject.MusicCultures);
                if (problems.Count != 0)
                    throw new InvalidOperationException("The music scripts could not be built:\n  " + string.Join("\n  ", problems));
            }

            if (audioProject.SoundBanks.Count == 0)
                return;

            _audioRepository.Load([audioProject.Language]);
            _logger.Here().Information($"Compiling {audioProjectFileName}");

            var audioFiles = new List<AudioFile>();
            var sounds = new List<Sound>();
            var audioProjectNameWithoutExtension = Path.GetFileNameWithoutExtension(audioProjectFileName);

            ClearTempAudioFiles();
            SetSoundBankData(audioProject, audioProjectNameWithoutExtension, audioFiles, sounds);
            GenerateWems(audioProject, audioFiles, sounds);
            GenerateSoundBanks(audioProject);
            GenerateDatFiles(audioProject, audioProjectNameWithoutExtension);

        }

        private static void ClearTempAudioFiles()
        {
            if (Directory.Exists(DirectoryHelper.AudioTempDirectory))
            {
                foreach (var file in Directory.GetFiles(DirectoryHelper.AudioTempDirectory, "*.wav"))
                    File.Delete(file);

                foreach (var file in Directory.GetFiles(DirectoryHelper.AudioTempDirectory, "*.wem"))
                    File.Delete(file);
            }
        }

        private void SetSoundBankData(AudioProjectFile audioProject, string audioProjectNameWithoutExtension, List<AudioFile> audioFiles, List<Sound> sounds)
        {
            _logger.Here().Information($"Setting SoundBank data");

            foreach (var soundBank in audioProject.SoundBanks)
            {
                soundBank.FileName = $"{soundBank.Name}.bnk";
                soundBank.AudioProjectName = audioProjectNameWithoutExtension;
                if (soundBank.Language == Wh3LanguageInformation.GetLanguageAsString(Wh3Language.Sfx))
                    soundBank.FilePath = $"audio\\wwise\\{soundBank.FileName}";
                else
                    soundBank.FilePath = $"audio\\wwise\\{soundBank.Language}\\{soundBank.FileName}";

                // Dialogue Events re-emit hircs that already exist in vanilla, so they need somewhere
                // to put them other than the .bnk the modder keeps. Music does too, but ships them
                // spliced into the vanilla .bnks under vanilla's own names, so it needs neither name.
                if (soundBank.DialogueEvents.Count != 0)
                {
                    // In WH3 .bnk files are loaded in descending name order. When a .bnk is loaded it overrides hircs with the same ID in .bnks loaded
                    // before it so the .bnk with the lowest alphanumeric name takes priority.
                    // Example load order:
                    // 1) campaign_vo__core.bnk
                    // 3) campaign_vo_1_project_name_for_testing.bnk
                    // 3) campaign_vo_0_audio_mixer.bnk
                    // So the dialogue events from campaign_vo_0_audio_mixer.bnk will be what take priority as they're loaded last.

                    var soundBankNameBase = soundBank.Name.Replace($"_{audioProjectNameWithoutExtension}", string.Empty);
                    soundBank.TestingFileName = $"{soundBankNameBase}_1_{audioProjectNameWithoutExtension}_for_testing.bnk";
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
                    SetActionEventData(audioProject, audioFiles, sounds, soundBank);

                if (soundBank.DialogueEvents.Count != 0)
                    SetDialogueEventData(audioProject, audioFiles, sounds, soundBank);

                if (soundBank.MusicSegments.Count != 0)
                    SetMusicData(audioProject, audioFiles, soundBank);

                if (soundBank.AmsPulses.Count != 0)
                    SetAmsPulseData(audioProject, audioFiles, soundBank);
            }
        }

        // Music segments carry their audio directly rather than through a Sound, so they need the
        // same wem paths setting but have no Sound to add alongside.
        private static void SetMusicData(AudioProjectFile audioProject, List<AudioFile> audioFiles, SoundBank soundBank)
        {
            foreach (var musicSegment in soundBank.MusicSegments)
            {
                // Battle culture music has a segment per container over the same audio, which only
                // needs encoding once.
                var audioFile = audioProject.GetAudioFile(musicSegment.SourceId);
                if (audioFile == null || audioFiles.Contains(audioFile))
                    continue;

                SetSoundData(audioFile, soundBank);
                audioFiles.Add(audioFile);
            }
        }

        /// <summary>
        /// Pulse clips carry their audio directly, the same as a music segment, and need the same wem
        /// paths setting. They have no Sound and no segment hirc of their own - the clip becomes a row
        /// in a vanilla switch track - so this only has the audio file to prepare.
        /// </summary>
        private static void SetAmsPulseData(AudioProjectFile audioProject, List<AudioFile> audioFiles, SoundBank soundBank)
        {
            foreach (var amsPulse in soundBank.AmsPulses)
            {
                foreach (var clip in amsPulse.Clips)
                {
                    var audioFile = audioProject.GetAudioFile(clip.SourceId);
                    if (audioFile == null)
                        continue;

                    SetSoundData(audioFile, soundBank);
                    audioFiles.Add(audioFile);
                }
            }
        }

        private static void SetActionEventData(AudioProjectFile audioProject, List<AudioFile> audioFiles, List<Sound> sounds, SoundBank soundBank)
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

        private static void SetDialogueEventData(AudioProjectFile audioProject, List<AudioFile> audioFiles, List<Sound> sounds, SoundBank soundBank)
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
            audioFile.WemDiskFilePath = $"{DirectoryHelper.AudioTempDirectory}\\{audioFile.WemPackFileName}";
            
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
                UpdateMusicSegmentAudioData(audioProject);
                UpdateAmsPulseAudioData(audioProject);
            }
        }

        /// <summary>
        /// A music segment's duration has to match its audio: it sets the segment length, the exit
        /// cue position and the clip's source duration, and a wrong value desynchronises the segment
        /// from the file rather than failing. The encoded wem is the authority - it is what ships -
        /// so the sample count and rate are read back out of it rather than off the source wav.
        /// </summary>
        private static void UpdateMusicSegmentAudioData(AudioProjectFile audioProject)
        {
            foreach (var soundBank in audioProject.SoundBanks)
            {
                foreach (var musicSegment in soundBank.MusicSegments)
                {
                    var audioFile = audioProject.GetAudioFile(musicSegment.SourceId);
                    if (audioFile == null || !File.Exists(audioFile.WemDiskFilePath))
                        continue;

                    var wemBytes = File.ReadAllBytes(audioFile.WemDiskFilePath);
                    var wemFile = WemFile.CreateFromWemBytes(wemBytes);

                    musicSegment.InMemoryMediaSize = wemBytes.Length;
                    musicSegment.DurationMs = wemFile.FmtChunk.SampleCount / (double)wemFile.FmtChunk.SampleRate * 1000;
                }
            }
        }

        /// <summary>
        /// The same reckoning as a music segment's, for the same reason: the clip declares how long
        /// its audio runs and how much of it to stream, and a wrong value desynchronises the pulse
        /// from the music rather than failing. The encoded wem is the authority since it is what ships.
        /// </summary>
        private static void UpdateAmsPulseAudioData(AudioProjectFile audioProject)
        {
            foreach (var soundBank in audioProject.SoundBanks)
            {
                foreach (var clip in soundBank.AmsPulses.SelectMany(amsPulse => amsPulse.Clips))
                {
                    var audioFile = audioProject.GetAudioFile(clip.SourceId);
                    if (audioFile == null || !File.Exists(audioFile.WemDiskFilePath))
                        continue;

                    var wemBytes = File.ReadAllBytes(audioFile.WemDiskFilePath);
                    var wemFile = WemFile.CreateFromWemBytes(wemBytes);

                    clip.InMemoryMediaSize = wemBytes.Length;
                    clip.DurationMs = wemFile.FmtChunk.SampleCount / (double)wemFile.FmtChunk.SampleRate * 1000;
                }
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
                if (soundBank.ActionEvents.Count != 0 || soundBank.DialogueEvents.Count != 0
                    || soundBank.MusicRandomSequences.Count != 0 || soundBank.AmsPulses.Count != 0)
                {
                    // Music only works spliced into the vanilla .bnks themselves, so a music bank
                    // compiles to those and nothing else: the mod's own .bnk would define every one of
                    // the same hircs a second time, and the merging .bnk would too since its name puts
                    // it in the same load as the music .bnks. See GenerateMusicReplacementSoundBanks.
                    if (soundBank.MusicRandomSequences.Count != 0 || soundBank.AmsPulses.Count != 0 || soundBank.AmsFragments.Count != 0)
                    {
                        _soundBankGeneratorService.GenerateMusicReplacementSoundBanks(soundBank);
                        continue;
                    }

                    _logger.Here().Information($"Generating SoundBank {soundBank.FilePath}");

                    // Create the .bnk that modders should keep
                    _soundBankGeneratorService.GenerateSoundBankWithoutMergedHircs(soundBank);

                    if (soundBank.DialogueEvents.Count != 0)
                    {
                        // Create a .bnk of the compiled Dialogue Events merged with vanilla Dialogue Events for modders to test
                        _logger.Here().Information($"Generating SoundBank {soundBank.TestingFilePath}");
                        _soundBankGeneratorService.GenerateDialogueEventsForTestingSoundBank(soundBank);
                    }

                    if (soundBank.DialogueEvents.Count != 0)
                    {
                        // Create the .bnk that modders should give to the merger
                        _logger.Here().Information($"Generating SoundBank {soundBank.MergingFilePath}");
                        _soundBankGeneratorService.GenerateMergingSoundBank(soundBank);
                    }
                }

            }
        }

        private void GenerateDatFiles(AudioProjectFile audioProject, string audioProjectNameWithoutExtension)
        {
            // In WH3 .dat files only seem necessary for Action Events for movies or anything triggered via common.trigger_soundevent()
            // but without testing all the different types of Action Event sounds it's safer to just make a .dat for all.
            // We store States in there so we can display them in the Audio Explorer.
            var actionEvents = audioProject.GetActionEvents();
            var hasActionEvents = actionEvents != null && actionEvents.Count > 0;
            var hasStateGroups = audioProject.StateGroups.Count != 0 && audioProject.StateGroups.Count > 0;

            if (hasActionEvents && hasStateGroups)
            {
                _logger.Here().Information($"Generating event data .dat");
                _datGeneratorService.GenerateEventDatFile(audioProjectNameWithoutExtension, actionEvents, audioProject.StateGroups);
            }
            else if (hasActionEvents && !hasStateGroups)
            {
                _logger.Here().Information($"Generating event data .dat");
                _datGeneratorService.GenerateEventDatFile(audioProjectNameWithoutExtension, actionEvents: actionEvents);
            }
            else if (!hasActionEvents && hasStateGroups)
            {
                _logger.Here().Information($"Generating event data .dat");
                _datGeneratorService.GenerateEventDatFile(audioProjectNameWithoutExtension, stateGroups: audioProject.StateGroups);
            }
        }
    }
}
