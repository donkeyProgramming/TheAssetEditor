using Microsoft.Xna.Framework.Input;
using MonoGame.Framework.WpfInterop;
using View3D.Commands.Object;
using View3D.Components.Component.Selection;
using View3D.Components.Input;

namespace View3D.Components.Component
{
    public class ObjectEditor : BaseComponent
    {
        KeyboardComponent _keyboard;
        SelectionManager _selectionManager; 
        SceneManager _sceneManager;
        CommandExecutor _commandManager;

        public ObjectEditor(WpfGame game) : base(game)
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
            var objectSelectionState = _selectionManager.GetState() as ObjectSelectionState;
            if (objectSelectionState == null)
                return;

            if (_keyboard.IsKeyReleased(Keys.Delete))
            {
                var command = new DeleteObjectsCommand(objectSelectionState.CurrentSelection(), _sceneManager, _selectionManager);
                _commandManager.ExecuteCommand(command);
            }
            else if (_keyboard.IsKeyComboReleased(Keys.D, Keys.LeftControl))
            {
                if (objectSelectionState.CurrentSelection().Count != 0)
                {
                    var command = new DuplicateObjectCommand(objectSelectionState.CurrentSelection(), _sceneManager, _selectionManager);
                    _commandManager.ExecuteCommand(command);
                }
            }
        }
    }
}
