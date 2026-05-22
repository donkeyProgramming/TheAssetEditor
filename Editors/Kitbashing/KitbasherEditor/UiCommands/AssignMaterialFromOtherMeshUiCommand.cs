using CommonControls.SelectionListDialog;
using Editors.KitbasherEditor.Commands;
using Editors.KitbasherEditor.Core.MenuBarViews;
using GameWorld.Core.Commands;
using GameWorld.Core.Components;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.Rendering.Materials.Capabilities;
using GameWorld.Core.SceneNodes;
using Shared.Core.Services;
using Shared.Ui.BaseDialogs.SelectionListDialog;
using Shared.Ui.Common.MenuSystem;

namespace Editors.KitbasherEditor.UiCommands
{
    internal class AssignMaterialFromOtherMeshUiCommand : IScopedKitbasherUiCommand
    {
        private readonly SelectionManager _selectionManager;
        private readonly SceneManager _sceneManager;
        private readonly IStandardDialogs _dialogProvider;
        private readonly CommandFactory _commandFactory;

        public string ToolTip { get; set; } = "Assign a already know material to selected objects";

        public ActionEnabledRule EnabledRule => ActionEnabledRule.AtleastOneObjectSelected;

        public Hotkey? HotKey => null;

        public AssignMaterialFromOtherMeshUiCommand(SelectionManager selectionManager, SceneManager sceneManager, IStandardDialogs dialogProvider, CommandFactory commandFactory)
        {
            _selectionManager = selectionManager;
            _sceneManager = sceneManager;
            _dialogProvider = dialogProvider;
            _commandFactory = commandFactory;
        }

        public void Execute()
        {
            // Ensure we have a selection and that it's of the correct type
            var selectedObjects = _selectionManager.GetState() as ObjectSelectionState;
            if (selectedObjects == null || selectedObjects.SelectionCount() == 0)
                return;

            // Get all meshes in the scene. 
            var meshNodes = _sceneManager.GetEnumeratorConditional(n => n is Rmv2MeshNode).OfType<Rmv2MeshNode>().ToList();
            var meshList = new List<SelectionListViewModel<Rmv2MeshNode>.Item>();
            foreach (var currentMeshNode in meshNodes)
            {
                var textureName = "";
                var metalRoughCap = currentMeshNode.Material.TryGetCapability<MetalRoughCapability>();
                if (metalRoughCap != null)
                    textureName = metalRoughCap.BaseColour.TexturePath;

                var specGlossCap = currentMeshNode.Material.TryGetCapability<SpecGlossCapability>();
                if (specGlossCap != null)
                    textureName = specGlossCap.DiffuseMap.TexturePath;


                var selectionItem = new SelectionListViewModel<Rmv2MeshNode>.Item
                {
                    DisplayName = currentMeshNode.Name + " - " + textureName,
                    ItemValue = currentMeshNode
                };
                meshList.Add(selectionItem);
            }

            var window = SelectionListWindow.ShowDialog("Select mesh to copy material from", meshList);
            var selected = meshList.Where(x => x.IsChecked.Value).ToList();
            if (!window.Result || selected.Count == 0)
            {
                _dialogProvider.ShowDialogBox("No mesh selected to copy from");
                return;
            }

            if (selected.Count > 1)
            {
                _dialogProvider.ShowDialogBox("Multiple meshes selected, please select only one to copy from");
                return;
            }

            var selectedMeshes = selectedObjects.SelectedObjects().OfType<Rmv2MeshNode>().ToList();
            _commandFactory.Create<AssignMaterialFromOtherMeshCommand>()
               .Configure(x => x.Configure(selected.First().ItemValue.Material, selectedMeshes))
               .BuildAndExecute();

        }
    }
}
