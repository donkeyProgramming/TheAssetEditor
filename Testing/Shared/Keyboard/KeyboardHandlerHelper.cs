using System.Windows.Input;
using Shared.Ui.Common.MenuSystem;

namespace Test.TestingUtility.Keyboard
{
    /// <summary>
    /// Minimal <see cref="IKeyboardHandler"/> that mirrors the dispatch pattern used
    /// by <c>MenuBarViewModel</c>:
    /// <list type="bullet">
    ///   <item><description><see cref="OnKeyDown"/> → no hotkey dispatch.</description></item>
    ///   <item><description><see cref="OnKeyReleased"/> → hotkey dispatch via <see cref="ActionHotkeyHandler"/>.</description></item>
    /// </list>
    /// </summary>
    public sealed class TestableKeyboardHandler : IKeyboardHandler
    {
        private readonly ActionHotkeyHandler _hotkeys;

        public TestableKeyboardHandler(ActionHotkeyHandler hotkeys) => _hotkeys = hotkeys;

        public void OnKeyDown(Key key, Key systemKey, ModifierKeys modifiers) { /* intentionally empty */ }

        public bool OnKeyReleased(Key key, Key systemKey, ModifierKeys modifierKeys)
            => _hotkeys.TriggerCommand(key, modifierKeys);
    }

    /// <summary>
    /// Factory helpers for creating keyboard-related test doubles.
    /// </summary>
    public static class KeyboardHandlerHelper
    {
        /// <summary>
        /// Creates a <see cref="TestableKeyboardHandler"/> wired to the supplied
        /// <paramref name="action"/> through a fresh <see cref="ActionHotkeyHandler"/>.
        /// </summary>
        public static IKeyboardHandler CreateForAction(MenuAction action)
        {
            var hotkeyHandler = new ActionHotkeyHandler();
            hotkeyHandler.Register(action);
            return new TestableKeyboardHandler(hotkeyHandler);
        }
    }
}
