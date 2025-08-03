using Editors.Audio.AudioEditor.AudioFilesExplorer;
using Editors.Audio.Utility;
using Shared.Core.Events;
using Shared.Core.Misc;
using Shared.Core.PackFiles;

namespace Editors.Audio.AudioEditor.UICommands
{
    public class PlayAudioFileCommand(IPackFileService packFileService, SoundPlayer soundPlyer) : IUiCommand
    {
        private readonly IPackFileService _packFileService = packFileService;
        private readonly SoundPlayer _soundPlayer = soundPlyer;

        public void Execute(AudioFilesTreeNode selectedAudioFile)
        {
            var wavFile = _packFileService.FindFile(selectedAudioFile.FilePath);
            var wavFileName = $"{selectedAudioFile.Name}";

            _soundPlayer.ExportFileToAEFolder(wavFileName, wavFile.DataSource.ReadData());

            var audioFolderName = $"{DirectoryHelper.Temp}\\Audio";
            var wavFilePath = $"{audioFolderName}\\{wavFileName}";

            _soundPlayer.PlayWav(wavFilePath);
        }
    }
}
