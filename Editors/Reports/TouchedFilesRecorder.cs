using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;

namespace Editors.Reports
{
    public class TouchedFilesRecorder
    {
        readonly ILogger _logger = Logging.Create<TouchedFilesRecorder>();
        readonly List<(string FilePath, PackFileContainer Container)> _files = new();
        readonly PackFileService _pfs;
        bool _isStarted = false;

        public TouchedFilesRecorder(PackFileService pfs)
        {
            _pfs = pfs;
        }

        private void LookUpEventHandler(string fileName, PackFileContainer? container, bool found)
        {
            if (found)
                _files.Add((fileName, container!));
        }

        public void Start()
        {
            if(_isStarted)
                return;
            _pfs.FileLookUpEvent += LookUpEventHandler;
            _isStarted = true;
        }

        public void Print()
        {
            var files = string.Join("\n", _files.Select(x => x.FilePath));
            _logger.Here().Information("\nFiles touched: \n\n" + files);
        }

        public void ExtractFilesToPack(string path)
        {
            var newPack = _pfs.CreateNewPackFileContainer("AutoExtracted", PackFileCAType.MOD);

            foreach(var item in _files)
                _pfs.CopyFileFromOtherPackFile(item.Container, item.FilePath, newPack);

            _pfs.Save(newPack, path, false);
        }

        public void Stop()
        {
            if(_isStarted)
                _pfs.FileLookUpEvent -= LookUpEventHandler;
            _isStarted = false;

        }
    }
}
