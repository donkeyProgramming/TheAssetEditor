using System.IO;
using Editors.Audio.Shared.AudioProject.Models;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Utility;
using Shared.GameFormats.Wwise.Wem.V132;

namespace Editors.Audio.Shared.Wwise.Generators
{
    public interface IWemGeneratorService
    {
        void RemoveExistingAudioFilesAndSounds(List<AudioFile> audioFiles, List<Sound> sounds);
        void GenerateWems(List<AudioFile> audioFiles);
        void SaveWemsToPack(List<AudioFile> audioFiles);
    }

    public class WemGeneratorService(IPackFileService packFileService) : IWemGeneratorService
    {
        private readonly IPackFileService _packFileService = packFileService;

        private readonly ILogger _logger = Logging.Create<WemGeneratorService>();

        public void RemoveExistingAudioFilesAndSounds(List<AudioFile> audioFiles, List<Sound> sounds)
        {
            // Make a copy so we remove from the original
            foreach (var audioFile in audioFiles.ToList())
            {
                var wemFile = _packFileService.FindFile(audioFile.WemPackFilePath);
                if (wemFile != null)
                {
                    audioFiles.Remove(audioFile);
                    sounds.Remove(sounds.FirstOrDefault(sound => sound.SourceId == audioFile.Id));
                }
            }
        }

        public void GenerateWems(List<AudioFile> audioFiles)
        {
            foreach (var audioFile in audioFiles)
            {
                var wavFile = _packFileService.FindFile(audioFile.WavPackFilePath);
                var wavBytes = wavFile.DataSource.ReadData();
                var wemBytes = WemFile.CreateFromWavBytes(wavBytes).WriteData();

                try
                {
                    DirectoryHelper.EnsureFileFolderCreated(audioFile.WemDiskFilePath);
                    File.WriteAllBytes(audioFile.WemDiskFilePath, wemBytes);
                }
                catch (Exception e)
                {
                    _logger.Here().Error(e.Message);
                }
            }
        }

        public void SaveWemsToPack(List<AudioFile> audioFiles)
        {
            var wemFiles = new List<PackFileUtil.FileRef>();
            foreach (var audioFile in audioFiles)
                wemFiles.Add(new PackFileUtil.FileRef(audioFile.WemDiskFilePath, Path.GetDirectoryName(audioFile.WemPackFilePath)));

            PackFileUtil.LoadFilesFromDisk(_packFileService, wemFiles);
        }
    }
}
