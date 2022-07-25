using CommonControls.Common;
using CommonControls.Common.MenuSystem;
using CommonControls.PackFileBrowser;
using CommonControls.Resources;
using CommonControls.Services;
using CommunityToolkit.Mvvm.Input;
using KitbasherEditor.Services;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using View3D.Components.Component;
using View3D.Components.Component.Selection;

namespace KitbasherEditor.ViewModels.MenuBarViews
{
    public enum MenuActionType
    { 
        Save,
        SaveAs,
        GenerateWsModelForWh3,
        GenerateWsModelForWh2,
        OpenImportReference,
        ImportReferencePaladin,
        ImportReferenceSlayer,
        ImportReferenceGoblin,
        ImportMapForDebug,
        DeleteLods,
        ClearConsole,
        SortModelsByName,

        Group,
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
        ToogleBackFaceRendering,
        ToggleLargeSceneRendering,

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
        CopyLod0ToEveryLodSlot,
    }

    public class MenuBarViewModel : IKeyboardHandler
    {
        public ObservableCollection<ToolbarItem> MenuItems { get; set; } = new ObservableCollection<ToolbarItem>();
        public ObservableCollection<MenuBarButton> CustomButtons { get; set; } = new ObservableCollection<MenuBarButton>();

        public KitbashSceneCreator ModelLoader { get; set; }
        public GizmoActions Gizmo { get; set; }
        public GeneralActions General { get; set; }
        public ToolActions Tools { get; set; }
        public TransformToolViewModel TransformTool { get; set; }

        IComponentManager _componentManager;
        ActionHotkeyHandler _commandFactory = new ActionHotkeyHandler();
        VisibilityHandler _ruleFactory;
        WindowKeyboard _keyboard = new WindowKeyboard();
        PackFileService _packFileService;
        SelectionManager _selectionManager;
        CommandExecutor _commandExecutor;

        Dictionary<MenuActionType, MenuAction> _actionList = new Dictionary<MenuActionType, MenuAction>();

        public MenuBarViewModel(IComponentManager componentManager, PackFileService packFileService)
        {
            _componentManager = componentManager;
            _packFileService = packFileService;


            _ruleFactory = new VisibilityHandler(componentManager);
            TransformTool = new TransformToolViewModel(componentManager);
            Gizmo = new GizmoActions(TransformTool, componentManager);
            General = new GeneralActions(componentManager);
            Tools = new ToolActions(componentManager, _packFileService, _keyboard);

            CreateActions();
            CreateButtons();
            CreateMenu();
            ProcessHotkeys();

            _selectionManager = componentManager.GetComponent<SelectionManager>();
            _selectionManager.SelectionChanged += OnSelectionChanged;

            _commandExecutor = componentManager.GetComponent<CommandExecutor>();
            _commandExecutor.CommandStackChanged += OnUndoStackChanged;
        }

        void CreateActions()
        {
            _actionList[MenuActionType.Save] = new MenuAction(General.Save) { EnableRule = ActionEnabledRule.Always, ToolTip = "Save" };
            _actionList[MenuActionType.SaveAs] = new MenuAction(General.SaveAs) { EnableRule = ActionEnabledRule.Always, ToolTip = "Save as" };
            _actionList[MenuActionType.GenerateWsModelForWh3] = new MenuAction(General.GenerateWsModelWh3) { EnableRule = ActionEnabledRule.Always, ToolTip = "Generate ws model (Wh3)" };
            _actionList[MenuActionType.GenerateWsModelForWh2] = new MenuAction(General.GenerateWsModelForWh2) { EnableRule = ActionEnabledRule.Always, ToolTip = "Generate ws model (Wh2)" };
            _actionList[MenuActionType.OpenImportReference] = new MenuAction(ImportReference) { EnableRule = ActionEnabledRule.Always, ToolTip = "Import Reference model" };
            _actionList[MenuActionType.ImportReferencePaladin] = new MenuAction(ImportReference_PaladinVMD) { EnableRule = ActionEnabledRule.Always, ToolTip = "Import Paladin Reference model" };
            _actionList[MenuActionType.ImportReferenceSlayer] = new MenuAction(ImportReference_Slayer) { EnableRule = ActionEnabledRule.Always, ToolTip = "Import Slayer Reference model" };
            _actionList[MenuActionType.ImportReferenceGoblin] = new MenuAction(ImportReference_Goblin) { EnableRule = ActionEnabledRule.Always, ToolTip = "Import Goblin Reference model" };
            _actionList[MenuActionType.ImportMapForDebug] = new MenuAction(ImportDebugMap) { EnableRule = ActionEnabledRule.Always, ToolTip = "Import map for debug" };
            _actionList[MenuActionType.DeleteLods] = new MenuAction(General.DeleteLods) { EnableRule = ActionEnabledRule.Always, ToolTip = "Delete all but first lod" };
            _actionList[MenuActionType.ClearConsole] = new MenuAction(ClearConsole) { EnableRule = ActionEnabledRule.Always, ToolTip = "Clear the debug console window" };
            _actionList[MenuActionType.Undo] = new MenuAction(General.Undo) { EnableRule = ActionEnabledRule.Custom, ToolTip = "Undo Last item", Hotkey = new Hotkey(Key.Z, ModifierKeys.Control) };
            _actionList[MenuActionType.SortModelsByName] = new MenuAction(General.SortMeshes) { EnableRule = ActionEnabledRule.Always, ToolTip = "Sort models by name" };

            _actionList[MenuActionType.Group] = new MenuAction(Tools.GroupItems) { EnableRule = ActionEnabledRule.AtleastOneObjectSelected, ToolTip = "(Un)Group", Hotkey = new Hotkey(Key.G, ModifierKeys.Control) };
            _actionList[MenuActionType.Gizmo_ScaleUp] = new MenuAction(Gizmo.ScaleGizmoUp) { EnableRule = ActionEnabledRule.Always, ToolTip = "Select Gizmo", Hotkey = new Hotkey(Key.Add, ModifierKeys.None) };
            _actionList[MenuActionType.Gizmo_ScaleDown] = new MenuAction(Gizmo.ScaleGizmoDown) { EnableRule = ActionEnabledRule.Always, ToolTip = "Select Gizmo", Hotkey = new Hotkey(Key.Subtract, ModifierKeys.None) };
            _actionList[MenuActionType.Gizmo_Arrow] = new MenuAction(Gizmo.Cursor) { EnableRule = ActionEnabledRule.Always, ToolTip = "Select Gizmo", Hotkey = new Hotkey(Key.Q, ModifierKeys.None) };
            _actionList[MenuActionType.Gizmo_Move] = new MenuAction(Gizmo.Move) { EnableRule = ActionEnabledRule.Always, ToolTip = "Move Gizmo", Hotkey = new Hotkey(Key.W, ModifierKeys.None) };
            _actionList[MenuActionType.Gizmo_Rotate] = new MenuAction(Gizmo.Rotate) { EnableRule = ActionEnabledRule.Always, ToolTip = "Rotate Gizmo", Hotkey = new Hotkey(Key.E, ModifierKeys.None) };
            _actionList[MenuActionType.Gizmo_Scale] = new MenuAction(Gizmo.Scale) { EnableRule = ActionEnabledRule.Always, ToolTip = "Scale Gizmo", Hotkey = new Hotkey(Key.R, ModifierKeys.None) };

            _actionList[MenuActionType.SelectObject] = new MenuAction(() => { Gizmo.UpdateSelectionMode(GeometrySelectionMode.Object); }) { EnableRule = ActionEnabledRule.Always, ToolTip = "Object Mode", Hotkey = new Hotkey(Key.F1, ModifierKeys.None) };
            _actionList[MenuActionType.SelectFace] = new MenuAction(() => { Gizmo.UpdateSelectionMode(GeometrySelectionMode.Face); }) { EnableRule = ActionEnabledRule.Always, ToolTip = "Face Mode", Hotkey = new Hotkey(Key.F2, ModifierKeys.None) };
            _actionList[MenuActionType.SelectVertex] = new MenuAction(() => { Gizmo.UpdateSelectionMode(GeometrySelectionMode.Vertex); }) { EnableRule = ActionEnabledRule.Always, ToolTip = "Vertex Mode", Hotkey = new Hotkey(Key.F3, ModifierKeys.None) };

            _actionList[MenuActionType.ViewOnlySelected] = new MenuAction(Tools.ToggleShowSelection) { EnableRule = ActionEnabledRule.Always, ToolTip = "View only selected", Hotkey = new Hotkey(Key.Space, ModifierKeys.None) };
            _actionList[MenuActionType.ResetCamera] = new MenuAction(General.ResetCamera) { EnableRule = ActionEnabledRule.Always, ToolTip = "Reset camera", Hotkey = new Hotkey(Key.F4, ModifierKeys.None) };
            _actionList[MenuActionType.FocusSelection] = new MenuAction(General.FocusSelection) { EnableRule = ActionEnabledRule.Always, ToolTip = "Focus camera on selected", Hotkey = new Hotkey(Key.F, ModifierKeys.Control) };
            _actionList[MenuActionType.ToogleBackFaceRendering] = new MenuAction(General.ToggleBackFaceRendering) { EnableRule = ActionEnabledRule.Always, ToolTip = "Toggle backface rendering" };
            _actionList[MenuActionType.ToggleLargeSceneRendering] = new MenuAction(General.ToggleLargeSceneRendering) { EnableRule = ActionEnabledRule.Always, ToolTip = "Toogle rendering of large scenes" };

            _actionList[MenuActionType.DevideToSubmesh] = new MenuAction(Tools.DivideSubMesh) { EnableRule = ActionEnabledRule.OneObjectSelected, ToolTip = "Split mesh into logical parts" };
            //_actionList[MenuActionType.DevideToSubmesh_withoutCombining] = new MenuAction(Tools.DivideSubMesh) { EnableRule = ActionEnabledRule.Always, ToolTip = "Split mesh into logical parts without combining" };
            _actionList[MenuActionType.MergeSelectedMeshes] = new MenuAction(Tools.MergeObjects) { EnableRule = ActionEnabledRule.TwoOrMoreObjectsSelected, ToolTip = "Merge selected meshes", Hotkey = new Hotkey(Key.M, ModifierKeys.Control) };
            _actionList[MenuActionType.DuplicateSelected] = new MenuAction(Tools.DubplicateObject) { EnableRule = ActionEnabledRule.ObjectOrFaceSelected, ToolTip = "Duplicate selection", Hotkey = new Hotkey(Key.D, ModifierKeys.Control) };
            _actionList[MenuActionType.DeleteSelected] = new MenuAction(Tools.DeleteObject) { EnableRule = ActionEnabledRule.ObjectOrFaceSelected, ToolTip = "Delete selected", Hotkey = new Hotkey(Key.Delete, ModifierKeys.None) };
            _actionList[MenuActionType.ConvertSelectedMeshIntoStaticAtCurrentAnimFrame] = new MenuAction(Tools.CreateStaticMeshes) { EnableRule = ActionEnabledRule.AtleastOneObjectSelected, ToolTip = "Convert the selected mesh at at the given animation frame into a static mesh" };

            // Group items missing -> Only menu 
            _actionList[MenuActionType.ReduceMesh10x] = new MenuAction(Tools.ReduceMesh) { EnableRule = ActionEnabledRule.AtleastOneObjectSelected, ToolTip = "Reduce the mesh polygon count by 10%" };
            _actionList[MenuActionType.CreateLod] = new MenuAction(Tools.CreateLods) { EnableRule = ActionEnabledRule.Always, ToolTip = "Auto generate lods for models" };
            _actionList[MenuActionType.OpenBmiTool] = new MenuAction(Tools.OpenBmiTool) { EnableRule = ActionEnabledRule.OneObjectSelected, ToolTip = "Open the Bmi tool" };
            _actionList[MenuActionType.OpenSkeletonResharper] = new MenuAction(Tools.OpenSkeletonReshaperTool) { EnableRule = ActionEnabledRule.AtleastOneObjectSelected, ToolTip = "Open the skeleton modeling tool" };
            _actionList[MenuActionType.OpenReRiggingTool] = new MenuAction(Tools.OpenReRiggingTool) { EnableRule = ActionEnabledRule.AtleastOneObjectSelected, ToolTip = "Open the re-rigging tool" };
            _actionList[MenuActionType.OpenPinTool] = new MenuAction(Tools.PinMeshToMesh) { EnableRule = ActionEnabledRule.Always, ToolTip = "Open the pin tool" };

            _actionList[MenuActionType.GrowFaceSelection] = new MenuAction(Tools.ExpandFaceSelection) { EnableRule = ActionEnabledRule.FaceSelected, ToolTip = "Grow selection" };
            _actionList[MenuActionType.ConvertFaceToVertexSelection] = new MenuAction(Tools.ConvertFacesToVertex) { EnableRule = ActionEnabledRule.FaceSelected, ToolTip = "Convert selected faces to vertexes" };
            _actionList[MenuActionType.OpenVertexDebuggerTool] = new MenuAction(Tools.ShowVertexDebugInfo) { EnableRule = ActionEnabledRule.ObjectOrVertexSelected, ToolTip = "Open vertex debugger" };

            _actionList[MenuActionType.CopyLod0ToEveryLodSlot] = new MenuAction(Tools.CopyLod0ToEveryLods) { EnableRule = ActionEnabledRule.Always, ToolTip = "Copy LOD 0 to every LOD slot" };
        }

        void CreateMenu()
        {
            MenuItems.Add(new ToolbarItem() { Name = "File" });
            MenuItems.Add(new ToolbarItem() { Name = "Debug" });
            MenuItems.Add(new ToolbarItem() { Name = "Tools" });
            MenuItems.Add(new ToolbarItem() { Name = "Rendering" });

            MenuItems[0].Children.Add(new ToolbarItem() { Name = "Save", Action = _actionList[MenuActionType.Save] });
            MenuItems[0].Children.Add(new ToolbarItem() { Name = "Save As", Action = _actionList[MenuActionType.SaveAs] });
            MenuItems[0].Children.Add(new ToolbarItem() { IsSeparator = true });
            MenuItems[0].Children.Add(new ToolbarItem() { Name = "Import Reference model", Action = _actionList[MenuActionType.OpenImportReference] });

            MenuItems[1].Children.Add(new ToolbarItem() { Name = "Import Paladin", Action = _actionList[MenuActionType.ImportReferencePaladin] });
            MenuItems[1].Children.Add(new ToolbarItem() { Name = "Import Slayer", Action = _actionList[MenuActionType.ImportReferenceSlayer] });
            MenuItems[1].Children.Add(new ToolbarItem() { Name = "Import Goblin", Action = _actionList[MenuActionType.ImportReferenceGoblin] });
            MenuItems[1].Children.Add(new ToolbarItem() { Name = "Import map", Action = _actionList[MenuActionType.ImportMapForDebug] });
            MenuItems[1].Children.Add(new ToolbarItem() { Name = "Delete lods", Action = _actionList[MenuActionType.DeleteLods] });
            MenuItems[1].Children.Add(new ToolbarItem() { Name = "Clear console", Action = _actionList[MenuActionType.ClearConsole] });

            MenuItems[2].Children.Add(new ToolbarItem() { Name = "(Un)Group selection", Action = _actionList[MenuActionType.Group] });
            MenuItems[2].Children.Add(new ToolbarItem() { Name = "Reduce mesh by 10%", Action = _actionList[MenuActionType.ReduceMesh10x] });
            MenuItems[2].Children.Add(new ToolbarItem() { Name = "Sort models by name", Action = _actionList[MenuActionType.SortModelsByName] });
            MenuItems[2].Children.Add(new ToolbarItem() { IsSeparator = true });
            MenuItems[2].Children.Add(new ToolbarItem() { Name = "Generat Ws Model (Wh3)", Action = _actionList[MenuActionType.GenerateWsModelForWh3] });
            MenuItems[2].Children.Add(new ToolbarItem() { Name = "Generat Ws Model (Wh2)", Action = _actionList[MenuActionType.GenerateWsModelForWh2] });
            MenuItems[2].Children.Add(new ToolbarItem() { IsSeparator = true });
            MenuItems[2].Children.Add(new ToolbarItem() { Name = "Copy lod 0 to every lod slot", Action = _actionList[MenuActionType.CopyLod0ToEveryLodSlot] });

            MenuItems[3].Children.Add(new ToolbarItem() { Name = "Focus camera", Action = _actionList[MenuActionType.FocusSelection] });
            MenuItems[3].Children.Add(new ToolbarItem() { Name = "Reset camera", Action = _actionList[MenuActionType.ResetCamera] });
            MenuItems[3].Children.Add(new ToolbarItem() { IsSeparator = true });
            MenuItems[3].Children.Add(new ToolbarItem() { Name = "Toggle backface rendering", Action = _actionList[MenuActionType.ToogleBackFaceRendering] });
            MenuItems[3].Children.Add(new ToolbarItem() { Name = "Toggle Big scene rendering", Action = _actionList[MenuActionType.ToggleLargeSceneRendering] });
        }

        void CreateButtons()
        {
            CreateGeneralButtons();
            CreateSelectionButtons();
            CreateObjectButtons();
            CreateFaceButtons();
            CreateVertexButtons();
        }

        void CreateGeneralButtons()
        {
            CustomButtons.Add(new MenuBarButton(_actionList[MenuActionType.Save]) { Image = ResourceController.SaveFileIcon });
            CustomButtons.Add(new MenuBarButton(_actionList[MenuActionType.OpenImportReference]) { Image = ResourceController.OpenReferenceMeshIcon });
            CustomButtons.Add(new MenuBarButton(_actionList[MenuActionType.Undo]) { Image = ResourceController.UndoIcon });

            CustomButtons.Add(new MenuBarButton(null) { IsSeperator = true });
        }

        void CreateSelectionButtons()
        {
            CustomButtons.Add(new MenuBarGroupButton(_actionList[MenuActionType.Gizmo_Arrow], "Gizmo", true) { Image = ResourceController.Gizmo_CursorIcon });
            CustomButtons.Add(new MenuBarGroupButton(_actionList[MenuActionType.Gizmo_Move],"Gizmo") { Image = ResourceController.Gizmo_MoveIcon });
            CustomButtons.Add(new MenuBarGroupButton(_actionList[MenuActionType.Gizmo_Rotate], "Gizmo") { Image = ResourceController.Gizmo_RotateIcon});
            CustomButtons.Add(new MenuBarGroupButton(_actionList[MenuActionType.Gizmo_Scale],"Gizmo") {  Image = ResourceController.Gizmo_ScaleIcon });

            CustomButtons.Add(new MenuBarButton(null) { IsSeperator = true });

            CustomButtons.Add(new MenuBarGroupButton(_actionList[MenuActionType.SelectObject], "SelectionMode", true) { Image = ResourceController.Selection_Object_Icon });
            CustomButtons.Add(new MenuBarGroupButton(_actionList[MenuActionType.SelectFace], "SelectionMode") {  Image = ResourceController.Selection_Face_Icon });
            CustomButtons.Add(new MenuBarGroupButton(_actionList[MenuActionType.SelectVertex], "SelectionMode") { Image = ResourceController.Selection_Vertex_Icon });
            CustomButtons.Add(new MenuBarButton(_actionList[MenuActionType.ViewOnlySelected]) { Image = ResourceController.ViewSelectedIcon });

            CustomButtons.Add(new MenuBarButton(null) { IsSeperator = true });
        }

        void CreateObjectButtons()
        {
            CustomButtons.Add(new MenuBarButton(_actionList[MenuActionType.DevideToSubmesh]) { ShowRule = ButtonVisabilityRule.ObjectMode, Image = ResourceController.DivideIntoSubMeshIcon });
            CustomButtons.Add(new MenuBarButton(_actionList[MenuActionType.MergeSelectedMeshes]) { ShowRule = ButtonVisabilityRule.ObjectMode, Image = ResourceController.MergeMeshIcon });
            CustomButtons.Add(new MenuBarButton(_actionList[MenuActionType.DuplicateSelected]) { ShowRule = ButtonVisabilityRule.ObjectMode, Image = ResourceController.DuplicateIcon });
            CustomButtons.Add(new MenuBarButton(_actionList[MenuActionType.DeleteSelected]) { ShowRule = ButtonVisabilityRule.ObjectMode, Image = ResourceController.DeleteIcon });
            CustomButtons.Add(new MenuBarButton(_actionList[MenuActionType.ConvertSelectedMeshIntoStaticAtCurrentAnimFrame]) { ShowRule = ButtonVisabilityRule.ObjectMode, Image = ResourceController.FreezeAnimationIcon });

            CustomButtons.Add(new MenuBarButton(null) { IsSeperator = true });

            CustomButtons.Add(new MenuBarButton(_actionList[MenuActionType.ReduceMesh10x]) { ShowRule = ButtonVisabilityRule.ObjectMode, Image = ResourceController.ReduceMeshIcon });
            CustomButtons.Add(new MenuBarButton(_actionList[MenuActionType.CreateLod]) { ShowRule = ButtonVisabilityRule.ObjectMode, Image = ResourceController.CreateLodIcon });
            CustomButtons.Add(new MenuBarButton(_actionList[MenuActionType.OpenBmiTool]) { ShowRule = ButtonVisabilityRule.ObjectMode, Image = ResourceController.BmiToolIcon });
            CustomButtons.Add(new MenuBarButton(_actionList[MenuActionType.OpenSkeletonResharper]) { ShowRule = ButtonVisabilityRule.ObjectMode, Image = ResourceController.SkeletonReshaperIcon });
            CustomButtons.Add(new MenuBarButton(_actionList[MenuActionType.OpenReRiggingTool]) { ShowRule = ButtonVisabilityRule.ObjectMode, Image = ResourceController.ReRiggingIcon });
            CustomButtons.Add(new MenuBarButton(_actionList[MenuActionType.OpenPinTool]) { ShowRule = ButtonVisabilityRule.ObjectMode, Image = ResourceController.PinIcon });
        }

        void CreateFaceButtons()
        {
            CustomButtons.Add(new MenuBarButton(_actionList[MenuActionType.ConvertFaceToVertexSelection]) { ShowRule = ButtonVisabilityRule.FaceMode, Image = ResourceController.FaceToVertexIcon });
            CustomButtons.Add(new MenuBarButton(_actionList[MenuActionType.GrowFaceSelection]) { ShowRule = ButtonVisabilityRule.FaceMode, Image = ResourceController.GrowSelectionIcon });
            CustomButtons.Add(new MenuBarButton(_actionList[MenuActionType.DevideToSubmesh]) { ShowRule = ButtonVisabilityRule.FaceMode, Image = ResourceController.DivideIntoSubMeshIcon });
            CustomButtons.Add(new MenuBarButton(_actionList[MenuActionType.DuplicateSelected]) { ShowRule = ButtonVisabilityRule.FaceMode, Image = ResourceController.DuplicateIcon });
            CustomButtons.Add(new MenuBarButton(_actionList[MenuActionType.DeleteSelected]) { ShowRule = ButtonVisabilityRule.FaceMode, Image = ResourceController.DeleteIcon });
        }

        void CreateVertexButtons()
        {
            CustomButtons.Add(new MenuBarButton(_actionList[MenuActionType.OpenVertexDebuggerTool]) { ShowRule = ButtonVisabilityRule.VertexMode, Image = ResourceController.VertexDebuggerIcon });
        }

        void ProcessHotkeys()
        {
            var actionList = _actionList
                .Where(x => x.Value.Hotkey != null)
                .Select(x => x.Value);

            foreach (var item in actionList)
            {
                item.UpdateToolTip();
                _commandFactory.Register(item);
            }
        }

        private void OnUndoStackChanged()
        {
            _actionList[MenuActionType.Undo].ToolTip = _commandExecutor.GetUndoHint();
            _actionList[MenuActionType.Undo].IsActionEnabled.Value = _commandExecutor.CanUndo();
        }

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
                _ruleFactory.Validate(button);

            // Validate if menu action is enabled
            foreach (var action in _actionList.Values)
                _ruleFactory.Validate(action);
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
        }

        void ImportReference_PaladinVMD() => ModelLoader.LoadReference(@"variantmeshes\variantmeshdefinitions\brt_paladin.variantmeshdefinition");
        void ImportReference_Slayer() => ModelLoader.LoadReference(@"variantmeshes\variantmeshdefinitions\dwf_giant_slayers.variantmeshdefinition");
        void ImportReference_Goblin() => ModelLoader.LoadReference(@"variantmeshes\variantmeshdefinitions\grn_forest_goblins_base.variantmeshdefinition");
        void ImportDebugMap() => MapLoaderService.Load(_packFileService, _componentManager, ModelLoader);

        void ClearConsole() => Console.Clear();
    }
}
