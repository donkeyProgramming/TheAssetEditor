using Editors.AnimationMeta.SuperView.Visualisation;
using GameWorld.Core.Components;
using GameWorld.Core.Components.Input;
using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Shared.GameFormats.AnimationMeta.Parsing;

namespace Editors.AnimationMeta.SuperView.Editing
{
    public sealed record MetaDataMarkerPickCandidate(IMetaDataMarkerPreview Preview, int StableOrder);

    public sealed record MetaDataMarkerHit(MetaDataMarkerPickCandidate Candidate, MetaDataMarkerHitTarget Target);

    // The marker point nearest the pointer's ray: closest to the ray first, then nearest the camera, then the tag's
    // order. Far markers get a little angular slack in perspective, so they stay clickable when small on screen.
    public static class MetaDataMarkerHitTester
    {
        const float MinimumAngularRadius = 0.008f;

        public static MetaDataMarkerHit? Pick(Ray ray, IEnumerable<MetaDataMarkerPickCandidate> candidates, bool useAngularTolerance)
        {
            MetaDataMarkerHit? bestHit = null;
            var bestDistanceToRaySquared = float.MaxValue;
            var bestDistanceAlongRay = float.MaxValue;
            var bestStableOrder = int.MaxValue;
            var bestTargetOrder = int.MaxValue;

            foreach (var candidate in candidates)
            {
                if (!candidate.Preview.IsHitTestVisible)
                    continue;

                var hitTargets = candidate.Preview.HitTargets;
                for (var targetOrder = 0; targetOrder < hitTargets.Count; targetOrder++)
                {
                    var target = hitTargets[targetOrder];
                    var position = target.Position;
                    if (!IsFinite(position))
                        continue;

                    var distanceAlongRay = Vector3.Dot(position - ray.Position, ray.Direction);
                    if (distanceAlongRay < 0)
                        continue;

                    var closestPoint = ray.Position + ray.Direction * distanceAlongRay;
                    var distanceToRaySquared = Vector3.DistanceSquared(position, closestPoint);
                    var radius = target.HitTestRadius ?? candidate.Preview.HitTestRadius;
                    if (useAngularTolerance)
                        radius = Math.Max(radius, distanceAlongRay * MinimumAngularRadius);

                    if (distanceToRaySquared > radius * radius ||
                        !IsBetter(distanceToRaySquared, distanceAlongRay, candidate.StableOrder, targetOrder,
                            bestDistanceToRaySquared, bestDistanceAlongRay, bestStableOrder, bestTargetOrder))
                    {
                        continue;
                    }

                    bestHit = new MetaDataMarkerHit(candidate, target);
                    bestDistanceToRaySquared = distanceToRaySquared;
                    bestDistanceAlongRay = distanceAlongRay;
                    bestStableOrder = candidate.StableOrder;
                    bestTargetOrder = targetOrder;
                }
            }

            return bestHit;
        }

        static bool IsFinite(Vector3 value) => float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);

        static bool IsBetter(float distanceToRaySquared, float distanceAlongRay, int stableOrder, int targetOrder,
            float bestDistanceToRaySquared, float bestDistanceAlongRay, int bestStableOrder, int bestTargetOrder)
        {
            var distanceComparison = distanceToRaySquared.CompareTo(bestDistanceToRaySquared);
            if (distanceComparison != 0)
                return distanceComparison < 0;

            var depthComparison = distanceAlongRay.CompareTo(bestDistanceAlongRay);
            if (depthComparison != 0)
                return depthComparison < 0;

            var stableOrderComparison = stableOrder.CompareTo(bestStableOrder);
            if (stableOrderComparison != 0)
                return stableOrderComparison < 0;

            return targetOrder < bestTargetOrder;
        }
    }

    public enum MetaDataMarkerPointerActionKind
    {
        None,
        ClearSelection,
        Select,
        Focus,
    }

    public readonly record struct MetaDataMarkerPointerAction(MetaDataMarkerPointerActionKind Kind, MetaDataMarkerHit? Hit)
    {
        public static MetaDataMarkerPointerAction None => new(MetaDataMarkerPointerActionKind.None, null);
    }

    // Press and release in the same place is a click: on a marker it selects, a second click on the same point within
    // half a second focuses the camera on it, and on nothing it clears the selection. A drag is not a click.
    public sealed class MetaDataMarkerPointerInteraction
    {
        const float MaximumClickMovementSquared = 16;
        static readonly TimeSpan s_doubleClickInterval = TimeSpan.FromMilliseconds(500);

        bool _pressActive;
        Vector2 _pressPosition;
        TimeSpan _lastClickTime;
        ParsedMetadataAttribute? _lastClickSource;
        MetaDataMarkerPoint _lastClickPoint;

        public MetaDataMarkerPointerAction Update(TimeSpan currentTime, Vector2 pointerPosition, bool isPressed, bool isReleased, bool isBlocked, MetaDataMarkerHit? hit)
        {
            if (isBlocked)
            {
                Reset();
                return MetaDataMarkerPointerAction.None;
            }

            if (isPressed)
            {
                _pressActive = true;
                _pressPosition = pointerPosition;
            }

            if (!isReleased || !_pressActive)
                return MetaDataMarkerPointerAction.None;

            _pressActive = false;
            if (Vector2.DistanceSquared(_pressPosition, pointerPosition) > MaximumClickMovementSquared)
            {
                _lastClickSource = null;
                return MetaDataMarkerPointerAction.None;
            }

            if (hit == null)
            {
                _lastClickSource = null;
                return new MetaDataMarkerPointerAction(MetaDataMarkerPointerActionKind.ClearSelection, null);
            }

            var source = hit.Candidate.Preview.Source;
            var elapsed = currentTime - _lastClickTime;
            if (ReferenceEquals(_lastClickSource, source) &&
                _lastClickPoint == hit.Target.Point &&
                elapsed >= TimeSpan.Zero &&
                elapsed <= s_doubleClickInterval)
            {
                _lastClickSource = null;
                return new MetaDataMarkerPointerAction(MetaDataMarkerPointerActionKind.Focus, hit);
            }

            _lastClickSource = source;
            _lastClickPoint = hit.Target.Point;
            _lastClickTime = currentTime;
            return new MetaDataMarkerPointerAction(MetaDataMarkerPointerActionKind.Select, hit);
        }

        public void Reset()
        {
            _pressActive = false;
            _lastClickSource = null;
        }
    }

    /// <summary>
    /// Hover, click and double-click on SuperView's META markers. It runs before the stock SelectionComponent and holds
    /// the mouse from a press to its release, so the click is not also a mesh selection. A press on a gizmo handle, a
    /// gizmo drag and Alt (the camera) are left alone.
    /// </summary>
    public sealed class MetaDataMarkerPickerComponent : BaseComponent, IDisposable
    {
        static readonly Color s_hoverColour = Color.Orange;

        readonly ArcBallCamera _camera;
        readonly IMouseComponent _mouse;
        readonly IKeyboardComponent _keyboard;
        readonly RenderEngineComponent _renderEngine;
        readonly CombatMetaDataGizmoComponent _gizmo;
        readonly MetaDataMarkerPointerInteraction _interaction = new();

        Func<IReadOnlyList<IMetaDataMarkerPreview>>? _candidateProvider;
        Action<ParsedMetadataAttribute, MetaDataMarkerPoint>? _select;
        Action<ParsedMetadataAttribute>? _focus;
        Action? _clearSelection;
        MetaDataMarkerHit? _hovered;

        public MetaDataMarkerPickerComponent(ArcBallCamera camera, IMouseComponent mouse, IKeyboardComponent keyboard, RenderEngineComponent renderEngine, CombatMetaDataGizmoComponent gizmo)
        {
            _camera = camera;
            _mouse = mouse;
            _keyboard = keyboard;
            _renderEngine = renderEngine;
            _gizmo = gizmo;
            // Before SelectionComponent, which takes the mouse on any press it sees first.
            UpdateOrder = (int)ComponentUpdateOrderEnum.Gizmo;
        }

        public void Connect(Func<IReadOnlyList<IMetaDataMarkerPreview>> candidateProvider, Action<ParsedMetadataAttribute, MetaDataMarkerPoint> select, Action<ParsedMetadataAttribute> focus, Action clearSelection)
        {
            _candidateProvider = candidateProvider;
            _select = select;
            _focus = focus;
            _clearSelection = clearSelection;
        }

        public void Disconnect()
        {
            ClearInteractionState();
            _candidateProvider = null;
            _select = null;
            _focus = null;
            _clearSelection = null;
        }

        public void ClearInteractionState()
        {
            _hovered = null;
            _interaction.Reset();
            if (_mouse.MouseOwner == this)
                _mouse.MouseOwner = null;
        }

        public override void Update(GameTime gameTime)
        {
            if (_candidateProvider == null)
                return;

            var owner = _mouse.MouseOwner;
            var isBlocked = _gizmo.IsDragActive || _gizmo.IsHandleHovered || _keyboard.IsKeyDown(Keys.LeftAlt) || (owner != null && owner != this);
            var position = _mouse.Position();
            var candidates = _candidateProvider().Select((preview, index) => new MetaDataMarkerPickCandidate(preview, index));
            var hit = isBlocked ? null : MetaDataMarkerHitTester.Pick(_camera.CreateCameraRay(position), candidates, _camera.CurrentProjectionType == ProjectionType.Perspective);
            _hovered = hit;
            DrawHover();

            var isPressed = _mouse.IsMouseButtonPressed(MouseButton.Left);
            var isReleased = _mouse.IsMouseButtonReleased(MouseButton.Left);
            if (!isBlocked && isPressed && owner == null)
                _mouse.MouseOwner = this;

            var action = _interaction.Update(gameTime.TotalGameTime, position, isPressed, isReleased, isBlocked, hit);

            // The release ends the click: the mouse goes back, and the release is not seen again as a selection click.
            if (_mouse.MouseOwner == this && !_mouse.IsMouseButtonDown(MouseButton.Left))
            {
                _mouse.MouseOwner = null;
                _mouse.ClearStates();
            }

            switch (action.Kind)
            {
                case MetaDataMarkerPointerActionKind.ClearSelection:
                    _clearSelection?.Invoke();
                    break;
                case MetaDataMarkerPointerActionKind.Select:
                    _select?.Invoke(action.Hit!.Candidate.Preview.Source, action.Hit.Target.Point);
                    break;
                case MetaDataMarkerPointerActionKind.Focus:
                    _focus?.Invoke(action.Hit!.Candidate.Preview.Source);
                    break;
            }
        }

        // An orange ring and cross on the hovered point, drawn this frame.
        void DrawHover()
        {
            if (_hovered is not { } hit)
                return;
            var radius = hit.Target.HitTestRadius ?? hit.Candidate.Preview.HitTestRadius;
            _renderEngine.AddRenderLines(LineHelper.AddCircle(hit.Target.Position, radius, s_hoverColour));
            _renderEngine.AddRenderLines(LineHelper.AddLocator(hit.Target.Position, radius, s_hoverColour));
        }

        public void Dispose() => Disconnect();
    }
}
