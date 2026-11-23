using System.Reflection;
using Editors.CscEditor.Data;
using Editors.CscEditor.Services;
using GameWorld.Core.Components;
using GameWorld.Core.Components.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Test.CscEditor
{
    /// <summary>The gizmo's drag bracketing (DragStarted/DragCompleted, one undo step per drag),
    /// driven through the handlers the low-level Gizmo calls, without a graphics device.</summary>
    public class CscGizmoDragTests
    {
        sealed class Harness
        {
            public IGameComponent? Owner;
            public ButtonState LeftButton = ButtonState.Pressed;
            public int Started;
            public int Completed;
            public CscGizmoComponent Gizmo { get; }

            public Harness()
            {
                var mouse = MouseProxy.Create(this);
                Gizmo = new CscGizmoComponent(null!, mouse, null!, null!, null!, null!, null!, null!);
                Gizmo.DragStarted += () => Started++;
                Gizmo.DragCompleted += () => Completed++;
            }

            public void Invoke(string handler) =>
                typeof(CscGizmoComponent).GetMethod(handler, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(Gizmo, null);
        }

        public class MouseProxy : DispatchProxy
        {
            object? _harness;

            internal static IMouseComponent Create(object harness)
            {
                var proxy = Create<IMouseComponent, MouseProxy>();
                ((MouseProxy)(object)proxy)._harness = harness;
                return proxy;
            }

            protected override object? Invoke(MethodInfo? method, object?[]? args)
            {
                var h = (Harness)_harness!;
                switch (method!.Name)
                {
                    case "get_MouseOwner": return h.Owner;
                    case "set_MouseOwner": h.Owner = (IGameComponent?)args![0]; return null;
                    case "State": return new MouseState(0, 0, 0, h.LeftButton, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
                    case "ClearStates": return null;
                    default: throw new NotSupportedException(method.Name);
                }
            }
        }

        static CscElement Locator() => new() { Kind = CscElementKind.Locator };

        [Test]
        public void A_drag_raises_started_and_completed_once_and_owns_the_mouse_meanwhile()
        {
            var h = new Harness();
            h.Gizmo.SetTarget(Locator());

            h.Invoke("OnDragStart");
            Assert.That(h.Gizmo.IsDragActive, Is.True);
            Assert.That(h.Owner, Is.SameAs(h.Gizmo));

            h.Invoke("OnDragEnd");
            h.Invoke("OnDragEnd");
            Assert.Multiple(() =>
            {
                Assert.That(h.Started, Is.EqualTo(1));
                Assert.That(h.Completed, Is.EqualTo(1));
                Assert.That(h.Gizmo.IsDragActive, Is.False);
                Assert.That(h.Owner, Is.Null);
            });
        }

        [Test]
        public void Changing_the_target_mid_drag_ends_the_step_and_ignores_the_rest_of_the_held_drag()
        {
            var h = new Harness();
            h.Gizmo.SetTarget(Locator());
            h.Invoke("OnDragStart");

            h.Gizmo.SetTarget(Locator());
            Assert.That(h.Completed, Is.EqualTo(1));
            Assert.That(h.Gizmo.IsDragActive, Is.False);

            // Still holding the button: the Gizmo widget keeps dragging, but nothing starts.
            h.Invoke("OnDragStart");
            Assert.That(h.Started, Is.EqualTo(1));

            // Released: the next press is a new drag.
            h.LeftButton = ButtonState.Released;
            h.Gizmo.Update(new GameTime());
            h.LeftButton = ButtonState.Pressed;
            h.Invoke("OnDragStart");
            Assert.That(h.Started, Is.EqualTo(2));
        }

        [Test]
        public void Losing_the_mouse_to_another_component_ends_the_drag()
        {
            var h = new Harness();
            h.Gizmo.SetTarget(Locator());
            h.Invoke("OnDragStart");

            h.Owner = null;
            h.Gizmo.Update(new GameTime());

            Assert.That(h.Completed, Is.EqualTo(1));
            Assert.That(h.Gizmo.IsDragActive, Is.False);
        }

        [Test]
        public void Turning_the_gizmo_off_mid_drag_ends_the_drag()
        {
            var h = new Harness();
            h.Gizmo.SetTarget(Locator());
            h.Invoke("OnDragStart");

            h.Gizmo.Disable();

            Assert.That(h.Completed, Is.EqualTo(1));
        }
    }
}
