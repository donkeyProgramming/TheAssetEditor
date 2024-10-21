using System.Diagnostics;
using System.Linq;
using CommonControls.PackFileBrowser;
using Editors.Audio.BnkCompiler;
using Shared.Core.ErrorHandling;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.ToolCreation;
using Shared.Ui.BaseDialogs.ErrorListDialog;

namespace Editors.Audio.Compiler
{
    public class CompilerViewModel : NotifyPropertyChangedImpl, IEditorViewModel
    {
        private readonly PackFileService _pfs;
        private readonly CompilerService _compilerService;

        public string DisplayName { get; set; } = "Audio Compiler";
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
    }
}
