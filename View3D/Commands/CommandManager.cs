using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Scene;

namespace View3D.Commands
{
    public class CommandManager
    {
        private readonly Input.Keyboard _keyboard;
        Stack<ICommand> _commands = new Stack<ICommand>();

        public CommandManager(Input.Keyboard keyboard )
        {
            _keyboard = keyboard;
            _keyboard.KeybordButtonReleased += OnUndoCommand;
        }

        private void OnUndoCommand(Keys key)
        {
            if (key == Keys.Z && _keyboard.IsKeyDown(Keys.LeftControl))
                Undo();
        }

        public void ExecuteCommand(ICommand command)
        {
            _commands.Push(command);
            command.Execute();
        }

        public void Undo()
        {
            if (_commands.Count != 0)
            {
                var command = _commands.Pop();
                command.Undo();
            }
        }
    }
}

