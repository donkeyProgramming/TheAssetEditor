using System.Windows.Input;

namespace KitbasherEditor.ViewModels.MenuBarViews
{
    public interface IKeyboardHandler
    {
        bool HandleKeyUp(Key key, ModifierKeys modifierKeys);
    }
}
