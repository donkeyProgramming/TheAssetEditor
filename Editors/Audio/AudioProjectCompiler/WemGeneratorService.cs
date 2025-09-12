using System.Collections.Generic;
using System.IO;
using Editors.Audio.AudioEditor.Models;
using Editors.Audio.Utility;
using Shared.Core.Misc;
using Shared.Core.PackFiles;

namespace Editors.Audio.AudioProjectCompiler
{
    public interface IWemGeneratorService
    {
        void GenerateWems(List<Sound> sounds);
        void SaveWemToPack(string wemDiskFilePath, string wemPackFilePath);
    }

    public class WemGeneratorService(IPackFileService packFileService, WSourcesWrapper wSourcesWrapper, SoundPlayer soundPlayer) : IWemGeneratorService
    {
        private readonly IPackFileService _packFileService = packFileService;
        private readonly WSourcesWrapper _wSourcesWrapper = wSourcesWrapper;
        private readonly SoundPlayer _soundPlayer = soundPlayer;

        public void GenerateWems(List<Sound> sounds)
        {
            var wavToWemFolderPath = $"{DirectoryHelper.Temp}\\WavToWem";
            var audioFolderPath = $"{DirectoryHelper.Temp}\\Audio";
            var wprojPath = $"{DirectoryHelper.Temp}\\WavToWem\\WavToWemWwiseProject\\WavToWem.wproj";
            var wsourcesPath = $"{DirectoryHelper.Temp}\\WavToWem\\wav_to_wem.wsources";

            var wavFileNames = new List<string>();
            foreach (var sound in sounds)
            {
                var wavFile = _packFileService.FindFile(sound.WavPackFilePath);
                var wavFileName = $"{sound.SourceId}.wav";
                _soundPlayer.ExportFileToAEFolder(wavFileName, wavFile.DataSource.ReadData());
                wavFileNames.Add(wavFileName);
            }

            _wSourcesWrapper.InitialiseWwiseProject();

            DirectoryHelper.EnsureCreated(wavToWemFolderPath);

            _wSourcesWrapper.CreateWsourcesFile(wavFileNames);

            var arguments = $"\"{wprojPath}\" -ConvertExternalSources \"{wsourcesPath}\" -ExternalSourcesOutput \"{audioFolderPath}\"";
            _wSourcesWrapper.RunExternalCommand(arguments);
            _wSourcesWrapper.DeleteExcessStuff();
        }

        public void SaveWemToPack(string wemDiskFilePath, string wemPackFilePath)
        {
            PackFileUtil.LoadFileFromDisk(_packFileService, new PackFileUtil.FileRef(wemDiskFilePath, Path.GetDirectoryName(wemPackFilePath)));
        }
    }
}
