using System.Diagnostics;
using System.Linq;
using Editors.Audio.BnkCompiler;
using Shared.Core.ErrorHandling;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.ToolCreation;
using Shared.Ui.BaseDialogs.ErrorListDialog;

namespace Editors.Audio.Compiler
{
    public class CompilerViewModel : NotifyPropertyChangedImpl, IEditorInterface
    {
        private readonly IPackFileService _pfs;
        private readonly CompilerService _compilerService;
        private readonly IPackFileUiProvider _packFileUiProvider;

        public string DisplayName { get; set; } = "Audio Compiler";
        public NotifyAttr<string> ProjectFilePath { get; set; } = new NotifyAttr<string>("audioprojects\\projectsimple.json");
        public NotifyAttr<ErrorListViewModel> ProjectResult { get; set; } = new NotifyAttr<ErrorListViewModel>(new ErrorListViewModel());

        public CompilerViewModel(IPackFileService pfs, CompilerService compilerService, IPackFileUiProvider packFileUiProvider)
        {
            _pfs = pfs;
            _compilerService = compilerService;
            _packFileUiProvider = packFileUiProvider;
            var audioProjectFiles = PackFileServiceUtility.FindAllFilesInDirectory(pfs, "audioprojects")
                .Where(x => x.Extention.ToLower() == ".json");

            if (audioProjectFiles.Any())
                ProjectFilePath.Value = pfs.GetFullPath(audioProjectFiles.First());
        }

        public void BrowseProjectFileAction()
        {
            var result = _packFileUiProvider.DisplayBrowseDialog([".json"]);
            if (result.Result)
                ProjectFilePath.Value = _pfs.GetFullPath(result.File);
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
