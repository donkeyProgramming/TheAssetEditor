using Editors.KitbasherEditor.Core.MenuBarViews;
using Editors.KitbasherEditor.Services;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.Ui.Common.MenuSystem;

namespace Editors.KitbasherEditor.UiCommands
{
    public class BrowseForReferenceCommand : ITransientKitbasherUiCommand
    {
        private readonly KitbashSceneCreator _kitbashSceneCreator;
        private readonly IStandardDialogs _packFileUiProvider;

        public string ToolTip { get; set; } = "Import Reference model";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey? HotKey { get; } = null;

        public BrowseForReferenceCommand(KitbashSceneCreator kitbashSceneCreator, IStandardDialogs packFileUiProvider)
        {
            _kitbashSceneCreator = kitbashSceneCreator;
            _packFileUiProvider = packFileUiProvider;
        }

        public void Execute()
        {
            var result = _packFileUiProvider.DisplayBrowseDialog(new List<string>() { ".variantmeshdefinition", ".wsmodel", ".rigid_model_v2" });
            if (result.Result == true && result.File != null)
                _kitbashSceneCreator.LoadReference(result.File);
        }
    }

    public abstract class BaseImportReferenceCommand : ITransientKitbasherUiCommand
    {
        private readonly KitbashSceneCreator _kitbashSceneCreator;
        private readonly IPackFileService _packFileService;

        public string ToolTip { get; set; }
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey HotKey { get; } = null;


        public BaseImportReferenceCommand(KitbashSceneCreator kitbashSceneCreator, IPackFileService packFileService)
        {
            _kitbashSceneCreator = kitbashSceneCreator;
            _packFileService = packFileService;
        }
        protected string _filePath;

        public virtual void Execute()
        {
            var packFile = _packFileService.FindFile(_filePath);
            if (packFile == null)
                throw new Exception($"Unable to load file {_filePath}");

            _kitbashSceneCreator.LoadReference(packFile);
        }
    }

    public class ImportReferenceMeshCommand : IUiCommand
    {
        private readonly KitbashSceneCreator _kitbashSceneCreator;
        private readonly IPackFileService _packFileService;

        public ImportReferenceMeshCommand(KitbashSceneCreator kitbashSceneCreator, IPackFileService packFileService)
        {
            _kitbashSceneCreator = kitbashSceneCreator;
            _packFileService = packFileService;
        }

        public void Execute(string path)
        {
            var packFile = _packFileService.FindFile(path);
            if (packFile == null)
                throw new Exception($"Unable to load file {path}");

            _kitbashSceneCreator.LoadReference(packFile);
        }

        public void Execute(PackFile file)
        {
            _kitbashSceneCreator.LoadReference(file);
        }
    }

    public class ImportGeneralReferenceCommand : BaseImportReferenceCommand
    {
        public ImportGeneralReferenceCommand(KitbashSceneCreator kitbashSceneCreator, IPackFileService packFileService) : base(kitbashSceneCreator, packFileService)
        {
            _filePath = @"variantmeshes\wh_variantmodels\hu1e\cth\cth_celestial_general\cth_celestial_general_body_02.wsmodel";
            ToolTip = "Import General as Reference";
        }
    }

    public class ImportKarlHammerReferenceCommand : BaseImportReferenceCommand
    {
        public ImportKarlHammerReferenceCommand(KitbashSceneCreator kitbashSceneCreator, IPackFileService packFileService) : base(kitbashSceneCreator, packFileService)
        {
            _filePath = @"variantmeshes\wh_variantmodels\hu1\emp\emp_props\emp_karl_franz_hammer_2h_01.wsmodel";
            ToolTip = "Import Hammer as Reference";
        }
    }
}
