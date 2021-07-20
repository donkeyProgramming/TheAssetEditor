using CommonControls.PackFileBrowser;
using CommonControls.Services;
using GalaSoft.MvvmLight.CommandWpf;
using KitbasherEditor.Services;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

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
        public ICommand ImportReferenceCommand_PaladinVMD { get; set; }

        public ModelLoaderService ModelLoader { get; set; }

        WindowKeyboard _keyboard = new WindowKeyboard();
        PackFileService _packFileService;
        public MenuBarViewModel(IComponentManager componentManager, PackFileService packFileService, SkeletonAnimationLookUpHelper skeletonHelper)
        {
            _packFileService = packFileService;

            TransformTool = new TransformToolViewModel(componentManager);
            Gizmo = new GizmoModeMenuBarViewModel(TransformTool, componentManager, _commandFactory);
            General = new GeneralMenuBarViewModel(componentManager, _commandFactory);
            Tools = new ToolsMenuBarViewModel(componentManager, _commandFactory, _packFileService, skeletonHelper, _keyboard);

            ImportReferenceCommand = new RelayCommand(ImportReference);
            ImportReferenceCommand_PaladinVMD = new RelayCommand(ImportReference_PaladinVMD);
        }

        public bool OnKeyReleased(Key key, Key systemKey, ModifierKeys modifierKeys)
        {
            _keyboard.SetKeyDown(key, false);
            _keyboard.SetKeyDown(systemKey, false);
            return _commandFactory.TriggerCommand(key, modifierKeys);
        }

        public void OnKeyDown(Key key, Key systemKey, ModifierKeys modifiers)
        {
            _keyboard.SetKeyDown(systemKey, true);
            _keyboard.SetKeyDown(key, true);
        }

        void ImportReference()
        {
            using (var browser = new PackFileBrowserWindow(_packFileService))
            {
                browser.ViewModel.Filter.SetExtentions(new List<string>() { ".variantmeshdefinition", ".wsmodel", ".rigid_model_v2" });
                if (browser.ShowDialog() == true && browser.SelectedFile != null)
                {
                    ModelLoader.LoadReference(browser.SelectedFile);
                }
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        void ImportReference_PaladinVMD()
        {
            ModelLoader.LoadReference(@"variantmeshes\variantmeshdefinitions\brt_paladin.variantmeshdefinition");
        }


    }
}
