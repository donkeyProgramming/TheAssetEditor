using Editors.Audio.Shared.Utilities;
using Shared.Core.Events;
using Shared.Core.Misc;
using Shared.Core.PackFiles;

namespace Editors.Audio.AudioEditor.Commands
{
    public class PlayAudioFileCommand(IPackFileService packFileService, SoundPlayer soundPlyer) : IUiCommand
    {
        private readonly IPackFileService _packFileService = packFileService;
        private readonly SoundPlayer _soundPlayer = soundPlyer;

        public void Execute(string fileName, string filePath)
        {
            var wavFile = _packFileService.FindFile(filePath);

            _soundPlayer.ExportFileToAEFolder(fileName, wavFile.DataSource.ReadData());

            var audioFolderName = $"{DirectoryHelper.Temp}\\Audio";
            var wavFilePath = $"{audioFolderName}\\{fileName}";

            _soundPlayer.PlayWav(wavFilePath);
        }
    }
}
