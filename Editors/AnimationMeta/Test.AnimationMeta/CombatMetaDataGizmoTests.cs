using System.Reflection;
using Editors.AnimationMeta.SuperView.Editing;
using GameWorld.Core.Components;
using GameWorld.Core.Components.Gizmo;
using GameWorld.Core.Components.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Shared.GameFormats.AnimationMeta.Definitions;

namespace Test.AnimationMeta
{
    // The META gizmo's drags, driven through the handlers the low-level Gizmo calls, without a graphics device.
    public class CombatMetaDataGizmoTests
    {
        sealed class Harness
        {
            public IGameComponent? Owner;
            public CombatMetaDataEditSession Session { get; } = new();
            public CombatMetaDataGizmoComponent Gizmo { get; }
            public FirePos_v10 Fire { get; } = new() { Position = new Vector3(1, 2, 3) };

            public Harness()
            {
                Gizmo = new CombatMetaDataGizmoComponent(null!, MouseProxy.Create(this), null!, null!, null!, null!, Session) { IsEnabled = true };
                Session.SetTarget(new object(), Fire, CombatMetaDataPoint.Position);
            }

            public void Invoke(string handler, params object[] args) =>
                typeof(CombatMetaDataGizmoComponent).GetMethod(handler, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(Gizmo, args);

            public void Translate(Vector3 delta) => Invoke("OnTranslate", null!, new TransformationEventArgs(delta, PivotType.ObjectCenter));
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
                    case "State": return new MouseState(0, 0, 0, ButtonState.Pressed, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
                    case "ClearStates": return null;
                    default: throw new NotSupportedException(method.Name);
                }
            }
        }

        [Test]
        public void Drag_MovesTheTag_AndIsOneUndoStep()
        {
            var h = new Harness();

            h.Invoke("OnDragStart");
            Assert.That(h.Owner, Is.SameAs(h.Gizmo));
            h.Translate(new Vector3(1, 0, 0));
            h.Translate(new Vector3(1, 0, 0));
            h.Invoke("OnDragEnd");

            Assert.Multiple(() =>
            {
                Assert.That(h.Fire.Position, Is.EqualTo(new Vector3(3, 2, 3)));
                Assert.That(h.Owner, Is.Null);
                Assert.That(h.Gizmo.IsDragActive, Is.False);
            });
            Assert.That(h.Session.Undo(), Is.True);
            Assert.That(h.Fire.Position, Is.EqualTo(new Vector3(1, 2, 3)));
            Assert.That(h.Session.CanUndo, Is.False);
        }

        [Test]
        public void CancelledDrag_PutsTheTagBack_AndIgnoresTheRestOfTheHeldDrag()
        {
            var h = new Harness();

            h.Invoke("OnDragStart");
            h.Translate(new Vector3(1, 0, 0));
            h.Invoke("CancelDrag");
            h.Translate(new Vector3(1, 0, 0));
            h.Invoke("OnDragEnd");

            Assert.Multiple(() =>
            {
                Assert.That(h.Fire.Position, Is.EqualTo(new Vector3(1, 2, 3)));
                Assert.That(h.Session.CanUndo, Is.False);
                Assert.That(h.Owner, Is.Null);
            });
        }

        [Test]
        public void EndDrag_KeepsWhatTheDragDid_AndIgnoresTheRest()
        {
            var h = new Harness();

            h.Invoke("OnDragStart");
            h.Translate(new Vector3(1, 0, 0));
            h.Gizmo.EndDrag();
            h.Translate(new Vector3(1, 0, 0));
            h.Invoke("OnDragStart"); // the button is still held: no new drag starts

            Assert.Multiple(() =>
            {
                Assert.That(h.Fire.Position, Is.EqualTo(new Vector3(2, 2, 3)));
                Assert.That(h.Session.CanUndo, Is.True);
                Assert.That(h.Gizmo.IsDragActive, Is.False);
            });
        }

        [Test]
        public void Disabled_DoesNotStartADrag()
        {
            var h = new Harness();
            h.Gizmo.IsEnabled = false;

            h.Invoke("OnDragStart");
            h.Translate(new Vector3(1, 0, 0));

            Assert.That(h.Fire.Position, Is.EqualTo(new Vector3(1, 2, 3)));
            Assert.That(h.Owner, Is.Null);
        }

        [Test]
        public void RotateMode_OnAPositionOnlyTag_DoesNotStartADrag()
        {
            var h = new Harness();
            h.Gizmo.TransformMode = CombatMetaDataTransformMode.Rotate;

            h.Invoke("OnDragStart");

            Assert.That(h.Gizmo.IsDragActive, Is.False);
            Assert.That(h.Owner, Is.Null);
        }
    }
}
