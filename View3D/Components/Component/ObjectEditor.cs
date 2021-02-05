using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Commands;
using View3D.Commands.Object;
using View3D.Components.Component.Selection;
using View3D.Components.Input;
using View3D.Rendering;

namespace View3D.Components.Component
{
    public class ObjectEditor : BaseComponent
    {
        KeyboardComponent _keyboard;
        SelectionManager _selectionManager; 
        SceneManager _sceneManager;
        CommandManager _commandManager;

        public ObjectEditor(WpfGame game) : base(game)
        {
        }

        public override void Initialize()
        {
            _keyboard = GetComponent<KeyboardComponent>();
            _selectionManager = GetComponent<SelectionManager>();
            _sceneManager = GetComponent<SceneManager>();
            _commandManager = GetComponent<CommandManager>();
            base.Initialize();
        }

        public override void Update(GameTime gameTime)
        {
            var objectSelectionState = _selectionManager.GetState() as ObjectSelectionState;
            if (objectSelectionState == null)
                return;


            if (_keyboard.IsKeyReleased(Keys.Delete))
            {
                var command = new DeleteObjectsCommand(objectSelectionState.CurrentSelection(), _sceneManager, _selectionManager);
                _commandManager.ExecuteCommand(command);
            }

            base.Update(gameTime);
        }

        // DeleteObject = del
        // Duplicate object= ctrl+d
    }
}
