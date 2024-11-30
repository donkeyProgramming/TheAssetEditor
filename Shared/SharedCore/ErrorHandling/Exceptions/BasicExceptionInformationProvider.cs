using System.Globalization;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Core.Settings;

namespace Shared.Core.ErrorHandling.Exceptions
{
    class BasicExceptionInformationProvider : IExceptionInformationProvider
    {
        private readonly ApplicationSettingsService _settingsService;
        private readonly IPackFileService _pfs;

        public BasicExceptionInformationProvider(ApplicationSettingsService service, IPackFileService pfs)
        {
            _settingsService = service;
            _pfs = pfs;
        }

        public void HydrateExcetion(ExceptionInformation exceptionInformation)
        {
            CreatePackFileInfo(exceptionInformation);
            CreateGeneralInfo(exceptionInformation);
            CreateContext(exceptionInformation);
            CreateSystemInformation(exceptionInformation);
        }


        void CreatePackFileInfo(ExceptionInformation extendedException)
        {
            var packfiles = _pfs.GetAllPackfileContainers();
            foreach (var db in packfiles)
            {
                var isMainEditable = _pfs.GetEditablePack() == db;
                var info = new ExceptionPackFileContainerInfo(isMainEditable, db.IsCaPackFile, db.Name, db.SystemFilePath);
                extendedException.ActivePackFiles.Add(info);
            }
        }

        void CreateGeneralInfo(ExceptionInformation extendedException)
        {
            extendedException.CurrentGame = _settingsService.CurrentSettings.CurrentGame;
            extendedException.Settings = _settingsService.CurrentSettings;
            extendedException.NumberOfOpenedEditors = ApplicationStateRecorder.GetNumberOfOpenedEditors();
            extendedException.RunTimeInSeconds = ApplicationStateRecorder.GetApplicationRunTimeInSec();
            extendedException.AssetEditorVersion = VersionChecker.CurrentVersion;
        }

        void CreateContext(ExceptionInformation extendedException)
        {  
            if (Logging.CustomSink != null)
                extendedException.LogHistory = Logging.CustomSink.GetHistory();
        }

        void CreateSystemInformation(ExceptionInformation extendedException)
        {
            extendedException.Culture = CultureInfo.CurrentCulture.Name;
            extendedException.OSVersion = Environment.OSVersion.VersionString;
        }
    }
}
