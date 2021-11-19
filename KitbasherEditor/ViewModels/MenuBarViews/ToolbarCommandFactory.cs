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
        List<MenuAction> _actions = new List<MenuAction>();

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


        public void Register(MenuAction action)
        {
            _actions.Add(action);
        }


        public bool TriggerCommand(Key key, ModifierKeys modifierKeys)
        {
            bool isHandled = false;
            foreach (var item in _actions)
            {
                if (item.Hotkey == null)
                    continue;

                if (item.Hotkey.Key == key && item.Hotkey.ModifierKeys == modifierKeys)
                {
                    item.TriggerAction();
                    isHandled = true;
                }
            }

            return isHandled;
        }


    }
}
