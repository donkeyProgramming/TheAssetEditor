﻿using CommonControls.PackFileBrowser;
using Editors.KitbasherEditor.Services;
using KitbasherEditor.ViewModels.MenuBarViews;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Ui.Common.MenuSystem;

namespace Editors.KitbasherEditor.UiCommands
{
    public class BrowseForReferenceCommand : IKitbasherUiCommand
    {
        private readonly KitbashSceneCreator _kitbashSceneCreator;
        private readonly PackFileService _packFileService;

        public string ToolTip { get; set; } = "Import Reference model";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey HotKey { get; } = null;

        public BrowseForReferenceCommand(KitbashSceneCreator kitbashSceneCreator, PackFileService packFileService)
        {
            _kitbashSceneCreator = kitbashSceneCreator;
            _packFileService = packFileService;
        }

        public void Execute()
        {
            using (var browser = new PackFileBrowserWindow(_packFileService))
            {
                browser.ViewModel.Filter.SetExtentions(new List<string>() { ".variantmeshdefinition", ".wsmodel", ".rigid_model_v2" });
                if (browser.ShowDialog() == true && browser.SelectedFile != null)
                {
                    _kitbashSceneCreator.LoadReference(browser.SelectedFile);
                }
            }
        }
    }

    public abstract class BaseImportReferenceCommand : IKitbasherUiCommand
    {
        private readonly KitbashSceneCreator _kitbashSceneCreator;
        private readonly PackFileService _packFileService;

        public string ToolTip { get; set; }
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey HotKey { get; } = null;


        public BaseImportReferenceCommand(KitbashSceneCreator kitbashSceneCreator, PackFileService packFileService)
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
        private readonly PackFileService _packFileService;

        public ImportReferenceMeshCommand(KitbashSceneCreator kitbashSceneCreator, PackFileService packFileService)
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
    }

    public class ImportGoblinReferenceCommand : BaseImportReferenceCommand
    {
        public ImportGoblinReferenceCommand(KitbashSceneCreator kitbashSceneCreator, PackFileService packFileService) : base(kitbashSceneCreator, packFileService)
        {
            _filePath = @"variantmeshes\variantmeshdefinitions\grn_forest_goblins_base.variantmeshdefinition";
            ToolTip = "Import Goblin as Reference";
        }
    }


    public class ImportGeneralHeadReferenceCommand : BaseImportReferenceCommand
    {
        public ImportGeneralHeadReferenceCommand(KitbashSceneCreator kitbashSceneCreator, PackFileService packFileService) : base(kitbashSceneCreator, packFileService)
        {
            _filePath = @"variantmeshes\wh_variantmodels\hu1e\cth\cth_celestial_general\cth_celestial_general_head_05.wsmodel";
            ToolTip = "Import Goblin as Reference";
        }
    }

    public class ImportSlayerReferenceCommand : BaseImportReferenceCommand
    {
        public ImportSlayerReferenceCommand(KitbashSceneCreator kitbashSceneCreator, PackFileService packFileService) : base(kitbashSceneCreator, packFileService)
        {
            _filePath = @"variantmeshes\variantmeshdefinitions\dwf_giant_slayers.variantmeshdefinition";
            ToolTip = "Import Slayer as Reference";
        }
    }

    public class ImportPaladinReferenceCommand : BaseImportReferenceCommand
    {
        public ImportPaladinReferenceCommand(KitbashSceneCreator kitbashSceneCreator, PackFileService packFileService) : base(kitbashSceneCreator, packFileService)
        {
            _filePath = @"variantmeshes\variantmeshdefinitions\brt_paladin.variantmeshdefinition";
            ToolTip = "Import Paladin as Reference";
        }
    }
}
