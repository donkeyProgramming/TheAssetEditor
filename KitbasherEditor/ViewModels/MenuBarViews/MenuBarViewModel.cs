using CommonControls.Common.MenuSystem;
using CommonControls.Events.UiCommands;
using CommonControls.Resources;
using KitbasherEditor.ViewModels.MenuBarViews.Helpers;
using KitbasherEditor.ViewModels.UiCommands;
using Monogame.WpfInterop.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using View3D.Components.Component;
using View3D.Components.Component.Selection;

namespace KitbasherEditor.ViewModels.MenuBarViews
{
    public class MenuBarViewModel : IKeyboardHandler
    {
        public ObservableCollection<ToolbarItem> MenuItems { get; set; } = new ObservableCollection<ToolbarItem>();
        public ObservableCollection<MenuBarButton> CustomButtons { get; set; } = new ObservableCollection<MenuBarButton>();
        public TransformToolViewModel TransformTool { get; set; }

        private readonly IUiCommandFactory _uiCommandFactory;
        private readonly CommandExecutor _commandExecutor;
        private readonly MenuItemVisibilityRuleEngine _menuItemVisibilityRuleEngine;
        private readonly ActionHotkeyHandler _hotKeyHandler = new ActionHotkeyHandler();
        private readonly WindowKeyboard _keyboard;
        private readonly Dictionary<Type, MenuAction> _uiCommands = new();

        public MenuBarViewModel(CommandExecutor commandExecutor, EventHub eventHub, MenuItemVisibilityRuleEngine menuItemVisibilityRuleEngine, TransformToolViewModel transformToolViewModel,IUiCommandFactory uiCommandFactory, WindowKeyboard windowKeyboard)
        {
            _commandExecutor = commandExecutor;
            _menuItemVisibilityRuleEngine = menuItemVisibilityRuleEngine;
            _uiCommandFactory = uiCommandFactory;
            _keyboard = windowKeyboard;
            TransformTool = transformToolViewModel;

            RegisterActions();
            RegisterHotkeys();
            CustomButtons = CreateButtons();
            MenuItems = CreateToolbarMenu();

            eventHub.Register<CommandStackChangedEvent>(OnUndoStackChanged);
            eventHub.Register<SelectionChangedEvent>(OnSelectionChanged);
        }

        void RegisterActions()
        {
            RegisterUiCommand<SaveCommand>();
            RegisterUiCommand<SaveAsCommand>();                      

            RegisterUiCommand<GenerateWh2WsModelCommand>();
            RegisterUiCommand<GenerateWh3WsModelCommand>();

            RegisterUiCommand<BrowseForReferenceCommand>();
            RegisterUiCommand<ImportPaladinReferenceCommand>();
            RegisterUiCommand<ImportGoblinReferenceCommand>();
            RegisterUiCommand<ImportSlayerReferenceCommand>();
            
            RegisterUiCommand<DeleteLodsCommand>();
            RegisterUiCommand<ClearConsoleCommand>();
            RegisterUiCommand<UndoCommand>();
            RegisterUiCommand<SortMeshesCommand>();

            RegisterUiCommand<GroupItemsCommand>();
            RegisterUiCommand<ScaleGizmoUpCommand>();
            RegisterUiCommand<ScaleGizmoDownCommand>();
            RegisterUiCommand<SelectGizmoModeCommand>();
            RegisterUiCommand<MoveGizmoModeCommand>();
            RegisterUiCommand<RotateGizmoModeCommand>();
            RegisterUiCommand<ScaleGizmoModeCommand>();

            RegisterUiCommand<ObjectSelectionModeCommand>();
            RegisterUiCommand<FaceSelectionModeCommand>();
            RegisterUiCommand<VertexSelectionModeCommand>();

            RegisterUiCommand<ToggleViewSelectedCommand>();
            RegisterUiCommand<ResetCameraCommand>();
            RegisterUiCommand<FocusCameraCommand>();
            RegisterUiCommand<ToggleBackFaceRenderingCommand>();
            RegisterUiCommand<ToggleLargeSceneRenderingCommand>();

            RegisterUiCommand<DivideSubMeshCommand>();
            RegisterUiCommand<MergeObjectsCommand>();
            RegisterUiCommand<DuplicateObjectCommand>();
            RegisterUiCommand<DeleteObjectCommand>();
            RegisterUiCommand<CreateStaticMeshCommand>();

            RegisterUiCommand<ReduceMeshCommand>();
            RegisterUiCommand<CreateLodCommand>();
            RegisterUiCommand<OpenBmiToolCommand>();
            RegisterUiCommand<OpenSkeletonReshaperToolCommand>();
            RegisterUiCommand<OpenReriggingToolCommand>();
            RegisterUiCommand<OpenPinToolCommand>();
            RegisterUiCommand<CopyRootLodCommand>();
            //CreateActionItem<UpdateWh2TexturesCommand>(x => x.Technique = View3D.Services.Rmv2UpdaterService.BaseColourGenerationTechniqueEnum.AdditiveBlending);
            //CreateActionItem<UpdateWh2TexturesCommand>(x => x.Technique = View3D.Services.Rmv2UpdaterService.BaseColourGenerationTechniqueEnum.ComparativeBlending);

            RegisterUiCommand<ExpandFaceSelectionCommand>();
            RegisterUiCommand<ConvertFaceToVertexCommand>();
            RegisterUiCommand<OpenVertexDebuggerCommand>();
        }

        ObservableCollection<ToolbarItem> CreateToolbarMenu()
        {
            ToolBarBuilder builder = new ToolBarBuilder(_uiCommands);

            var fileToolbar = builder.CreateRootToolBar("File");
            builder.CreateToolBarItem<SaveCommand>(fileToolbar, "Save");
            builder.CreateToolBarItem<SaveAsCommand>(fileToolbar, "Save As");
            builder.CreateToolBarSeparator(fileToolbar);
            builder.CreateToolBarItem<BrowseForReferenceCommand>(fileToolbar, "Import Reference model");
            
            var debugToolbar = builder.CreateRootToolBar("Debug");
            builder.CreateToolBarItem<ImportPaladinReferenceCommand>(debugToolbar, "Import Paladin");
            builder.CreateToolBarItem<ImportSlayerReferenceCommand>(debugToolbar, "Import Slayer");
            builder.CreateToolBarItem<ImportGoblinReferenceCommand>(debugToolbar, "Import Goblin");
            builder.CreateToolBarItem<DeleteLodsCommand>(debugToolbar, "Delete lods");
            builder.CreateToolBarItem<ClearConsoleCommand>(debugToolbar, "Clear console");

            var toolsToolbar = builder.CreateRootToolBar("Tools");
            builder.CreateToolBarItem<GroupItemsCommand>(toolsToolbar, "(Un)Group selection");
            builder.CreateToolBarItem<ReduceMeshCommand>(toolsToolbar, "\"Reduce mesh by 10%\"");
            builder.CreateToolBarItem<SortMeshesCommand>(toolsToolbar, "Sort models by name");
            builder.CreateToolBarSeparator(toolsToolbar);
            builder.CreateToolBarItem<GenerateWh3WsModelCommand>(toolsToolbar, "Generate WSMODEL (WH3)");
            builder.CreateToolBarItem<GenerateWh2WsModelCommand>(toolsToolbar, "Generate WSMODEL (WH2)");
            builder.CreateToolBarSeparator(toolsToolbar);
            builder.CreateToolBarItem<CopyRootLodCommand>(toolsToolbar, "Copy lod 0 to every lod slot");

            var renderingToolbar = builder.CreateRootToolBar("Rendering");
            builder.CreateToolBarItem<FocusCameraCommand>(renderingToolbar, "Focus camera");
            builder.CreateToolBarItem<ResetCameraCommand>(renderingToolbar, "Reset camera");
            builder.CreateToolBarSeparator(renderingToolbar);
            builder.CreateToolBarItem<ToggleBackFaceRenderingCommand>(renderingToolbar, "Toggle backface rendering");
            builder.CreateToolBarItem<ToggleLargeSceneRenderingCommand>(renderingToolbar, "Toggle Big scene rendering");

            return builder.Build();
        }

        ObservableCollection<MenuBarButton> CreateButtons()
        {
            ButtonBuilder builder = new ButtonBuilder(_uiCommands);

            // General
            builder.CreateButton<SaveCommand>(ResourceController.SaveFileIcon);
            builder.CreateButton<BrowseForReferenceCommand>(ResourceController.OpenReferenceMeshIcon);
            builder.CreateButton<UndoCommand>(ResourceController.UndoIcon);
            builder.CreateButtonSeparator();

            // Gizmo buttons
            builder.CreateGroupedButton<SelectGizmoModeCommand>("Gizmo", true, ResourceController.Gizmo_CursorIcon);
            builder.CreateGroupedButton<MoveGizmoModeCommand>("Gizmo", false, ResourceController.Gizmo_MoveIcon);
            builder.CreateGroupedButton<RotateGizmoModeCommand>("Gizmo", false, ResourceController.Gizmo_RotateIcon);
            builder.CreateGroupedButton<ScaleGizmoUpCommand>("Gizmo", false, ResourceController.Gizmo_ScaleIcon);
            builder.CreateButtonSeparator();

            // Selection buttons
            builder.CreateGroupedButton<ObjectSelectionModeCommand>("SelectionMode", true, ResourceController.Selection_Object_Icon);
            builder.CreateGroupedButton<FaceSelectionModeCommand>("SelectionMode", false, ResourceController.Selection_Face_Icon);
            builder.CreateGroupedButton<VertexSelectionModeCommand>("SelectionMode", false, ResourceController.Selection_Vertex_Icon);
            builder.CreateButton<ToggleViewSelectedCommand>(ResourceController.ViewSelectedIcon);
            builder.CreateButtonSeparator();

            // Object buttons
            builder.CreateButton<DivideSubMeshCommand>(ResourceController.DivideIntoSubMeshIcon, ButtonVisibilityRule.ObjectMode);
            builder.CreateButton<MergeObjectsCommand>(ResourceController.MergeMeshIcon, ButtonVisibilityRule.ObjectMode);
            builder.CreateButton<DuplicateObjectCommand>(ResourceController.DuplicateIcon, ButtonVisibilityRule.ObjectMode);
            builder.CreateButton<DeleteObjectCommand>(ResourceController.DeleteIcon, ButtonVisibilityRule.ObjectMode);
            builder.CreateButton<CreateStaticMeshCommand>(ResourceController.FreezeAnimationIcon, ButtonVisibilityRule.ObjectMode);
            builder.CreateButtonSeparator();
            builder.CreateButton<ReduceMeshCommand>(ResourceController.ReduceMeshIcon, ButtonVisibilityRule.ObjectMode);
            builder.CreateButton<CreateLodCommand>(ResourceController.CreateLodIcon, ButtonVisibilityRule.ObjectMode);
            builder.CreateButton<OpenBmiToolCommand>(ResourceController.BmiToolIcon, ButtonVisibilityRule.ObjectMode);
            builder.CreateButton<OpenSkeletonReshaperToolCommand>(ResourceController.SkeletonReshaperIcon, ButtonVisibilityRule.ObjectMode);
            builder.CreateButton<OpenReriggingToolCommand>(ResourceController.ReRiggingIcon, ButtonVisibilityRule.ObjectMode);
            builder.CreateButton<OpenPinToolCommand>(ResourceController.PinIcon, ButtonVisibilityRule.ObjectMode);

            // Face buttons
            builder.CreateButton<ConvertFaceToVertexCommand>(ResourceController.FaceToVertexIcon, ButtonVisibilityRule.FaceMode);
            builder.CreateButton<ExpandFaceSelectionCommand>(ResourceController.GrowSelectionIcon, ButtonVisibilityRule.FaceMode);
            builder.CreateButton<DivideSubMeshCommand>(ResourceController.DivideIntoSubMeshIcon, ButtonVisibilityRule.FaceMode);
            builder.CreateButton<DuplicateObjectCommand>(ResourceController.DuplicateIcon, ButtonVisibilityRule.FaceMode);
            builder.CreateButton<DeleteObjectCommand>(ResourceController.DeleteIcon, ButtonVisibilityRule.FaceMode);

            // Vertex buttons
            builder.CreateButton<OpenVertexDebuggerCommand>(ResourceController.VertexDebuggerIcon, ButtonVisibilityRule.VertexMode);
            
            return builder.Build();
        }

        void RegisterUiCommand<T>() where T : IKitbasherUiCommand
        {
            if (_uiCommands.ContainsKey(typeof(T)))
                throw new Exception($"Ui Action of type {typeof(T)} already added");
            _uiCommands[typeof(T)] = new KitbasherMenuItem<T>(_uiCommandFactory);

            
        }

        void RegisterHotkeys()
        {
            var actionList = _uiCommands
                .Where(x => x.Value.Hotkey != null)
                .Select(x => x.Value);

            foreach (var item in actionList)
            {
                item.UpdateToolTip();
                _hotKeyHandler.Register(item);
            }
        }

        public bool OnKeyReleased(Key key, Key systemKey, ModifierKeys modifierKeys)
        {
            _keyboard.SetKeyDown(key, false);
            _keyboard.SetKeyDown(systemKey, false);
            return _hotKeyHandler.TriggerCommand(key, modifierKeys);
        }

        public void OnKeyDown(Key key, Key systemKey, ModifierKeys modifiers)
        {
            _keyboard.SetKeyDown(systemKey, true);
            _keyboard.SetKeyDown(key, true);
        }

        void OnUndoStackChanged(CommandStackChangedEvent notification)
        {
            var undoAction = GetMenuAction<UndoCommand>();

            undoAction.ToolTip = notification.HintText;
            undoAction.IsActionEnabled.Value = _commandExecutor.CanUndo();
        }

        void OnSelectionChanged(SelectionChangedEvent notification)
        {
            var state = notification.NewState;

            if (state.Mode == GeometrySelectionMode.Object)
                GetMenuAction<ObjectSelectionModeCommand>().TriggerAction();
            else if (state.Mode == GeometrySelectionMode.Face)
                GetMenuAction<FaceSelectionModeCommand>().TriggerAction();
            else if (state.Mode == GeometrySelectionMode.Vertex)
                GetMenuAction<VertexSelectionModeCommand>().TriggerAction();
            else
                throw new NotImplementedException("Unknown state");

            // Validate if tool button is visible
            foreach (var button in CustomButtons)
                _menuItemVisibilityRuleEngine.Validate(button);

            // Validate if menu action is enabled
            foreach (var action in _uiCommands.Values)
                _menuItemVisibilityRuleEngine.Validate(action);
        }

        MenuAction GetMenuAction<T>() where T : IKitbasherUiCommand
        {
            return _uiCommands.First(x => x.Key == typeof(T)).Value;
        }
    }
}
