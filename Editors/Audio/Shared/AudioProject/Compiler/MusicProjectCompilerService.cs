using System.IO;
using Editors.Audio.Shared.AudioProject.Models;
using Editors.Audio.Shared.Dat;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Editors.Audio.Shared.Storage;
using Editors.Audio.Shared.Wwise.Generators;
using Shared.Core.Misc;
using Shared.GameFormats.Wwise.Wem.V132;

namespace Editors.Audio.Shared.AudioProject.Compiler
{
    public interface IMusicProjectCompilerService
    {
        void Compile(AudioProjectFile musicProject, string musicProjectFileName);
    }

    /// <summary>
    /// Compiles a music project (.mproj): both music scripts rebuilt from vanilla with every culture,
    /// the audio encoded, the music spliced into the vanilla .bnks, and the event data. Separate from
    /// <see cref="AudioProjectCompilerService"/>, which compiles Dialogue Event and Action Event projects.
    /// </summary>
    public class MusicProjectCompilerService(
        IMusicScriptBuildService musicScriptBuildService,
        IMusicSoundBankGeneratorService musicSoundBankGeneratorService,
        IWemGeneratorService wemGeneratorService,
        IDatGeneratorService datGeneratorService,
        IAudioRepository audioRepository) : IMusicProjectCompilerService
    {
        private readonly IMusicScriptBuildService _musicScriptBuildService = musicScriptBuildService;
        private readonly IMusicSoundBankGeneratorService _musicSoundBankGeneratorService = musicSoundBankGeneratorService;
        private readonly IWemGeneratorService _wemGeneratorService = wemGeneratorService;
        private readonly IDatGeneratorService _datGeneratorService = datGeneratorService;
        private readonly IAudioRepository _audioRepository = audioRepository;

        private readonly ILogger _logger = Logging.Create<MusicProjectCompilerService>();

        public void Compile(AudioProjectFile musicProject, string musicProjectFileName)
        {
            var music = musicProject.Music ?? throw new InvalidOperationException($"{musicProjectFileName} is not a music project.");

            // First: a culture that only borrows existing audio has no events of its own, but still
            // has to be wired into the scripts.
            if (music.Cultures.Count != 0)
            {
                var problems = _musicScriptBuildService.BuildAndSave(music.Cultures);
                if (problems.Count != 0)
                    throw new InvalidOperationException("The music scripts could not be built:\n  " + string.Join("\n  ", problems));
            }

            var soundBank = musicProject.SoundBanks.FirstOrDefault(bank => bank.GameSoundBank == Wh3SoundBank.GlobalMusic);
            if (soundBank == null || soundBank.ActionEvents.Count == 0)
                return;

            _audioRepository.Load([musicProject.Language]);
            _logger.Here().Information($"Compiling {musicProjectFileName}");

            GenerateWems(musicProject, music, soundBank);
            _musicSoundBankGeneratorService.GenerateSoundBanks(soundBank, music);

            _logger.Here().Information("Generating event data .dat");
            var stateGroups = musicProject.StateGroups.Count != 0 ? musicProject.StateGroups : null;
            _datGeneratorService.GenerateEventDatFile(Path.GetFileNameWithoutExtension(musicProjectFileName), soundBank.ActionEvents, stateGroups);
        }

        private void GenerateWems(AudioProjectFile musicProject, MusicProjectData music, SoundBank soundBank)
        {
            if (Directory.Exists(DirectoryHelper.AudioTempDirectory))
            {
                foreach (var file in Directory.GetFiles(DirectoryHelper.AudioTempDirectory, "*.wav").Concat(Directory.GetFiles(DirectoryHelper.AudioTempDirectory, "*.wem")))
                    File.Delete(file);
            }

            // Segments and pulse clips play their audio directly; the ambient fragments play it
            // through Sounds. A wav used in several places is encoded once.
            var fragmentSounds = music.AmsFragments
                .SelectMany(amsFragment => amsFragment.SoundIds)
                .Select(soundBank.GetSound)
                .Where(sound => sound != null)
                .ToList();

            var audioFiles = music.GetDirectSourceIds()
                .Concat(fragmentSounds.Select(sound => sound.SourceId))
                .Distinct()
                .Select(musicProject.GetAudioFile)
                .Where(audioFile => audioFile != null)
                .ToList();

            foreach (var audioFile in audioFiles)
                SetWemPaths(audioFile, soundBank.Language);

            // Drops audio whose wem the pack already holds, which keeps the durations it was given then.
            _wemGeneratorService.RemoveExistingAudioFilesAndSounds(audioFiles, fragmentSounds);
            if (audioFiles.Count == 0)
                return;

            _logger.Here().Information($"Generating {audioFiles.Count} WEMs");
            _wemGeneratorService.GenerateWems(audioFiles);
            _wemGeneratorService.SaveWemsToPack(audioFiles);

            foreach (var sound in fragmentSounds)
            {
                if (TryReadWemAudioData(musicProject, sound.SourceId, out var size, out _))
                    sound.InMemoryMediaSize = size;
            }

            UpdateMusicAudioData(musicProject, music);
        }

        private static void SetWemPaths(AudioFile audioFile, string language)
        {
            audioFile.WemPackFileName = $"{audioFile.Id}.wem";
            audioFile.WemDiskFilePath = $"{DirectoryHelper.AudioTempDirectory}\\{audioFile.WemPackFileName}";
            audioFile.WemPackFilePath = language == Wh3LanguageInformation.GetLanguageAsString(Wh3Language.Sfx)
                ? $"audio\\wwise\\{audioFile.WemPackFileName}"
                : $"audio\\wwise\\{language}\\{audioFile.WemPackFileName}";
        }

        /// <summary>
        /// A segment's or pulse clip's duration sets the segment length, the exit cue position and the
        /// clip's source duration, and a wrong value desynchronises the music from the file rather than
        /// failing. The encoded wem is what ships, so the sample count and rate are read back out of it.
        /// </summary>
        private static void UpdateMusicAudioData(AudioProjectFile musicProject, MusicProjectData music)
        {
            foreach (var segment in music.Segments)
            {
                if (TryReadWemAudioData(musicProject, segment.SourceId, out var size, out var durationMs))
                    (segment.InMemoryMediaSize, segment.DurationMs) = (size, durationMs);
            }

            foreach (var clip in music.AmsPulses.SelectMany(amsPulse => amsPulse.Clips))
            {
                if (TryReadWemAudioData(musicProject, clip.SourceId, out var size, out var durationMs))
                    (clip.InMemoryMediaSize, clip.DurationMs) = (size, durationMs);
            }
        }

        private static bool TryReadWemAudioData(AudioProjectFile musicProject, uint sourceId, out long size, out double durationMs)
        {
            (size, durationMs) = (0, 0);
            var audioFile = musicProject.GetAudioFile(sourceId);
            if (audioFile == null || !File.Exists(audioFile.WemDiskFilePath))
                return false;

            var wemBytes = File.ReadAllBytes(audioFile.WemDiskFilePath);
            var wemFile = WemFile.CreateFromWemBytes(wemBytes);

            size = wemBytes.Length;
            durationMs = wemFile.FmtChunk.SampleCount / (double)wemFile.FmtChunk.SampleRate * 1000;
            return true;
        }
    }
}
