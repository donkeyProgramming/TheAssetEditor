using CommonControls.Common;
using CommonControls.PackFileBrowser;
using CommonControls.Resources;
using CommonControls.Services;
using GalaSoft.MvvmLight.CommandWpf;
using KitbasherEditor.Services;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using View3D.Components.Component;
using View3D.Components.Component.Selection;

namespace KitbasherEditor.ViewModels.MenuBarViews
{

    public class MenuItem
    {
        public string Name { get; set; }
        public ICommand Command { get; set; }
        public ObservableCollection<MenuItem> MenuItems { get; set; } = new ObservableCollection<MenuItem>();
        public string ToolTip { get; set; }
        public string ImagePath { get; set; } = @"pack://application:,,,/CommonControls;component/Resources/Icons/kitbasher/tool_split.png";

        public MenuItem(string name = "", ICommand command = null)
        {
            Name = name;
            Command = command;
        }
    }

    public enum ActionEnabledRule
    { 
        Always,
        OneObjectSelected,
        AtleastOneObjectSelected,
        TwoObjectesSelected,
        TwoOrMoreObjectsSelected,
        FaceSelected,
        VertexSelected,
        AnythingSelected,
        ObjectOrVertexSelected,
        ObjectOrFaceSelected,
        Custom
    }

    public enum ButtonVisabilityRule
    {
        Always,
        ObjectMode,
        FaceMode,
        VertexMode,
    }

    public class MenuAction
    {
        public Hotkey Hotkey { get; set; }
        public ICommand Command { get; set; }
        public NotifyAttr<string> ToolTipAttribute { get; set; } = new NotifyAttr<string>();
        public ActionEnabledRule EnableRule { get; set; }
        public NotifyAttr<bool> IsActionEnabled { get; set; } = new NotifyAttr<bool>(true);

        public void TriggerAction()
        {
            if (ActionTriggeredCallback != null)
                ActionTriggeredCallback();

            _func();
        }

        public Action ActionTriggeredCallback { get; set; }
        Action _func { get; set; }

        public MenuAction(Action function)
        {
            _func = function;
            Command = new RelayCommand(TriggerAction);
        }

        public string ToolTip
        {
            set 
            {
                ToolTipAttribute.Value = value + ToopTipText();
            }
        }

        string ToopTipText()
        {
            return "";
        }
    }

    public class Hotkey
    {
        public ModifierKeys ModifierKeys { get; set; }
        public Key Key { get; set; }

        public Hotkey(Key key, ModifierKeys modifierKeys)
        {
            Key = key;
            ModifierKeys = modifierKeys;
        }
    }

    public class MenuDesc
    {
        public string Name { get; set; }
        public ObservableCollection<MenuDesc> CustomMenu { get; set; } = new ObservableCollection<MenuDesc>();

        public MenuAction Action { get; set; }
        public bool IsSeparator { get; set; } = false;
    }


    public class ButtonDesc
    {
        public NotifyAttr<bool> IsVisible { get; set; } = new NotifyAttr<bool>(true);
        public BitmapImage Image { get; set; }
        public MenuAction Action { get; set; }
        public ButtonVisabilityRule ShowRule { get; set; } = ButtonVisabilityRule.Always;
        public bool IsSeperator { get; set; } = false;

        public ButtonDesc(MenuAction action)
        {
            Action = action;
        }
    }

    public class RadioButtonDesc : ButtonDesc
    {
        public string GroupName { get; set; } = Guid.NewGuid().ToString();
        public NotifyAttr<bool> IsChecked { get; set; } = new NotifyAttr<bool>(false);

        public RadioButtonDesc(MenuAction action, bool isChecked = false) : base(action)
        {
            IsChecked.Value = isChecked;
            action.ActionTriggeredCallback = OnActionTriggered;
        }

        void OnActionTriggered() => IsChecked.Value = true;
    }

    public class ButtonDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate DefaultButtonTemplate { get; set; }
        public DataTemplate RadioButtonTemplate { get; set; }
        public DataTemplate SeperatorTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var button = (ButtonDesc)item;
            if (button.IsSeperator)
                return SeperatorTemplate;
            switch (button)
            {
                case RadioButtonDesc _:
                    return RadioButtonTemplate;
                default: 
                    return DefaultButtonTemplate;
            }
        }
    }


    public enum MenuActionType
    { 
        Save,
        SaveAs,
        GenerateWsModel,
        OpenImportReference,
        ImportReferencePaladin,
        SortModelsByName,

        Undo,
        Gizmo_ScaleUp,
        Gizmo_ScaleDown,
        Gizmo_Arrow,
        Gizmo_Move,
        Gizmo_Rotate,
        Gizmo_Scale,

        SelectObject,
        SelectFace,
        SelectVertex,
        
        ViewOnlySelected,
        FocusSelection,
        ResetCamera,

        DevideToSubmesh,
        DevideToSubmesh_withoutCombining,
        MergeSelectedMeshes,
        DuplicateSelected,
        DeleteSelected,
        ConvertSelectedMeshIntoStaticAtCurrentAnimFrame,

        ReduceMesh10x,
        CreateLod,
        OpenBmiTool,
        OpenSkeletonResharper,
        OpenReRiggingTool,
        OpenPinTool,
        OpenVertexDebuggerTool,

        GrowFaceSelection,
        ConvertFaceToVertexSelection,


    }

    public class MenuBarViewModel : IKeyboardHandler
    {
        public ObservableCollection<MenuItem> MenuItems { get; set; }

        public ObservableCollection<MenuDesc> CustomMenu { get; set; } = new ObservableCollection<MenuDesc>();
        public ObservableCollection<ButtonDesc> CustomButtons { get; set; } = new ObservableCollection<ButtonDesc>();


        public GizmoModeMenuBarViewModel Gizmo { get; set; }
        public GeneralMenuBarViewModel General { get; set; }
        public ToolsMenuBarViewModel Tools { get; set; }
        public TransformToolViewModel TransformTool { get; set; }

        public ToolbarCommandFactory _commandFactory = new ToolbarCommandFactory();


        public ModelLoaderService ModelLoader { get; set; }

        WindowKeyboard _keyboard = new WindowKeyboard();
        PackFileService _packFileService;

        SelectionManager _selectionManager;


        Dictionary<MenuActionType, MenuAction> _actionList = new Dictionary<MenuActionType, MenuAction>();
        Dictionary<ActionEnabledRule, Func<bool>> _actionEnabledRules = new Dictionary<ActionEnabledRule, Func<bool>>();
        Dictionary<ButtonVisabilityRule, Func<bool>> _buttonVisabilityRules = new Dictionary<ButtonVisabilityRule, Func<bool>>();
        public MenuBarViewModel(IComponentManager componentManager, PackFileService packFileService, SkeletonAnimationLookUpHelper skeletonHelper)
        {
            _packFileService = packFileService;

            TransformTool = new TransformToolViewModel(componentManager);
            Gizmo = new GizmoModeMenuBarViewModel(TransformTool, componentManager);
            General = new GeneralMenuBarViewModel(componentManager);
            Tools = new ToolsMenuBarViewModel(componentManager, _commandFactory, _packFileService, skeletonHelper, _keyboard);

            // -----------------------------------
            // Actions
            // -----------------------------------
            _actionList[MenuActionType.Save] = new MenuAction(General.Save ) { EnableRule = ActionEnabledRule.Always, ToolTip = "Save" };
            _actionList[MenuActionType.SaveAs] = new MenuAction(General.SaveAs) { EnableRule = ActionEnabledRule.Always, ToolTip = "Save as" };
            _actionList[MenuActionType.GenerateWsModel] = new MenuAction(General.GenerateWsModel ) { EnableRule = ActionEnabledRule.Always, ToolTip = "Generate ws model" };
            _actionList[MenuActionType.OpenImportReference] = new MenuAction(ImportReference) { EnableRule = ActionEnabledRule.Always, ToolTip = "Import Reference model" };
            _actionList[MenuActionType.ImportReferencePaladin] = new MenuAction(ImportReference_PaladinVMD) { EnableRule = ActionEnabledRule.Always, ToolTip = "Import paladin Reference model" };
            _actionList[MenuActionType.Undo] = new MenuAction(General.Undo) { EnableRule = ActionEnabledRule.Custom, ToolTip = "Undo Last item", Hotkey = new Hotkey(Key.Z, ModifierKeys.Control)};
            _actionList[MenuActionType.SortModelsByName] = new MenuAction(General.SortMeshes) { EnableRule = ActionEnabledRule.Always, ToolTip = "Sort models by name" };
            

            _actionList[MenuActionType.Gizmo_ScaleUp] = new MenuAction(Gizmo.ScaleGizmoUp) { EnableRule = ActionEnabledRule.Always, ToolTip = "Select Gizmo", Hotkey = new Hotkey(Key.Add, ModifierKeys.None) };
            _actionList[MenuActionType.Gizmo_ScaleDown] = new MenuAction(Gizmo.ScaleGizmoDown) { EnableRule = ActionEnabledRule.Always, ToolTip = "Select Gizmo", Hotkey = new Hotkey(Key.Subtract, ModifierKeys.None) };
            _actionList[MenuActionType.Gizmo_Arrow] = new MenuAction(Gizmo.Cursor) { EnableRule = ActionEnabledRule.Always, ToolTip = "Select Gizmo", Hotkey = new Hotkey(Key.Q, ModifierKeys.None) };
            _actionList[MenuActionType.Gizmo_Move] = new MenuAction(Gizmo.Move) { EnableRule = ActionEnabledRule.Always, ToolTip = "Move Gizmo", Hotkey = new Hotkey(Key.W, ModifierKeys.None) };
            _actionList[MenuActionType.Gizmo_Rotate] = new MenuAction( Gizmo.Rotate) { EnableRule = ActionEnabledRule.Always, ToolTip = "Rotate Gizmo", Hotkey = new Hotkey(Key.E, ModifierKeys.None) };
            _actionList[MenuActionType.Gizmo_Scale] = new MenuAction( Gizmo.Scale) { EnableRule = ActionEnabledRule.Always, ToolTip = "Scale Gizmo", Hotkey = new Hotkey(Key.R, ModifierKeys.None) };

            _actionList[MenuActionType.SelectObject] = new MenuAction(() => { Gizmo.UpdateSelectionMode(GeometrySelectionMode.Object); } ) {EnableRule = ActionEnabledRule.Always, ToolTip = "Object Mode", Hotkey = new Hotkey(Key.F1, ModifierKeys.None) };
            _actionList[MenuActionType.SelectFace] = new MenuAction(() => { Gizmo.UpdateSelectionMode(GeometrySelectionMode.Face); }) { EnableRule = ActionEnabledRule.Always, ToolTip = "Face Mode", Hotkey = new Hotkey(Key.F2, ModifierKeys.None) };
            _actionList[MenuActionType.SelectVertex] = new MenuAction(() => { Gizmo.UpdateSelectionMode(GeometrySelectionMode.Vertex); }) {EnableRule = ActionEnabledRule.Always, ToolTip = "Vertex Mode", Hotkey = new Hotkey(Key.F3, ModifierKeys.None) };

            _actionList[MenuActionType.ViewOnlySelected] = new MenuAction(Tools.ToggleShowSelection) { EnableRule = ActionEnabledRule.Always, ToolTip = "View only selected", Hotkey = new Hotkey(Key.Space, ModifierKeys.None) };
            _actionList[MenuActionType.ResetCamera] = new MenuAction(General.ResetCamera) { EnableRule = ActionEnabledRule.Always, ToolTip = "Reset camera", Hotkey = new Hotkey(Key.F4, ModifierKeys.None) };
            _actionList[MenuActionType.FocusSelection] = new MenuAction(General.FocusSelection) { EnableRule = ActionEnabledRule.Always, ToolTip = "Focus camera on selected", Hotkey = new Hotkey(Key.R, ModifierKeys.Control) };

            _actionList[MenuActionType.DevideToSubmesh] = new MenuAction(Tools.DivideSubMesh) { EnableRule = ActionEnabledRule.OneObjectSelected, ToolTip = "Split mesh into logical parts" };
            //_actionList[MenuActionType.DevideToSubmesh_withoutCombining] = new MenuAction(Tools.DivideSubMesh) { EnableRule = ActionEnabledRule.Always, ToolTip = "Split mesh into logical parts without combining" };
            _actionList[MenuActionType.MergeSelectedMeshes] = new MenuAction(Tools.MergeObjects) { EnableRule = ActionEnabledRule.TwoOrMoreObjectsSelected, ToolTip = "Merge selected meshes", Hotkey = new Hotkey(Key.M, ModifierKeys.Control) };
            _actionList[MenuActionType.DuplicateSelected] = new MenuAction(Tools.DubplicateObject) { EnableRule = ActionEnabledRule.ObjectOrFaceSelected, ToolTip = "Duplicate selection", Hotkey = new Hotkey(Key.D, ModifierKeys.Control) };
            _actionList[MenuActionType.DeleteSelected] = new MenuAction(Tools.DeleteObject) { EnableRule = ActionEnabledRule.ObjectOrFaceSelected, ToolTip = "Delete selected", Hotkey = new Hotkey(Key.Delete, ModifierKeys.None) };
            _actionList[MenuActionType.ConvertSelectedMeshIntoStaticAtCurrentAnimFrame] = new MenuAction(Tools.CreateStaticMeshes) { EnableRule = ActionEnabledRule.AtleastOneObjectSelected, ToolTip = "Convert the selected mesh at at the given animation frame into a static mesh" };

            _actionList[MenuActionType.ReduceMesh10x] = new MenuAction(Tools.ReduceMesh) { EnableRule = ActionEnabledRule.AtleastOneObjectSelected, ToolTip = "Reduce the mesh polygon count by 10%" };
            _actionList[MenuActionType.CreateLod] = new MenuAction(Tools.CreateLods) { EnableRule = ActionEnabledRule.Always, ToolTip = "Auto generate lods for models" };
            _actionList[MenuActionType.OpenBmiTool] = new MenuAction(Tools.OpenBmiTool) { EnableRule = ActionEnabledRule.OneObjectSelected, ToolTip = "Open the Bmi tool" };
            _actionList[MenuActionType.OpenSkeletonResharper] = new MenuAction(Tools.OpenSkeletonReshaperTool) { EnableRule = ActionEnabledRule.AtleastOneObjectSelected, ToolTip = "Open the skeleton modeling tool" };
            _actionList[MenuActionType.OpenReRiggingTool] = new MenuAction(Tools.OpenReRiggingTool) { EnableRule = ActionEnabledRule.AtleastOneObjectSelected, ToolTip = "Open the re-rigging tool" };
            _actionList[MenuActionType.OpenPinTool] = new MenuAction(Tools.PinMeshToMesh) { EnableRule = ActionEnabledRule.TwoObjectesSelected, ToolTip = "Open the pin tool" };

            _actionList[MenuActionType.GrowFaceSelection] = new MenuAction(Tools.ExpandFaceSelection) { EnableRule = ActionEnabledRule.FaceSelected, ToolTip = "Grow selection" };
            _actionList[MenuActionType.ConvertFaceToVertexSelection] = new MenuAction(Tools.ConvertFacesToVertex) { EnableRule = ActionEnabledRule.FaceSelected, ToolTip = "Convert selected faces to vertexes" };
            _actionList[MenuActionType.OpenVertexDebuggerTool] = new MenuAction(Tools.ShowVertexDebugInfo) { EnableRule = ActionEnabledRule.ObjectOrVertexSelected, ToolTip = "Open vertex debugger" };


            // -----------------------------------
            // Menu items
            // -----------------------------------
            CustomMenu.Add(new MenuDesc() { Name = "File" });
            CustomMenu.Add(new MenuDesc() { Name = "Debug" });
            CustomMenu.Add(new MenuDesc() { Name = "Tools" });
            CustomMenu.Add(new MenuDesc() { Name = "Rendering" });

            CustomMenu[0].CustomMenu.Add(new MenuDesc() { Name = "Save", Action = _actionList[MenuActionType.Save] });
            CustomMenu[0].CustomMenu.Add(new MenuDesc() { Name = "Save As", Action = _actionList[MenuActionType.SaveAs] });
            CustomMenu[0].CustomMenu.Add(new MenuDesc() { IsSeparator = true }); 
            CustomMenu[0].CustomMenu.Add(new MenuDesc() { Name = "Import Reference model", Action = _actionList[MenuActionType.OpenImportReference] });

            CustomMenu[1].CustomMenu.Add(new MenuDesc() { Name = "Import Paladin", Action = _actionList[MenuActionType.ImportReferencePaladin] });

            CustomMenu[2].CustomMenu.Add(new MenuDesc() { Name = "Sort models by name", Action = _actionList[MenuActionType.SortModelsByName] });
            CustomMenu[2].CustomMenu.Add(new MenuDesc() { Name = "Generat Ws Model", Action = _actionList[MenuActionType.GenerateWsModel] });

            CustomMenu[3].CustomMenu.Add(new MenuDesc() { Name = "Focus camera", Action = _actionList[MenuActionType.FocusSelection] });
            CustomMenu[3].CustomMenu.Add(new MenuDesc() { Name = "Reset camera", Action = _actionList[MenuActionType.ResetCamera] });

            // -----------------------------------
            // Button items
            // -----------------------------------

            CustomButtons.Add(new ButtonDesc(_actionList[MenuActionType.Save]) { Image = ResourceController.SaveFileIcon});
            CustomButtons.Add(new ButtonDesc(_actionList[MenuActionType.OpenImportReference]) { Image = ResourceController.OpenReferenceMeshIcon});
            CustomButtons.Add(new ButtonDesc(_actionList[MenuActionType.Undo]) { Image = ResourceController.UndoIcon });

            CustomButtons.Add(new ButtonDesc(null) { IsSeperator = true });

            CustomButtons.Add(new RadioButtonDesc(_actionList[MenuActionType.Gizmo_Arrow], true) { GroupName = "Gizmo", Image = ResourceController.Gizmo_CursorIcon });
            CustomButtons.Add(new RadioButtonDesc(_actionList[MenuActionType.Gizmo_Move]) { GroupName = "Gizmo", Image = ResourceController.Gizmo_MoveIcon });
            CustomButtons.Add(new RadioButtonDesc(_actionList[MenuActionType.Gizmo_Rotate]) { GroupName = "Gizmo", Image = ResourceController.Gizmo_ScaleIcon });
            CustomButtons.Add(new RadioButtonDesc(_actionList[MenuActionType.Gizmo_Scale]) { GroupName = "Gizmo", Image = ResourceController.Gizmo_RotateIcon });

            CustomButtons.Add(new ButtonDesc(null) { IsSeperator = true });

            CustomButtons.Add(new RadioButtonDesc(_actionList[MenuActionType.SelectObject], true) { GroupName = "SelectionMode", Image = ResourceController.Selection_Object_Icon  });
            CustomButtons.Add(new RadioButtonDesc(_actionList[MenuActionType.SelectFace]) { GroupName = "SelectionMode", Image = ResourceController.Selection_Face_Icon });
            CustomButtons.Add(new RadioButtonDesc(_actionList[MenuActionType.SelectVertex]) { GroupName = "SelectionMode", Image = ResourceController.Selection_Vertex_Icon  });

            CustomButtons.Add(new ButtonDesc(_actionList[MenuActionType.ViewOnlySelected]) { Image = ResourceController.ViewSelectedIcon});

            CustomButtons.Add(new ButtonDesc(null) { IsSeperator = true });

            CustomButtons.Add(new ButtonDesc(_actionList[MenuActionType.DevideToSubmesh]) { ShowRule = ButtonVisabilityRule.ObjectMode,  Image = ResourceController.DivideIntoSubMeshIcon });
            CustomButtons.Add(new ButtonDesc(_actionList[MenuActionType.MergeSelectedMeshes]) { ShowRule = ButtonVisabilityRule.ObjectMode, Image = ResourceController.MergeMeshIcon });
            CustomButtons.Add(new ButtonDesc(_actionList[MenuActionType.DuplicateSelected]) { ShowRule = ButtonVisabilityRule.ObjectMode, Image = ResourceController.DuplicateIcon });
            CustomButtons.Add(new ButtonDesc(_actionList[MenuActionType.DeleteSelected]) { ShowRule = ButtonVisabilityRule.ObjectMode, Image = ResourceController.DeleteIcon });
            CustomButtons.Add(new ButtonDesc(_actionList[MenuActionType.ConvertSelectedMeshIntoStaticAtCurrentAnimFrame]) { ShowRule = ButtonVisabilityRule.ObjectMode, Image = ResourceController.FreezeAnimationIcon });

            CustomButtons.Add(new ButtonDesc(null) { IsSeperator = true });


            CustomButtons.Add(new ButtonDesc(_actionList[MenuActionType.ReduceMesh10x]) { ShowRule = ButtonVisabilityRule.ObjectMode, Image = ResourceController.ReduceMeshIcon });
            CustomButtons.Add(new ButtonDesc(_actionList[MenuActionType.CreateLod]) { ShowRule = ButtonVisabilityRule.ObjectMode, Image = ResourceController.CreateLodIcon });
            CustomButtons.Add(new ButtonDesc(_actionList[MenuActionType.OpenBmiTool]) { ShowRule = ButtonVisabilityRule.ObjectMode, Image = ResourceController.BmiToolIcon });
            CustomButtons.Add(new ButtonDesc(_actionList[MenuActionType.OpenSkeletonResharper]) { ShowRule = ButtonVisabilityRule.ObjectMode, Image = ResourceController.SkeletonReshaperIcon });
            CustomButtons.Add(new ButtonDesc(_actionList[MenuActionType.OpenReRiggingTool]) { ShowRule = ButtonVisabilityRule.ObjectMode, Image = ResourceController.ReRiggingIcon });
            CustomButtons.Add(new ButtonDesc(_actionList[MenuActionType.OpenPinTool]) { ShowRule = ButtonVisabilityRule.ObjectMode, Image = ResourceController.PinIcon });


            // Face tools
            CustomButtons.Add(new ButtonDesc(_actionList[MenuActionType.ConvertFaceToVertexSelection]) { ShowRule = ButtonVisabilityRule.FaceMode, Image = ResourceController.FaceToVertexIcon });
            CustomButtons.Add(new ButtonDesc(_actionList[MenuActionType.GrowFaceSelection]) { ShowRule = ButtonVisabilityRule.FaceMode, Image = ResourceController.GrowSelectionIcon });
            CustomButtons.Add(new ButtonDesc(_actionList[MenuActionType.DevideToSubmesh]) { ShowRule = ButtonVisabilityRule.FaceMode, Image = ResourceController.DivideIntoSubMeshIcon });
            CustomButtons.Add(new ButtonDesc(_actionList[MenuActionType.DuplicateSelected]) { ShowRule = ButtonVisabilityRule.FaceMode, Image = ResourceController.DuplicateIcon });
            CustomButtons.Add(new ButtonDesc(_actionList[MenuActionType.DeleteSelected]) { ShowRule = ButtonVisabilityRule.FaceMode, Image = ResourceController.DeleteIcon });


            // Vertex tools
            CustomButtons.Add(new ButtonDesc(_actionList[MenuActionType.OpenVertexDebuggerTool]) { ShowRule = ButtonVisabilityRule.VertexMode, Image = ResourceController.VertexDebuggerIcon });
            
            RegisterHotkeys();

            _selectionManager = componentManager.GetComponent<SelectionManager>();
            _selectionManager.SelectionChanged += OnSelectionChanged;

            _commandExecutor = componentManager.GetComponent<CommandExecutor>();
            _commandExecutor.CommandStackChanged += OnUndoStackChanged;


            _buttonVisabilityRules[ButtonVisabilityRule.Always] = AllwaysTrueRule;
            _buttonVisabilityRules[ButtonVisabilityRule.ObjectMode] = IsObjectMode;
            _buttonVisabilityRules[ButtonVisabilityRule.FaceMode] = IsFaceMode;
            _buttonVisabilityRules[ButtonVisabilityRule.VertexMode] = IsVertexMode;

            _actionEnabledRules[ActionEnabledRule.Always] = AllwaysTrueRule;
            _actionEnabledRules[ActionEnabledRule.OneObjectSelected] = OneObjectSelectedRule;
            _actionEnabledRules[ActionEnabledRule.AtleastOneObjectSelected] = AtleastOneObjectSelectedRule;
            _actionEnabledRules[ActionEnabledRule.TwoOrMoreObjectsSelected] = TwoOrMoreObjectsSelectedRule;
            _actionEnabledRules[ActionEnabledRule.TwoObjectesSelected] = TwoObjectSelectedRule;

            _actionEnabledRules[ActionEnabledRule.FaceSelected] = FaceSelectedRule;
            _actionEnabledRules[ActionEnabledRule.VertexSelected] = VertexSelectedRule;
            _actionEnabledRules[ActionEnabledRule.AnythingSelected] = AnythingSelectedRule;
            _actionEnabledRules[ActionEnabledRule.ObjectOrVertexSelected] = ObjectOrVertexSelectedRule;
            _actionEnabledRules[ActionEnabledRule.ObjectOrFaceSelected] = ObjectOrFaceSelectedReule;
           
            
        }





        bool IsObjectMode() => _selectionManager.GetState().Mode == GeometrySelectionMode.Object;
        bool IsFaceMode() => _selectionManager.GetState().Mode == GeometrySelectionMode.Face;
        bool IsVertexMode() => _selectionManager.GetState().Mode == GeometrySelectionMode.Vertex;
        bool AllwaysTrueRule() => true;

        bool OneObjectSelectedRule() => IsObjectMode() && _selectionManager.GetState().SelectionCount() == 1;
        bool TwoObjectSelectedRule() => IsObjectMode() && _selectionManager.GetState().SelectionCount() == 1;
        bool AtleastOneObjectSelectedRule() => IsObjectMode() && _selectionManager.GetState().SelectionCount() >= 1;
        bool TwoOrMoreObjectsSelectedRule() => IsObjectMode() && _selectionManager.GetState().SelectionCount() >= 2;
        bool AnythingSelectedRule() => _selectionManager.GetState().SelectionCount() >= 1;
        bool ObjectOrFaceSelectedReule() => (IsObjectMode() || IsFaceMode()) && _selectionManager.GetState().SelectionCount() >= 1;
        bool ObjectOrVertexSelectedRule() => (IsObjectMode() || IsVertexMode()) && _selectionManager.GetState().SelectionCount() >= 1;

        bool FaceSelectedRule() =>  IsFaceMode() && _selectionManager.GetState().SelectionCount() >= 1;
        bool VertexSelectedRule() => IsVertexMode() && _selectionManager.GetState().SelectionCount() >= 1;


        CommandExecutor _commandExecutor;
        private void OnUndoStackChanged()
        {
            _actionList[MenuActionType.Undo].ToolTip = _commandExecutor.GetUndoHint();
            _actionList[MenuActionType.Undo].IsActionEnabled.Value = _commandExecutor.CanUndo();
        }

        // Create Object tools
        // Create face tools
        // Create vertex tools

        void RegisterHotkeys()
        {
            var actionList = _actionList
                .Where(x => x.Value.Hotkey != null)
                .Select(x => x.Value);

            foreach (var item in actionList)
                _commandFactory.Register(item);
        }




        // Update undo action tool tip and state

        private void OnSelectionChanged(ISelectionState state)
        {
            if (state.Mode == GeometrySelectionMode.Object)
                _actionList[MenuActionType.SelectObject].TriggerAction();
            else if (state.Mode == GeometrySelectionMode.Face)
                _actionList[MenuActionType.SelectFace].TriggerAction();
            else if (state.Mode == GeometrySelectionMode.Vertex)
                _actionList[MenuActionType.SelectVertex].TriggerAction();
            else
                throw new NotImplementedException("Unkown state");

            // Validate if tool button is visible
            foreach (var button in CustomButtons)
            {
                var rule = button.ShowRule;
                button.IsVisible.Value = _buttonVisabilityRules[rule].Invoke();
            }


            // Validate if menu action is enabled
            foreach (var action in _actionList.Values)
            {
                var rule = action.EnableRule;
                if (rule == ActionEnabledRule.Custom)
                    continue;
                action.IsActionEnabled.Value = _actionEnabledRules[rule].Invoke();
            }
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
