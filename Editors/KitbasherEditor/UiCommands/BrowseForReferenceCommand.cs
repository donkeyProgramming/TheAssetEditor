using Editors.KitbasherEditor.Services;
using KitbasherEditor.ViewModels.MenuBarViews;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Ui.Common.MenuSystem;

namespace Editors.KitbasherEditor.UiCommands
{
    public class BrowseForReferenceCommand : IKitbasherUiCommand
    {
        private readonly KitbashSceneCreator _kitbashSceneCreator;
        private readonly IPackFileUiProvider _packFileUiProvider;

        public string ToolTip { get; set; } = "Import Reference model";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey HotKey { get; } = null;

        public BrowseForReferenceCommand(KitbashSceneCreator kitbashSceneCreator, IPackFileUiProvider packFileUiProvider)
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

    public abstract class BaseImportReferenceCommand : IKitbasherUiCommand
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

    public class ImportGoblinReferenceCommand : BaseImportReferenceCommand
    {
        public ImportGoblinReferenceCommand(KitbashSceneCreator kitbashSceneCreator, IPackFileService packFileService) : base(kitbashSceneCreator, packFileService)
        {
            _filePath = @"variantmeshes\variantmeshdefinitions\grn_forest_goblins_base.variantmeshdefinition";
            ToolTip = "Import Goblin as Reference";
        }
    }


    public class ImportGeneralHeadReferenceCommand : BaseImportReferenceCommand
    {
        public ImportGeneralHeadReferenceCommand(KitbashSceneCreator kitbashSceneCreator, IPackFileService packFileService) : base(kitbashSceneCreator, packFileService)
        {
            _filePath = @"variantmeshes\wh_variantmodels\hu1e\cth\cth_celestial_general\cth_celestial_general_head_05.wsmodel";
            ToolTip = "Import Goblin as Reference";
        }
    }

    public class ImportSlayerReferenceCommand : BaseImportReferenceCommand
    {
        public ImportSlayerReferenceCommand(KitbashSceneCreator kitbashSceneCreator, IPackFileService packFileService) : base(kitbashSceneCreator, packFileService)
        {
            _filePath = @"variantmeshes\variantmeshdefinitions\dwf_giant_slayers.variantmeshdefinition";
            ToolTip = "Import Slayer as Reference";
        }
    }

    public class ImportPaladinReferenceCommand : BaseImportReferenceCommand
    {
        public ImportPaladinReferenceCommand(KitbashSceneCreator kitbashSceneCreator, IPackFileService packFileService) : base(kitbashSceneCreator, packFileService)
        {
            _filePath = @"variantmeshes\variantmeshdefinitions\brt_paladin.variantmeshdefinition";
            ToolTip = "Import Paladin as Reference";
        }
    }
}
