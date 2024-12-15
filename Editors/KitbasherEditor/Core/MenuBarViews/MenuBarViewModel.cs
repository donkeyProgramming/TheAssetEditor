using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using Editors.KitbasherEditor.ChildEditors.MeshFitter;
using Editors.KitbasherEditor.ChildEditors.PinTool.UiCommands;
using Editors.KitbasherEditor.ChildEditors.ReRiggingTool;
using Editors.KitbasherEditor.ChildEditors.VertexDebugger;
using Editors.KitbasherEditor.Core.MenuBarViews;
using Editors.KitbasherEditor.UiCommands;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.Services;
using KitbasherEditor.ViewModels.MenuBarViews.Helpers;
using Shared.Core.Events;
using Shared.EmbeddedResources;
using Shared.Ui.Common.MenuSystem;

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

        public MenuBarViewModel(CommandExecutor commandExecutor, IEventHub eventHub, MenuItemVisibilityRuleEngine menuItemVisibilityRuleEngine, TransformToolViewModel transformToolViewModel,IUiCommandFactory uiCommandFactory, WindowKeyboard windowKeyboard)
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

            eventHub.Register<CommandStackChangedEvent>(this, OnUndoStackChanged);
            eventHub.Register<SelectionChangedEvent>(this, OnSelectionChanged);

        }

        void RegisterActions()
        {
            RegisterUiCommand<SaveCommand>();
            RegisterUiCommand<SaveAsCommand>();

            RegisterUiCommand<BrowseForReferenceCommand>();
            RegisterUiCommand<ImportGeneralReferenceCommand>();
            RegisterUiCommand<ImportKarlHammerReferenceCommand>();
            
            RegisterUiCommand<DeleteLodsCommand>();    
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
            RegisterUiCommand<OpenRenderSettingsWindowCommand>();

            RegisterUiCommand<DivideSubMeshCommand>();
            RegisterUiCommand<MergeObjectsCommand>();
            RegisterUiCommand<DuplicateObjectCommand>();
            RegisterUiCommand<DeleteObjectCommand>();
            RegisterUiCommand<CreateStaticMeshCommand>();

            RegisterUiCommand<ReduceMeshCommand>();
            RegisterUiCommand<OpenBmiToolCommand>();
            RegisterUiCommand<OpenSkeletonReshaperToolCommand>();
            RegisterUiCommand<OpenReriggingToolCommand>();
            RegisterUiCommand<OpenPinToolCommand>();

            RegisterUiCommand<ExpandFaceSelectionCommand>();
            RegisterUiCommand<ConvertFaceToVertexCommand>();
            RegisterUiCommand<OpenVertexDebuggerCommand>();
        }

        ObservableCollection<ToolbarItem> CreateToolbarMenu()
        {
            var builder = new ToolbarBuilder(_uiCommands);

            var fileToolbar = builder.CreateRootToolBar("File");
            builder.CreateToolBarItem<SaveCommand>(fileToolbar, "Save");
            builder.CreateToolBarItem<SaveAsCommand>(fileToolbar, "Save As");
            builder.CreateToolBarSeparator(fileToolbar);
            builder.CreateToolBarItem<BrowseForReferenceCommand>(fileToolbar, "Import Reference model");

            var debugToolbar = builder.CreateRootToolBar("Debug");
            builder.CreateToolBarItem<ImportGeneralReferenceCommand>(debugToolbar, "Import General");
            builder.CreateToolBarItem<ImportKarlHammerReferenceCommand>(debugToolbar, "Import Hammer");
            builder.CreateToolBarItem<DeleteLodsCommand>(debugToolbar, "Delete lods");

            var toolsToolbar = builder.CreateRootToolBar("Tools");
            builder.CreateToolBarItem<GroupItemsCommand>(toolsToolbar, "(Un)Group selection");
            builder.CreateToolBarItem<ReduceMeshCommand>(toolsToolbar, "\"Reduce mesh by 10%\"");
            builder.CreateToolBarItem<SortMeshesCommand>(toolsToolbar, "Sort models by name");

            var renderingToolbar = builder.CreateRootToolBar("Rendering");
            builder.CreateToolBarItem<FocusCameraCommand>(renderingToolbar, "Focus camera");
            builder.CreateToolBarItem<ResetCameraCommand>(renderingToolbar, "Reset camera");
            builder.CreateToolBarItem<OpenRenderSettingsWindowCommand>(renderingToolbar, "Open render settings");


            return builder.Build();
        }

        ObservableCollection<MenuBarButton> CreateButtons()
        {
            var builder = new ButtonBuilder(_uiCommands);

            // General
            builder.CreateButton<SaveCommand>(IconLibrary.SaveFileIcon);
            builder.CreateButton<BrowseForReferenceCommand>(IconLibrary.OpenReferenceMeshIcon);
            builder.CreateButton<UndoCommand>(IconLibrary.UndoIcon);
            builder.CreateButtonSeparator();

            // Gizmo buttons
            builder.CreateGroupedButton<SelectGizmoModeCommand>("Gizmo", true, IconLibrary.Gizmo_CursorIcon);
            builder.CreateGroupedButton<MoveGizmoModeCommand>("Gizmo", false, IconLibrary.Gizmo_MoveIcon);
            builder.CreateGroupedButton<RotateGizmoModeCommand>("Gizmo", false, IconLibrary.Gizmo_RotateIcon);
            builder.CreateGroupedButton<ScaleGizmoModeCommand>("Gizmo", false, IconLibrary.Gizmo_ScaleIcon);
            builder.CreateButtonSeparator();

            // Selection buttons
            builder.CreateGroupedButton<ObjectSelectionModeCommand>("SelectionMode", true, IconLibrary.Selection_Object_Icon);
            builder.CreateGroupedButton<FaceSelectionModeCommand>("SelectionMode", false, IconLibrary.Selection_Face_Icon);
            builder.CreateGroupedButton<VertexSelectionModeCommand>("SelectionMode", false, IconLibrary.Selection_Vertex_Icon);
            builder.CreateButton<ToggleViewSelectedCommand>(IconLibrary.ViewSelectedIcon);
            builder.CreateButtonSeparator();

            // Object buttons
            builder.CreateButton<DivideSubMeshCommand>(IconLibrary.DivideIntoSubMeshIcon, ButtonVisibilityRule.ObjectMode);
            builder.CreateButton<MergeObjectsCommand>(IconLibrary.MergeMeshIcon, ButtonVisibilityRule.ObjectMode);
            builder.CreateButton<DuplicateObjectCommand>(IconLibrary.DuplicateIcon, ButtonVisibilityRule.ObjectMode);
            builder.CreateButton<DeleteObjectCommand>(IconLibrary.DeleteIcon, ButtonVisibilityRule.ObjectMode);
            builder.CreateButton<CreateStaticMeshCommand>(IconLibrary.FreezeAnimationIcon, ButtonVisibilityRule.ObjectMode);
            builder.CreateButtonSeparator();
            builder.CreateButton<ReduceMeshCommand>(IconLibrary.ReduceMeshIcon, ButtonVisibilityRule.ObjectMode);
            //builder.CreateButton<OpenBmiToolCommand>(ResourceController.BmiToolIcon, ButtonVisibilityRule.ObjectMode);    <-- Disabled to see if anyone complains. Plan is to delete it
            builder.CreateButton<OpenSkeletonReshaperToolCommand>(IconLibrary.SkeletonReshaperIcon, ButtonVisibilityRule.ObjectMode);
            builder.CreateButton<OpenReriggingToolCommand>(IconLibrary.ReRiggingIcon, ButtonVisibilityRule.ObjectMode);
            builder.CreateButton<OpenPinToolCommand>(IconLibrary.PinIcon, ButtonVisibilityRule.ObjectMode);

            // Face buttons
            builder.CreateButton<ConvertFaceToVertexCommand>(IconLibrary.FaceToVertexIcon, ButtonVisibilityRule.FaceMode);
            builder.CreateButton<ExpandFaceSelectionCommand>(IconLibrary.GrowSelectionIcon, ButtonVisibilityRule.FaceMode);
            builder.CreateButton<DivideSubMeshCommand>(IconLibrary.DivideIntoSubMeshIcon, ButtonVisibilityRule.FaceMode);
            builder.CreateButton<DuplicateObjectCommand>(IconLibrary.DuplicateIcon, ButtonVisibilityRule.FaceMode);
            builder.CreateButton<DeleteObjectCommand>(IconLibrary.DeleteIcon, ButtonVisibilityRule.FaceMode);

            // Vertex buttons
            builder.CreateButton<OpenVertexDebuggerCommand>(IconLibrary.VertexDebuggerIcon, ButtonVisibilityRule.VertexMode);
            
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

        MenuAction GetMenuAction<T>() where T : ITransientKitbasherUiCommand
        {
            return _uiCommands.First(x => x.Key == typeof(T)).Value;
        }
    }
}
