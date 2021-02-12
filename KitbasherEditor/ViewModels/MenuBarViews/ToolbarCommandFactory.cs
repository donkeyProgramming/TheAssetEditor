using System.Collections.Generic;
using System.Windows.Input;

namespace KitbasherEditor.ViewModels.MenuBarViews
{
    public class ToolbarCommandFactory
    {

        class CommandMapping
        {
            public ICommand Command { get; set; }
            public Key Key { get; set; }
            public ModifierKeys ModifierKeys { get; set; }
        }

        List<CommandMapping> _mappings = new List<CommandMapping>();

        public ICommand Register(ICommand command, Key key, ModifierKeys modifierKeys)
        {
            var mapping = new CommandMapping()
            {
                Command = command,
                Key = key,
                ModifierKeys = modifierKeys
            };
            _mappings.Add(mapping);
            return command;
        }


        public bool TriggerCommand(Key key, ModifierKeys modifierKeys)
        {
            bool isHandled = false;
            foreach (var item in _mappings)
            {
                if (item.Key == key && item.ModifierKeys == modifierKeys)
                {
                    item.Command.Execute(null);
                    isHandled = true;
                }
            }

            return isHandled;
        }


    }
}
