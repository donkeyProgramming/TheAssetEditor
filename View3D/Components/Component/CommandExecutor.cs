using Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Commands;
using View3D.Components;
using View3D.Components.Input;
using View3D.Scene;

namespace View3D.Components.Component
{
    public delegate void CommandStackChangedDelegate();
    public class CommandExecutor : BaseComponent
    {
        ILogger _logger = Logging.Create<CommandExecutor>();
        Stack<ICommand> _commands = new Stack<ICommand>();


        public event CommandStackChangedDelegate CommandStackChanged;

        public CommandExecutor(WpfGame game) : base(game)
        {
        }

        public void ExecuteCommand(ICommand command)
        {
            _commands.Push(command);
            command.Initialize(Game);
            command.Execute();

            CommandStackChanged?.Invoke();
        }

        public string GetUndoHint()
        {
            if (!CanUndo())
                return "No items to undo";

            var obj = _commands.Peek();
            return obj.GetType().Name;
        }

        public bool CanUndo()
        {
            return _commands.Count != 0;
        }

        public void Undo()
        {
            if (CanUndo())
            {
                var command = _commands.Pop();
                command.Undo();
                CommandStackChanged?.Invoke();
            }
        }
    }
}

