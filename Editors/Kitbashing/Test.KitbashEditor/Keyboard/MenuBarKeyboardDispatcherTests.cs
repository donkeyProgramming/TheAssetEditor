using System.Windows.Input;
using Editors.KitbasherEditor.Core.MenuBarViews;
using Shared.Ui.Common.MenuSystem;
using Test.TestingUtility.Keyboard;

namespace Test.KitbashEditor.Keyboard
{
    /// <summary>
    /// Tests for <see cref="MenuBarKeyboardDispatcher"/> which guards keyboard
    /// dispatch with a visibility check and TextBox-source filtering.
    ///
    /// Bug guarded against:
    /// 1. When multiple KitbasherEditor tabs are open, each MenuBarView subscribes
    ///    its handler to the main Window's KeyUp/KeyDown. Without the visibility
    ///    guard, background (non-visible) editors receive keyboard events and
    ///    dispatch commands against stale or disposed scopes.
    /// 2. When an editor tab is closed, the Unloaded event fires after the control
    ///    is disconnected from the visual tree. The old code called
    ///    Window.GetWindow(this) which returned null, so the handler was never
    ///    unsubscribed. The fix stores the window reference on Load.
    /// </summary>
    [TestFixture]
    public class MenuBarKeyboardDispatcherTests
    {
        private int _keyReleasedCount;
        private int _keyDownCount;
        private bool _isVisible;
        private IKeyboardHandler? _handler;
        private MenuBarKeyboardDispatcher _dispatcher = null!;

        [SetUp]
        public void Setup()
        {
            _keyReleasedCount = 0;
            _keyDownCount = 0;
            _isVisible = true;

            var hotkeyHandler = new ActionHotkeyHandler();
            hotkeyHandler.Register(new MenuAction
            {
                Hotkey = new Hotkey(Key.Delete, ModifierKeys.None),
                ActionTriggeredCallback = () => _keyReleasedCount++
            });

            _handler = new CountingKeyboardHandler(hotkeyHandler, () => _keyDownCount++);
            _dispatcher = new MenuBarKeyboardDispatcher(
                () => _isVisible,
                () => _handler);
        }

        [Test]
        public void HandleKeyUp_WhenVisible_DispatchesToHandler()
        {
            var result = _dispatcher.HandleKeyUp(Key.Delete, Key.Delete, ModifierKeys.None, isSourceTextBox: false);

            Assert.That(result, Is.True);
            Assert.That(_keyReleasedCount, Is.EqualTo(1));
        }

        [Test]
        public void HandleKeyUp_WhenNotVisible_DoesNotDispatch()
        {
            _isVisible = false;

            var result = _dispatcher.HandleKeyUp(Key.Delete, Key.Delete, ModifierKeys.None, isSourceTextBox: false);

            Assert.That(result, Is.False);
            Assert.That(_keyReleasedCount, Is.EqualTo(0));
        }

        [Test]
        public void HandleKeyDown_WhenVisible_DispatchesToHandler()
        {
            var result = _dispatcher.HandleKeyDown(Key.Delete, Key.Delete, ModifierKeys.None, isSourceTextBox: false);

            Assert.That(result, Is.True);
            Assert.That(_keyDownCount, Is.EqualTo(1));
        }

        [Test]
        public void HandleKeyDown_WhenNotVisible_DoesNotDispatch()
        {
            _isVisible = false;

            var result = _dispatcher.HandleKeyDown(Key.Delete, Key.Delete, ModifierKeys.None, isSourceTextBox: false);

            Assert.That(result, Is.False);
            Assert.That(_keyDownCount, Is.EqualTo(0));
        }

        [Test]
        public void HandleKeyUp_FromTextBoxWithNoModifiers_DoesNotDispatch()
        {
            var result = _dispatcher.HandleKeyUp(Key.Delete, Key.Delete, ModifierKeys.None, isSourceTextBox: true);

            Assert.That(result, Is.False);
            Assert.That(_keyReleasedCount, Is.EqualTo(0));
        }

        [Test]
        public void HandleKeyUp_FromTextBoxWithModifiers_Dispatches()
        {
            var hotkeyHandler = new ActionHotkeyHandler();
            hotkeyHandler.Register(new MenuAction
            {
                Hotkey = new Hotkey(Key.D, ModifierKeys.Control),
                ActionTriggeredCallback = () => _keyReleasedCount++
            });
            _handler = new CountingKeyboardHandler(hotkeyHandler, () => _keyDownCount++);
            _dispatcher = new MenuBarKeyboardDispatcher(() => _isVisible, () => _handler);

            var result = _dispatcher.HandleKeyUp(Key.D, Key.D, ModifierKeys.Control, isSourceTextBox: true);

            Assert.That(result, Is.True);
            Assert.That(_keyReleasedCount, Is.EqualTo(1));
        }

        [Test]
        public void HandleKeyDown_FromTextBoxWithNoModifiers_DoesNotDispatch()
        {
            var result = _dispatcher.HandleKeyDown(Key.A, Key.A, ModifierKeys.None, isSourceTextBox: true);

            Assert.That(result, Is.False);
            Assert.That(_keyDownCount, Is.EqualTo(0));
        }

        [Test]
        public void HandleKeyUp_WhenHandlerIsNull_DoesNotDispatch()
        {
            _handler = null;

            var result = _dispatcher.HandleKeyUp(Key.Delete, Key.Delete, ModifierKeys.None, isSourceTextBox: false);

            Assert.That(result, Is.False);
            Assert.That(_keyReleasedCount, Is.EqualTo(0));
        }

        [Test]
        public void HandleKeyDown_WhenHandlerIsNull_DoesNotDispatch()
        {
            _handler = null;

            var result = _dispatcher.HandleKeyDown(Key.Delete, Key.Delete, ModifierKeys.None, isSourceTextBox: false);

            Assert.That(result, Is.False);
            Assert.That(_keyDownCount, Is.EqualTo(0));
        }

        [Test]
        public void MultipleDispatchers_OnlyVisibleOneHandlesEvent()
        {
            // Simulates two editor tabs sharing the same window.
            var count1 = 0;
            var count2 = 0;

            var handler1 = CreateCountingHandler(() => count1++);
            var handler2 = CreateCountingHandler(() => count2++);

            var dispatcher1 = new MenuBarKeyboardDispatcher(() => false, () => handler1);  // Background tab
            var dispatcher2 = new MenuBarKeyboardDispatcher(() => true, () => handler2);   // Active tab

            dispatcher1.HandleKeyUp(Key.Delete, Key.Delete, ModifierKeys.None, isSourceTextBox: false);
            dispatcher2.HandleKeyUp(Key.Delete, Key.Delete, ModifierKeys.None, isSourceTextBox: false);

            Assert.That(count1, Is.EqualTo(0), "Background tab should NOT receive the key event.");
            Assert.That(count2, Is.EqualTo(1), "Active tab should receive the key event.");
        }

        private static IKeyboardHandler CreateCountingHandler(Action onKeyReleased)
        {
            var hotkeys = new ActionHotkeyHandler();
            hotkeys.Register(new MenuAction
            {
                Hotkey = new Hotkey(Key.Delete, ModifierKeys.None),
                ActionTriggeredCallback = onKeyReleased
            });
            return new TestableKeyboardHandler(hotkeys);
        }

        /// <summary>
        /// A keyboard handler that counts both OnKeyDown and OnKeyReleased invocations.
        /// </summary>
        private sealed class CountingKeyboardHandler : IKeyboardHandler
        {
            private readonly ActionHotkeyHandler _hotkeys;
            private readonly Action _onKeyDown;

            public CountingKeyboardHandler(ActionHotkeyHandler hotkeys, Action onKeyDown)
            {
                _hotkeys = hotkeys;
                _onKeyDown = onKeyDown;
            }

            public void OnKeyDown(Key key, Key systemKey, ModifierKeys modifiers) => _onKeyDown();

            public bool OnKeyReleased(Key key, Key systemKey, ModifierKeys modifierKeys)
                => _hotkeys.TriggerCommand(key, modifierKeys);
        }
    }
}
