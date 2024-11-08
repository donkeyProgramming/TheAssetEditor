using System;
using AssetEditor.ViewModels;
using Shared.Core.ErrorHandling.Exceptions;
using Shared.Core.PackFiles;
using Shared.Core.ToolCreation;

namespace AssetEditor.Services
{
    internal class CurrentEditorExceptionInfoProvider : IExceptionInformationProvider
    {
        private readonly MainViewModel _mainView;
        private readonly PackFileService _pfs;

        public CurrentEditorExceptionInfoProvider( MainViewModel mainView, PackFileService pfs)
        {
            _mainView = mainView;
            _pfs = pfs;
        }

        public void HydrateExcetion(ExceptionInformation extendedException)
        {
            extendedException.NumberOfOpenEditors = (uint)_mainView.CurrentEditorsList.Count;

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
        }
    }
}
