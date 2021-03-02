using CommonControls.PackFileBrowser;
using FileTypes.PackFiles.Services;
using GalaSoft.MvvmLight.CommandWpf;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using View3D.Components.Component.Selection;

namespace KitbasherEditor.ViewModels.MenuBarViews
{

    public class MenuBarViewModel : IKeyboardHandler
    {
        public GizmoModeMenuBarViewModel Gizmo { get; set; }
        public GeneralMenuBarViewModel General { get; set; }
        public ToolsMenuBarViewModel Tools { get; set; }
        public TransformToolViewModel TransformTool { get; set; }

        public ToolbarCommandFactory _commandFactory = new ToolbarCommandFactory();

        public ICommand ImportReferenceCommand { get; set; }
        public ICommand ImportReferenceCommand_OrcTorso { get; set; }
        public ICommand ImportReferenceCommand_PaladinVMD { get; set; }

        public ModelLoader ModelLoader { get; set; }


        PackFileService _packFileService;
        public MenuBarViewModel(IComponentManager componentManager, PackFileService packFileService)
        {
            _packFileService = packFileService;

            TransformTool = new TransformToolViewModel(componentManager);
            Gizmo = new GizmoModeMenuBarViewModel(TransformTool, componentManager, _commandFactory);
            General = new GeneralMenuBarViewModel(componentManager, _commandFactory);
            Tools = new ToolsMenuBarViewModel(componentManager, _commandFactory);

            ImportReferenceCommand = new RelayCommand(ImportReference);
            ImportReferenceCommand_OrcTorso = new RelayCommand(ImportReference_OrcTorso);
            ImportReferenceCommand_PaladinVMD = new RelayCommand(ImportReference_PaladinVMD);
        }

        public bool HandleKeyUp(Key key, ModifierKeys modifierKeys)
        {
            return _commandFactory.TriggerCommand(key, modifierKeys);
        }

        void ImportReference()
        {
          
            var browser = new PackFileBrowserWindow(_packFileService);
            browser.ViewModel.Filter.SetExtentions(new List<string>() { ".variantmeshdefinition", ".wsmodel", ".rigid_model_v2"});
            if (browser.ShowDialog() == true && browser.SelectedFile != null)
            {
                ModelLoader.LoadReference(browser.SelectedFile);
            }
        }

        void ImportReference_OrcTorso()
        { }

        void ImportReference_PaladinVMD()
        {
            ModelLoader.LoadReference(@"variantmeshes\variantmeshdefinitions\brt_paladin.variantmeshdefinition");
        }
    }
}
