using Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System;
using System.Linq;
using View3D.Commands.Object;
using View3D.Components.Component.Selection;
using View3D.Components.Input;
using View3D.Rendering;
using View3D.SceneNodes;
using View3D.Services;

namespace View3D.Components.Component
{
    public class ObjectEditor : BaseComponent
    {
        ILogger _logger = Logging.Create<ObjectEditor>();

        KeyboardComponent _keyboard;
        SelectionManager _selectionManager; 
        CommandExecutor _commandManager;

        public ObjectEditor(WpfGame game) : base(game)
        {
        }

        public override void Initialize()
        {
            _keyboard = GetComponent<KeyboardComponent>();
            _selectionManager = GetComponent<SelectionManager>();
            _commandManager = GetComponent<CommandExecutor>();

            base.Initialize();
        }

        public override void Update(GameTime gameTime)
        {
            var objectSelectionState = _selectionManager.GetState() as ObjectSelectionState;
            if (objectSelectionState == null)
                return;

            if (_keyboard.IsKeyReleased(Keys.Delete))
            {
                var command = new DeleteObjectsCommand(objectSelectionState.CurrentSelection());
                _commandManager.ExecuteCommand(command);
            }
            else if (_keyboard.IsKeyComboReleased(Keys.D, Keys.LeftControl))
            {
                if (objectSelectionState.CurrentSelection().Count != 0)
                {
                    var command = new DuplicateObjectCommand(objectSelectionState.CurrentSelection().Select(x=>(SceneNode)x).ToList());
                    _commandManager.ExecuteCommand(command);
                }
            }
            else if (_keyboard.IsKeyComboReleased(Keys.S, Keys.LeftAlt))
            {
                if (objectSelectionState.GetSingleSelectedObject() is IDrawableNode drawableNode)
                {
                    var command = new DivideObjectIntoSubmeshesCommand(drawableNode);
                    _commandManager.ExecuteCommand(command);
                }
            }
        }
    }
}
