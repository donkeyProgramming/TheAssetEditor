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
        void GenerateWems(List<Sound> sounds);
        void SaveWemsToPack(List<Sound> sounds);
    }

    public class WemGeneratorService(IPackFileService packFileService, WSourcesWrapper wSourcesWrapper) : IWemGeneratorService
    {
        private readonly IPackFileService _packFileService = packFileService;
        private readonly WSourcesWrapper _wSourcesWrapper = wSourcesWrapper;

        private readonly ILogger _logger = Logging.Create<WemGeneratorService>();

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

        public void SaveWemsToPack(List<Sound> sounds)
        {
            var wemFiles = new List<PackFileUtil.FileRef>();
            foreach (var sound in sounds)
                wemFiles.Add(new PackFileUtil.FileRef(sound.WemDiskFilePath, Path.GetDirectoryName(sound.WemPackFilePath)));

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
