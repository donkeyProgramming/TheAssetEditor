using System.Globalization;
using Shared.Core.Services;
using Shared.Core.Settings;

namespace Shared.Core.ErrorHandling.Exceptions
{
    class BasicExceptionInformationProvider : IExceptionInformationProvider
    {
        private readonly ApplicationSettingsService _settingsService;

        public BasicExceptionInformationProvider(ApplicationSettingsService service)
        {
            _settingsService = service;
        }

        public void HydrateExcetion(ExceptionInformation exceptionInformation)
        {
            CreateGeneralInfo(exceptionInformation);
            CreateContext(exceptionInformation);
            CreateSystemInformation(exceptionInformation);
        }


        void CreateGeneralInfo(ExceptionInformation extendedException)
        {
            extendedException.CurrentGame = _settingsService.CurrentSettings.CurrentGame;
            extendedException.Settings = _settingsService.CurrentSettings;
            extendedException.NumberOfOpenedEditors = ApplicationStateRecorder.GetNumberOfOpenedEditors();
            extendedException.RunTimeInSeconds = ApplicationStateRecorder.GetApplicationRunTimeInSec();
            extendedException.AssetEditorVersion = VersionChecker.GetCurrentVersion().ToString();
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
