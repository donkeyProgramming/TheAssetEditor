using System.Windows.Input;
using Shared.Ui.Common.MenuSystem;

namespace Editors.KitbasherEditor.Core.MenuBarViews
{
    /// <summary>
    /// Encapsulates the keyboard event dispatch logic for MenuBarView.
    /// Extracted to enable unit testing of the visibility and source-filtering guards.
    /// </summary>
    public class MenuBarKeyboardDispatcher
    {
        private readonly Func<bool> _isVisible;
        private readonly Func<IKeyboardHandler?> _getHandler;

        public MenuBarKeyboardDispatcher(Func<bool> isVisible, Func<IKeyboardHandler?> getHandler)
        {
            _isVisible = isVisible;
            _getHandler = getHandler;
        }

        /// <summary>
        /// Returns true if the event was dispatched to the handler.
        /// </summary>
        public bool HandleKeyUp(Key key, Key systemKey, ModifierKeys modifiers, bool isSourceTextBox)
        {
            if (!_isVisible())
                return false;

            if (isSourceTextBox && modifiers == ModifierKeys.None)
                return false;

            if (_getHandler() is IKeyboardHandler handler)
            {
                handler.OnKeyReleased(key, systemKey, modifiers);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns true if the event was dispatched to the handler.
        /// </summary>
        public bool HandleKeyDown(Key key, Key systemKey, ModifierKeys modifiers, bool isSourceTextBox)
        {
            if (!_isVisible())
                return false;

            if (isSourceTextBox && modifiers == ModifierKeys.None)
                return false;

            if (_getHandler() is IKeyboardHandler handler)
            {
                handler.OnKeyDown(key, systemKey, modifiers);
                return true;
            }

            return false;
        }
    }
}
