using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Editors.Audio.Shared.AudioProject.Models;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;

namespace Editors.Audio.Shared.AudioProject
{
    public record AudioProjectFileServiceLoadResult(AudioProjectFile AudioProject, string FileName, string FilePath);

    public interface IAudioProjectFileService
    {
        void Save(AudioProjectFile audioProject, string fileName, string filePath);
        AudioProjectFile DeserialiseAudioProject(PackFile packFile);
        AudioProjectFileServiceLoadResult Load(string fileName, string filePath);
        AudioProjectFileServiceLoadResult LoadFromDialog();
    }

    public class AudioProjectFileService(
        IPackFileService packFileService,
        IFileSaveService fileSaveService,
        IStandardDialogs standardDialogs) : IAudioProjectFileService
    {
        private readonly IPackFileService _packFileService = packFileService;
        private readonly IFileSaveService _fileSaveService = fileSaveService;
        private readonly IStandardDialogs _standardDialogs = standardDialogs;

        public void Save(AudioProjectFile audioProject, string fileName, string filePath)
        {
            var cleanedAudioProject = AudioProjectFile.Clean(audioProject);

            var options = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, WriteIndented = true };
            var audioProjectJson = JsonSerializer.Serialize(cleanedAudioProject, options);

            var packFile = PackFile.CreateFromASCII(fileName, audioProjectJson);
            _fileSaveService.Save(filePath, packFile.DataSource.ReadData(), false);
        }

        public AudioProjectFileServiceLoadResult Load(string fileName, string filePath)
        {
            var packFile = _packFileService.FindFile(filePath);
            var audioProject = DeserialiseAudioProject(packFile);
            return new AudioProjectFileServiceLoadResult(audioProject, fileName, filePath);
        }

        public AudioProjectFileServiceLoadResult LoadFromDialog()
        {
            var result = _standardDialogs.DisplayBrowseDialog([".aproj"]);
            if (result.Result)
            {
                var filePath = _packFileService.GetFullPath(result.File);
                var fileName = Path.GetFileName(filePath);
                return Load(fileName, filePath);
            }
            return null;
        }

        public AudioProjectFile DeserialiseAudioProject(PackFile packFile)
        {
            var bytes = packFile.DataSource.ReadData();
            var audioProjectJson = Encoding.UTF8.GetString(bytes);
            var audioProject = JsonSerializer.Deserialize<AudioProjectFile>(audioProjectJson);
            return audioProject;
        }
    }
}
