using System;
using AssetEditor.ViewModels;
using Shared.Core.ErrorHandling.Exceptions;
using Shared.Core.PackFiles;
using Shared.Core.ToolCreation;

namespace AssetEditor.Services
{
    internal class CurrentEditorExceptionInfoProvider : IExceptionInformationProvider
    {
        private readonly IEditorManager _editorManager;
        private readonly IPackFileService _pfs;

        public CurrentEditorExceptionInfoProvider(IEditorManager editorManager, IPackFileService pfs)
        {
            _editorManager = editorManager;
            _pfs = pfs;
        }

        public void HydrateExcetion(ExceptionInformation extendedException)
        {
            try
            {
                var allEditors = _editorManager.GetAllEditors();
                var currentEditorIndex = _editorManager.GetCurrentEditor();

                extendedException.NumberOfOpenEditors = (uint)allEditors.Count;

                var editorName = "";
                var editorFileInput = "";
                var editorFileFullName = "";
                var editorFilePack = "";
                if (currentEditorIndex != -1 && allEditors.Count != 0)
                {
                    var editor = allEditors[currentEditorIndex];
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
