using System;
using System.Collections.Generic;
using System.Globalization;
using AssetEditor.ViewModels;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Core.ToolCreation;

namespace Shared.Core.ErrorHandling
{
    public class ExtendedExceptionInformation
    {
        // Info about loaded packfile
        public record PackFileContainerInfo(bool IsMainEditable, bool IsCa, string Name, string SystemPath);
        public List<PackFileContainerInfo> ActivePackFiles { get; set; } = [];

        // General info
        public GameTypeEnum CurrentGame { get; set; }

        public ApplicationSettings Settings { get; set; }
        public uint NumberOfOpenEditors { get; set; }
        public uint NumberOfOpenedEditors { get; set; }
        public double RunTimeInSeconds { get; set; }
        public string AssetEditorVersion { get; set; } = "Not set";

        // Exception Info
        public string Context { get; set; } = "Not set";
        public string[] ExceptionMessage { get; set; } = [];
        public string StackTrace { get; set; } = "Not set";
        public string UserComment { get; set; } = "";

        // Contex info
        public string CurrentEditorName { get; set; }
        public string EditorInputFile { get; set; } = "Not set";
        public string EditorInputFileFull { get; set; } = "Not set";
        public string EditorInputFilePack { get; set; } = "Not set";
        public List<string> LogHistory { get; set; } = [];

        // System info
        public string Culture { get; internal set; } = "Not set";
        public string OSVersion { get; internal set; } = "Not set";

        // 
    }

    public class ExtendedExceptionService
    {
        private readonly ApplicationSettingsService _settingsService;
        private readonly PackFileService _pfs;
        private readonly MainViewModel _mainView;

        public ExtendedExceptionService(ApplicationSettingsService service, PackFileService pfs, MainViewModel mainView)
        {
            _settingsService = service;
            _pfs = pfs;
            _mainView = mainView;
        }

        public ExtendedExceptionInformation Create(Exception e)
        {
            var output = new ExtendedExceptionInformation();
            CreatePackFileInfo(output);
            CreateGeneralInfo(output);
            CreateExceptionInfo(output, e, "");
            CreateContext(output);
            CreateSystemInformation(output);
            return output;
        }

        void CreatePackFileInfo(ExtendedExceptionInformation extendedException)
        {
            foreach (var db in _pfs.Database.PackFiles)
            {
                var isMainEditable = _pfs.Database.PackSelectedForEdit == db;
                var info = new ExtendedExceptionInformation.PackFileContainerInfo(isMainEditable, db.IsCaPackFile, db.Name, db.SystemFilePath);
                extendedException.ActivePackFiles.Add(info);
            }
        }

        void CreateGeneralInfo(ExtendedExceptionInformation extendedException)
        {
            extendedException.CurrentGame = _settingsService.CurrentSettings.CurrentGame;
            extendedException.Settings = _settingsService.CurrentSettings;
            extendedException.NumberOfOpenedEditors = ApplicationStateRecorder.GetNumberOfOpenedEditors();
            extendedException.NumberOfOpenEditors = (uint)_mainView.CurrentEditorsList.Count;
            extendedException.RunTimeInSeconds = ApplicationStateRecorder.GetApplicationRunTimeInSec();
            extendedException.AssetEditorVersion = VersionChecker.CurrentVersion;
        }

        void CreateExceptionInfo(ExtendedExceptionInformation extendedException, Exception e, string context)
        {
            extendedException.Context = context;
            extendedException.ExceptionMessage = ExceptionHelper.GetErrorStringArray(e).ToArray();
            extendedException.StackTrace = e.StackTrace;
        }

        void CreateContext(ExtendedExceptionInformation extendedException)
        {
            try
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
            }

            if (Logging.CustomSink != null)
                extendedException.LogHistory = Logging.CustomSink.GetHistory();
        }

        void CreateSystemInformation(ExtendedExceptionInformation extendedException)
        {
            extendedException.Culture = CultureInfo.CurrentCulture.Name;
            extendedException.OSVersion = Environment.OSVersion.VersionString;
        }
    }
}
