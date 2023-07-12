using CommonControls.Common.MenuSystem;
using CommonControls.PackFileBrowser;
using CommonControls.Services;
using KitbasherEditor.Services;
using KitbasherEditor.ViewModels.MenuBarViews;
using System;
using System.Collections.Generic;
using System.IO;

namespace KitbasherEditor.ViewModels.UiCommands
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

    public abstract class ImportReferenceCommand : IKitbasherUiCommand
    {
        private readonly KitbashSceneCreator _kitbashSceneCreator;

        public string ToolTip { get; set; }
        public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
        public Hotkey HotKey { get; } = null;


        public ImportReferenceCommand(KitbashSceneCreator kitbashSceneCreator)
        {
            _kitbashSceneCreator = kitbashSceneCreator;
        }
        protected string _filePath;

        public void Execute() => _kitbashSceneCreator.LoadReference(_filePath);
    }

    public class ImportGoblinReferenceCommand : ImportReferenceCommand
    {
        public ImportGoblinReferenceCommand(KitbashSceneCreator kitbashSceneCreator) : base(kitbashSceneCreator)
        {
            _filePath = @"variantmeshes\variantmeshdefinitions\grn_forest_goblins_base.variantmeshdefinition";
            ToolTip = "Import Goblin as Reference";
        }
    }

    public class ImportSlayerReferenceCommand : ImportReferenceCommand
    {
        public ImportSlayerReferenceCommand(KitbashSceneCreator kitbashSceneCreator) : base(kitbashSceneCreator)
        {
            _filePath = @"variantmeshes\variantmeshdefinitions\dwf_giant_slayers.variantmeshdefinition";
            ToolTip = "Import Slayer as Reference";
        }
    }

    public class ImportPaladinReferenceCommand : ImportReferenceCommand
    {
        public ImportPaladinReferenceCommand(KitbashSceneCreator kitbashSceneCreator) : base(kitbashSceneCreator)
        {
            _filePath = @"variantmeshes\variantmeshdefinitions\brt_paladin.variantmeshdefinition";
            ToolTip = "Import Paladin as Reference";
        }
    }
}
