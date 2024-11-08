using System.Globalization;
using Shared.Core.PackFiles;
using Shared.Core.Services;

namespace Shared.Core.ErrorHandling.Exceptions
{
    class BasicExceptionInformationProvider : IExceptionInformationProvider
    {
        private readonly ApplicationSettingsService _settingsService;
        private readonly PackFileService _pfs;

        public BasicExceptionInformationProvider(ApplicationSettingsService service, PackFileService pfs)
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
            foreach (var db in _pfs.Database.PackFiles)
            {
                var isMainEditable = _pfs.Database.PackSelectedForEdit == db;
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
        {     // extendedException.NumberOfOpenEditors = (uint)_mainView.CurrentEditorsList.Count;
            /*try
            {
                var editorName = "";
                var editorFileInput = "";
                var editorFileFullName = "";
                var editorFilePack = "";
                if (_mainView.SelectedEditorIndex != -1 && _mainView.CurrentEditorsList.Count != 0)
                {
                    var editor = _mainView.CurrentEditorsList[_mainView.SelectedEditorIndex];
                    editorName = editor.GetType().Name;

                    if (editor is IFileEditor fileEditor)
                    {
                        editorFileInput = fileEditor.CurrentFile.Name;
                        editorFileFullName = _pfs.GetFullPath(fileEditor.CurrentFile);
                        editorFilePack = _pfs.GetPackFileContainer(fileEditor.CurrentFile).Name;
                    }

                }
                extendedException.CurrentEditorName = editorName;
                extendedException.EditorInputFile = editorFileInput;
                extendedException.EditorInputFileFull = editorFileFullName;
                extendedException.EditorInputFilePack = editorFilePack;
            }
            catch (Exception e)
            {
                extendedException.CurrentEditorName = "Error - " + e.Message;
            }*/

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
