using Shared.Core.ErrorHandling;
using Shared.Core.Events;
using Shared.Core.Events.Global;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Settings;

namespace Shared.Core.Services
{
    public class TouchedFilesRecorder
    {
        readonly ILogger _logger = Logging.Create<TouchedFilesRecorder>();
        readonly List<(string FilePath, PackFileContainer Container)> _files = new();
        readonly IPackFileService _pfs;
        private readonly IGlobalEventHub _eventHub;
        private readonly ApplicationSettingsService _applicationSettingsService;
        bool _isStarted = false;

        public TouchedFilesRecorder(IPackFileService pfs, IGlobalEventHub eventHub, ApplicationSettingsService applicationSettingsService)
        {
            _pfs = pfs;
            _eventHub = eventHub;
            _applicationSettingsService = applicationSettingsService;
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

            var gameInformation = GameInformationDatabase.GetGameById(_applicationSettingsService.CurrentSettings.CurrentGame);
            _pfs.SavePackContainer(newPack, path, false, gameInformation);
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
