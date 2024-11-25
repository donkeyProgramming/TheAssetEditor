using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Events;
using Shared.Core.Events.Global;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;

namespace Shared.Core.Services
{
    public class TouchedFilesRecorder
    {
        readonly ILogger _logger = Logging.Create<TouchedFilesRecorder>();
        readonly List<(string FilePath, PackFileContainer Container)> _files = new();
        readonly IPackFileService _pfs;
        private readonly IGlobalEventHub _eventHub;
        bool _isStarted = false;

        public TouchedFilesRecorder(IPackFileService pfs, IGlobalEventHub eventHub)
        {
            _pfs = pfs;
            _eventHub = eventHub;
        }

        public void Start()
        {
            if (_isStarted)
                return;

            _pfs.EnableFileLookUpEvents = true;
            _eventHub.Register<PackFileLookUpEvent>(this, Handle);
            _isStarted = true;
        }

        void Handle(PackFileLookUpEvent lookUpEvent) 
        {
            if (lookUpEvent.Found)
                _files.Add((lookUpEvent.FileName, lookUpEvent.Container!));
        }
        public void Print()
        {
            var files = string.Join("\n", _files.Select(x => x.FilePath));
            _logger.Here().Information("\nFiles touched: \n\n" + files);
        }

        public void ExtractFilesToPack(string path)
        {
            var newPack = _pfs.CreateNewPackFileContainer("AutoExtracted", PackFileCAType.MOD);

            foreach (var item in _files)
                _pfs.CopyFileFromOtherPackFile(item.Container, item.FilePath, newPack);

            _pfs.SavePackContainer(newPack, path, false);
        }

        public void Stop()
        {
            if (_isStarted)
            {
                _eventHub.UnRegister(this);
                _pfs.EnableFileLookUpEvents = false;
            }

            _isStarted = false;
            _files.Clear();
        }
    }
}
