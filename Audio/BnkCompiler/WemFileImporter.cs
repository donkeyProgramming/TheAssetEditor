using CommonControls.Common;
using CommonControls.Services;
using Serilog;

namespace Audio.BnkCompiler
{
    public class WemFileImporter
    {
        private readonly PackFileService _pfs;
        ILogger _logger = Logging.Create<WemFileImporter>();

        public WemFileImporter(PackFileService pfs)
        {
            _pfs = pfs;
        }

      
        public Result<bool> ImportAudio(CompilerData compilerData)
        {
            foreach (var item in compilerData.GameSounds)
            { 
                // check if disk => Import/overwrite

            }

            return Result<bool>.FromOk(true);
        }

        bool IsFileOnDisk(string path) => true;
        
    }
}
