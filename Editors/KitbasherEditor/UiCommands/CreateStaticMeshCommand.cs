using Editors.KitbasherEditor.Core.MenuBarViews;
using GameWorld.Core.Commands;
using GameWorld.Core.Commands.Object;
using GameWorld.Core.Components;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.SceneNodes;
using Shared.Ui.Common.MenuSystem;
using MessageBox = System.Windows.MessageBox;

namespace Editors.KitbasherEditor.UiCommands
{
    public class CreateStaticMeshCommand : ITransientKitbasherUiCommand
    {
        public string ToolTip { get; set; } = "Convert the selected mesh at at the given animation frame into a static mesh";
        public ActionEnabledRule EnabledRule => ActionEnabledRule.AtleastOneObjectSelected;
        public Hotkey? HotKey { get; } = null;

        private readonly AnimationsContainerComponent _animationsContainerComponent;
        private readonly SelectionManager _selectionManager;
        private readonly CommandFactory _commandFactory;
        private readonly SceneManager _sceneManager;

        public CreateStaticMeshCommand(AnimationsContainerComponent animationsContainerComponent, SelectionManager selectionManager, CommandFactory commandFactory, SceneManager sceneManager)
        {
            _animationsContainerComponent = animationsContainerComponent;
            _selectionManager = selectionManager;
            _commandFactory = commandFactory;
            _sceneManager = sceneManager;
        }

        public void Execute()
        {
            // Get the frame
            var animationPlayers = _animationsContainerComponent;
            var mainPlayer = animationPlayers.Get("MainPlayer");

            var frame = mainPlayer.GetCurrentAnimationFrame();
            if (frame == null)
            {
                MessageBox.Show("An animation must be playing for this tool to work");
                return;
            }

            var state = _selectionManager.GetState<ObjectSelectionState>();
            var selectedObjects = state.SelectedObjects();
            var meshes = new List<Rmv2MeshNode>();

            var groupNodeContainer = new GroupNode("staticMesh");
            var root = _sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);
            var lod0 = root.GetLodNodes()[0];
            lod0.AddObject(groupNodeContainer);
            foreach (var obj in selectedObjects)
            {
                if (obj is Rmv2MeshNode meshNode)
                {
                    var cpy = SceneNodeHelper.CloneNode(meshNode);
                    groupNodeContainer.AddObject(cpy);
                    meshes.Add(cpy);
                }
            }

            _commandFactory.Create<CreateAnimatedMeshPoseCommand>()
                .IsUndoable(false)
                .Configure(x => x.Configure(meshes, frame, true))
                .BuildAndExecute();
        }
    }
}
