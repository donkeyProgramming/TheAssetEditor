using System.Windows.Input;
using Shared.Ui.Common.MenuSystem;
using Test.TestingUtility.Keyboard;

namespace Test.KitbashEditor.Keyboard
{
    /// <summary>
    /// Regression tests for keyboard hotkey dispatch in the kitbash tool.
    ///
    /// Root cause guarded against: MenuBarView.HandleKeyDown previously invoked
    /// both OnKeyDown AND OnKeyReleased for the same event, causing hotkeys bound
    /// to key-release (e.g. spacebar / ToggleViewSelectedCommand) to fire twice —
    /// once during KeyDown and again during the real KeyUp — producing a flicker
    /// where state was toggled on then immediately toggled off again.
    ///
    /// Correct contract:
    ///   • OnKeyDown  → hotkeys are NOT triggered.
    ///   • OnKeyReleased → hotkeys ARE triggered exactly once per call.
    /// </summary>
    [TestFixture]
    public class MenuBarKeyboardHandlingTests
    {
        private int _triggerCount;
        private ActionHotkeyHandler _handler = null!;
        private MenuAction _spaceAction = null!;

        [SetUp]
        public void Setup()
        {
            _triggerCount = 0;
            _handler = new ActionHotkeyHandler();

            _spaceAction = new MenuAction
            {
                Hotkey = new Hotkey(Key.Space, ModifierKeys.None),
                ActionTriggeredCallback = () => _triggerCount++
            };

            _handler.Register(_spaceAction);
        }

        [Test]
        public void TriggerCommand_SpaceKey_IncrementsCountByOne()
        {
            _handler.TriggerCommand(Key.Space, ModifierKeys.None);

            Assert.That(_triggerCount, Is.EqualTo(1));
        }

        [Test]
        public void TriggerCommand_CalledTwice_IncrementsCountByTwo()
        {
            // Simulates the broken behaviour: if the caller invokes TriggerCommand
            // inside both KeyDown and KeyUp, the count would reach 2.
            _handler.TriggerCommand(Key.Space, ModifierKeys.None);
            _handler.TriggerCommand(Key.Space, ModifierKeys.None);

            Assert.That(_triggerCount, Is.EqualTo(2),
                "Calling TriggerCommand twice (old bug: once during KeyDown, once during KeyUp) fires the action twice.");
        }

        [Test]
        public void OnKeyDown_DoesNotTriggerHotkey()
        {
            // MenuBarViewModel.OnKeyDown is intentionally empty for hotkeys —
            // they are only triggered from OnKeyReleased.
            var vm = KeyboardHandlerHelper.CreateForAction(_spaceAction);
            vm.OnKeyDown(Key.Space, Key.Space, ModifierKeys.None);

            Assert.That(_triggerCount, Is.EqualTo(0),
                "OnKeyDown must not trigger hotkey actions.");
        }

        [Test]
        public void OnKeyReleased_TriggersHotkeyOnce()
        {
            var vm = KeyboardHandlerHelper.CreateForAction(_spaceAction);
            vm.OnKeyReleased(Key.Space, Key.Space, ModifierKeys.None);

            Assert.That(_triggerCount, Is.EqualTo(1),
                "OnKeyReleased must trigger the hotkey exactly once.");
        }

        [Test]
        public void OnKeyDown_ThenOnKeyReleased_TriggersHotkeyOnce()
        {
            // Simulates the normal key press sequence that the WPF window generates:
            // HandleKeyDown fires → HandleKeyUp fires.
            // The action must be triggered exactly once.
            var vm = KeyboardHandlerHelper.CreateForAction(_spaceAction);
            vm.OnKeyDown(Key.Space, Key.Space, ModifierKeys.None);
            vm.OnKeyReleased(Key.Space, Key.Space, ModifierKeys.None);

            Assert.That(_triggerCount, Is.EqualTo(1),
                "A full key press (KeyDown + KeyUp) must trigger the hotkey exactly once.");
        }
    }
}
