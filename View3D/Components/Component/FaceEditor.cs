using Microsoft.Xna.Framework.Input;
using MonoGame.Framework.WpfInterop;
using View3D.Commands.Face;
using View3D.Components.Component.Selection;
using View3D.Components.Input;

namespace View3D.Components.Component
{
    class FaceEditor : BaseComponent
    {

        KeyboardComponent _keyboard;
        SelectionManager _selectionManager;
        SceneManager _sceneManager;
        CommandManager _commandManager;

        public FaceEditor(WpfGame game) : base(game)
        {
        }

        public override void Initialize()
        {
            _keyboard = GetComponent<KeyboardComponent>();
            _selectionManager = GetComponent<SelectionManager>();
            _sceneManager = GetComponent<SceneManager>();
            _commandManager = GetComponent<CommandManager>();

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
                var command = new DeleteFaceCommand(_selectionManager);
                command.FacesToDelete = faceSelectionState.CurrentSelection();
                _commandManager.ExecuteCommand(command);
            }
            
        }
    }
}
