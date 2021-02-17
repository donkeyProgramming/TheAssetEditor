using Microsoft.Xna.Framework.Input;
using MonoGame.Framework.WpfInterop;
using System.Collections.Generic;
using System.Linq;
using View3D.Commands.Face;
using View3D.Commands.Object;
using View3D.Components.Component.Selection;
using View3D.Components.Input;
using View3D.SceneNodes;

namespace View3D.Components.Component
{
    public class FaceEditor : BaseComponent
    {

        KeyboardComponent _keyboard;
        SelectionManager _selectionManager;
        SceneManager _sceneManager;
        CommandExecutor _commandManager;

        public FaceEditor(WpfGame game) : base(game)
        {
        }

        public override void Initialize()
        {
            _keyboard = GetComponent<KeyboardComponent>();
            _selectionManager = GetComponent<SelectionManager>();
            _sceneManager = GetComponent<SceneManager>();
            _commandManager = GetComponent<CommandExecutor>();

            _keyboard.KeybordButtonReleased += OnKeyReleased;

            base.Initialize();
        }

        private void OnKeyReleased(Keys key)
        {
            var faceSelectionState = _selectionManager.GetState() as FaceSelectionState;
            if (faceSelectionState == null)
                return;

            if (_keyboard.IsKeyReleased(Keys.Delete))
            {
                var selectedFaceCount = faceSelectionState.CurrentSelection().Count()*3;
                var totalObjectFaceCount = faceSelectionState.RenderObject.Geometry.GetIndexCount();

                if (selectedFaceCount == totalObjectFaceCount)
                {
                    var command = new DeleteObjectsCommand(new List<ISelectable>() { faceSelectionState.RenderObject });
                    _commandManager.ExecuteCommand(command);
                }
                else
                {
                    var command = new DeleteFaceCommand(faceSelectionState.RenderObject.Geometry, faceSelectionState.CurrentSelection());
                    _commandManager.ExecuteCommand(command);
                }
            }
            
        }
    }
}
