using System.Diagnostics;
using System.Linq;
using Audio.BnkCompiler;
using CommonControls.BaseDialogs.ErrorListDialog;
using CommonControls.PackFileBrowser;
using SharedCore.ErrorHandling;
using SharedCore.Misc;
using SharedCore.PackFiles;
using SharedCore.PackFiles.Models;

namespace Audio.Presentation.Compiler
{
    public class CompilerViewModel : NotifyPropertyChangedImpl, IEditorViewModel
    {
        private readonly PackFileService _pfs;
        private readonly CompilerService _compilerService;

        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("Audio Compiler");
        public NotifyAttr<string> ProjectFilePath { get; set; } = new NotifyAttr<string>("audioprojects\\projectsimple.json");
        public NotifyAttr<ErrorListViewModel> ProjectResult { get; set; } = new NotifyAttr<ErrorListViewModel>(new ErrorListViewModel());

        public CompilerViewModel(PackFileService pfs, CompilerService compilerService)
        {
            _pfs = pfs;
            _compilerService = compilerService;

            var audioProjectFiles = pfs.FindAllFilesInDirectory("audioprojects")
                .Where(x => x.Extention.ToLower() == ".json");

            if (audioProjectFiles.Any())
                ProjectFilePath.Value = pfs.GetFullPath(audioProjectFiles.First());
        }

        public void BrowseProjectFileAction()
        {
            using var browser = new PackFileBrowserWindow(_pfs, new string[] { ".json" });
            if (browser.ShowDialog())
                ProjectFilePath.Value = _pfs.GetFullPath(browser.SelectedFile);
        }

        public void CompileProjectAction()
        {
            ProjectResult.Value.ErrorItems.Clear();
            var result = _compilerService.Compile(ProjectFilePath.Value, CompilerSettings.Default());

            ProjectResult.Value.ErrorItems.Add(new ErrorListDataItem() { IsError = !result.IsSuccess, ErrorType = "Result", Description = $"Compile result is '{result.IsSuccess}'" });
            if (result.IsSuccess == false)
                result.LogItems.Errors.ForEach(x => ProjectResult.Value.ErrorItems.Add(x));
        }

        public void DisplayDocumantationAction() => Process.Start(new ProcessStartInfo("cmd", $"/c start https://tw-modding.com/index.php/Audio_modding") { CreateNoWindow = true });


        public void Close() { }
        public bool Save() => true;
        public PackFile MainFile { get; set; }
        public bool HasUnsavedChanges { get; set; } = false;
    }
}
