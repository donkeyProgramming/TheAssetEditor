using Audio.Utility;
using CommonControls.Common;
using CommonControls.Services;
using CommunityToolkit.Diagnostics;
using Serilog;
using System.IO;
using System.Linq;

namespace Audio.BnkCompiler
{
    public class AudioFileImporter
    {
        ILogger _logger = Logging.Create<AudioFileImporter>();

        private readonly PackFileService _pfs;
        private readonly VgStreamWrapper _vgStreamWrapper;

        public AudioFileImporter(PackFileService pfs, VgStreamWrapper vgStreamWrapper)
        {
            _pfs = pfs;
            _vgStreamWrapper = vgStreamWrapper;
        }

        public Result<bool> ImportAudio(CompilerData compilerData)
        {
            foreach (var gameSound in compilerData.GameSounds)
            {
                if (string.IsNullOrWhiteSpace(gameSound.SystemFilePath) == false)
                {
                    if (File.Exists(gameSound.SystemFilePath) == false)
                        return Result<bool>.FromError("Audio converter", $"Unable to find file '{gameSound.SystemFilePath}' for item '{gameSound.Name}'");

                    // Convert file
                    var wemPath = _vgStreamWrapper.ConvertToWem(gameSound.SystemFilePath);
                    if(wemPath.Failed)
                        return Result<bool>.FromError(wemPath.LogItems);

                    // Compute hash
                    var fileName = Path.GetFileName(wemPath.Item);
                    var hashName = WWiseHash.Compute30(fileName);

                    // Load
                    var createdFiles = PackFileUtil.LoadFilesFromDisk(_pfs, new PackFileUtil.FileRef(wemPath.Item, $"Audio\\WWise\\{compilerData.ProjectSettings.Language}", $"{hashName}.wem"));
                    gameSound.Path = _pfs.GetFullPath(createdFiles.First());
                }
            }

            // Sanity check
            foreach (var gameSound in compilerData.GameSounds)
            {
                Guard.IsNotNullOrWhiteSpace(gameSound.Path);
                Guard.IsNotNull(_pfs.FindFile(gameSound.Path));
            }

            return Result<bool>.FromOk(true);
        }
        
    }
}
