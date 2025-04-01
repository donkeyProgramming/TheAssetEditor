using System.IO;
using System.Linq;
using Editors.Audio.AudioEditor.AudioProjectData;
using Editors.Audio.Utility;
using Shared.Core.Misc;
using Shared.Core.PackFiles;

namespace Editors.Audio.AudioProjectCompiler
{
    public class WemGenerator
    {
        private readonly IPackFileService _packFileService;
        private readonly SoundPlayer _soundPlayer;
        private readonly WSourcesWrapper _wSourcesWrapper;

        public WemGenerator(
            IPackFileService packFileService,
            SoundPlayer soundPlayer,
            WSourcesWrapper wSourcesWrapper)
        {
            _packFileService = packFileService;
            _soundPlayer = soundPlayer;
            _wSourcesWrapper = wSourcesWrapper;
        }

        private readonly string _wavToWemFolderPath = $"{DirectoryHelper.Temp}\\WavToWem";
        private readonly string _audioFolderPath = $"{DirectoryHelper.Temp}\\Audio";
        private readonly string _wprojPath = $"{DirectoryHelper.Temp}\\WavToWem\\WavToWemWwiseProject\\WavToWemWwiseProject.wproj";
        private readonly string _wsourcesPath = $"{DirectoryHelper.Temp}\\WavToWem\\wav_to_wem.wsources";

        public void GenerateWems(AudioProject audioProject)
        {
            AudioProjectCompilerHelpers.DeleteAudioFilesInTempAudioFolder();

            var soundsWithUniqueSourceIds = AudioProjectCompilerHelpers.GetAllUniqueSounds(audioProject);
            foreach (var sound in soundsWithUniqueSourceIds)
            {
                var wavFile = _packFileService.FindFile(sound.WavFilePath);
                var wavFileName = $"{sound.SourceID}.wav";
                _soundPlayer.ExportFileToAEFolder(wavFileName, wavFile.DataSource.ReadData());
            }

            _wSourcesWrapper.InitialiseWwiseProject();

            var wavFileNames = soundsWithUniqueSourceIds
                .Select(sound => $"{sound.SourceID}.wav")
                .ToList();

            DirectoryHelper.EnsureCreated(_wavToWemFolderPath);

            _wSourcesWrapper.CreateWsourcesFile(wavFileNames);

            var arguments = $"\"{_wprojPath}\" -ConvertExternalSources \"{_wsourcesPath}\" -ExternalSourcesOutput \"{_audioFolderPath}\"";

            _wSourcesWrapper.RunExternalCommand(arguments);
            _wSourcesWrapper.DeleteExcessStuff();
        }

        public void SaveWemsToPack(AudioProject audioProject)
        {
            var soundsWithUniqueSourceIds = AudioProjectCompilerHelpers.GetAllUniqueSounds(audioProject);
            foreach (var sound in soundsWithUniqueSourceIds)
                PackFileUtil.LoadFileFromDisk(_packFileService, new PackFileUtil.FileRef(sound.WemDiskFilePath, Path.GetDirectoryName(sound.WemFilePath)));
        }
    }
}
