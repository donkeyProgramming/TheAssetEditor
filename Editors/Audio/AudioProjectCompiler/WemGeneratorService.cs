using System;
using System.Collections.Generic;
using System.IO;
using Editors.Audio.AudioEditor.Models;
using Editors.Audio.Utility;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Misc;
using Shared.Core.PackFiles;

namespace Editors.Audio.AudioProjectCompiler
{
    public interface IWemGeneratorService
    {
        void GenerateWems(List<AudioFile> audioFiles);
        void SaveWemsToPack(List<AudioFile> audioFiles);
    }

    public class WemGeneratorService(IPackFileService packFileService, WSourcesWrapper wSourcesWrapper) : IWemGeneratorService
    {
        private readonly IPackFileService _packFileService = packFileService;
        private readonly WSourcesWrapper _wSourcesWrapper = wSourcesWrapper;

        private readonly ILogger _logger = Logging.Create<WemGeneratorService>();

        public void GenerateWems(List<AudioFile> audioFiles)
        {
            var wavToWemFolderPath = $"{DirectoryHelper.Temp}\\WavToWem";
            var audioFolderPath = $"{DirectoryHelper.Temp}\\Audio";
            var wprojPath = $"{DirectoryHelper.Temp}\\WavToWem\\WavToWemWwiseProject\\WavToWem.wproj";
            var wsourcesPath = $"{DirectoryHelper.Temp}\\WavToWem\\wav_to_wem.wsources";

            var wavFileNames = new List<string>();
            foreach (var audioFile in audioFiles)
            {
                var wavFile = _packFileService.FindFile(audioFile.WavPackFilePath);
                var wavFileName = $"{audioFile.Id}.wav";
                var wavFilePath = $"{audioFolderPath}\\{wavFileName}";

                ExportWav(wavFilePath, wavFile.DataSource.ReadData());

                wavFileNames.Add(wavFileName);
            }

            _wSourcesWrapper.InitialiseWwiseProject();

            DirectoryHelper.EnsureCreated(wavToWemFolderPath);

            _wSourcesWrapper.CreateWsourcesFile(wavFileNames);

            var arguments = $"\"{wprojPath}\" -ConvertExternalSources \"{wsourcesPath}\" -ExternalSourcesOutput \"{audioFolderPath}\"";
            _wSourcesWrapper.RunExternalCommand(arguments);
            _wSourcesWrapper.DeleteExcessStuff();
        }

        public void SaveWemsToPack(List<AudioFile> audioFiles)
        {
            var wemFiles = new List<PackFileUtil.FileRef>();
            foreach (var audioFile in audioFiles)
                wemFiles.Add(new PackFileUtil.FileRef(audioFile.WemDiskFilePath, Path.GetDirectoryName(audioFile.WemPackFilePath)));

            PackFileUtil.LoadFilesFromDisk(_packFileService, wemFiles);
        }

        private void ExportWav(string filePath, byte[] bytes)
        {
            try
            {
                DirectoryHelper.EnsureFileFolderCreated(filePath);
                File.WriteAllBytes(filePath, bytes);
            }
            catch (Exception e)
            {
                _logger.Here().Error(e.Message);
            }
        }
    }
}
