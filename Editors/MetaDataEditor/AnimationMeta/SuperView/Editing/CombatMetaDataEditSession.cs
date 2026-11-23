using Microsoft.Xna.Framework;
using Shared.GameFormats.AnimationMeta.Definitions;
using Shared.GameFormats.AnimationMeta.Parsing;

namespace Editors.AnimationMeta.SuperView.Editing;

public enum CombatMetaDataPoint
{
    Position,
    SplashStart,
    SplashEnd
}

public enum CombatMetaDataTransformMode
{
    Translate,
    Rotate
}

public enum CombatMetaDataEditChangeKind
{
    Preview,
    Committed,
    Cancelled,
    Undone,
    Redone
}

public sealed record CombatMetaDataEditChange(
    object Owner,
    ParsedMetadataAttribute Source,
    CombatMetaDataPoint Point,
    CombatMetaDataEditChangeKind Kind);

// One tag point the 3D view is editing, and each META document's own undo and redo of those edits. A drag
// previews on every step, is recorded as one step on release, and cancelled puts the values back as they were.
public sealed class CombatMetaDataEditSession
{
    private readonly Dictionary<object, DocumentHistory> _histories =
        new(ReferenceEqualityComparer.Instance);

    private object? _currentOwner;
    private PointAccessor? _target;
    private bool _gestureActive;
    private CombatMetaDataTransformMode _gestureMode;
    private Vector3 _gestureStart;
    private Quaternion _gestureOrientationStart;

    public event Action<CombatMetaDataEditChange>? ValueChanged;
    public event Action? HistoryChanged;

    public bool CanUndo =>
        _currentOwner != null &&
        _histories.TryGetValue(_currentOwner, out var history) &&
        history.Undo.Count != 0;

    public bool CanRedo =>
        _currentOwner != null &&
        _histories.TryGetValue(_currentOwner, out var history) &&
        history.Redo.Count != 0;

    public bool IsGestureActive => _gestureActive;
    public Vector3? CurrentPosition => _target?.Get();
    public Quaternion? CurrentOrientation => _target?.GetOrientation();
    public Vector3? CurrentWorldPosition => _target?.GetWorldPosition();
    public Quaternion? CurrentWorldOrientation =>
        _target?.GetWorldOrientation();
    public bool CanRotate => _target?.CanRotate == true;
    public CombatMetaDataPoint? CurrentPoint => _target?.Point;
    public ParsedMetadataAttribute? CurrentSource => _target?.Source;

    public static bool CanEdit(ParsedMetadataAttribute source) =>
        PointAccessor.TryCreate(source, CombatMetaDataPoint.Position) != null ||
        PointAccessor.TryCreate(source, CombatMetaDataPoint.SplashStart) != null ||
        PointAccessor.TryCreate(source, CombatMetaDataPoint.SplashEnd) != null;

    public bool SetTarget(
        object owner,
        ParsedMetadataAttribute source,
        CombatMetaDataPoint point,
        Func<Matrix>? referenceWorldTransform = null)
    {
        CancelGesture();
        _currentOwner = owner;
        _target = PointAccessor.TryCreate(source, point);
        _target?.SetTransformSpace(referenceWorldTransform);
        HistoryChanged?.Invoke();
        return _target != null;
    }

    public void ClearTarget(object? owner = null)
    {
        CancelGesture();
        _currentOwner = owner;
        _target = null;
        HistoryChanged?.Invoke();
    }

    public bool BeginGesture(
        CombatMetaDataTransformMode mode = CombatMetaDataTransformMode.Translate)
    {
        if (_target == null ||
            _gestureActive ||
            (mode == CombatMetaDataTransformMode.Rotate && !_target.CanRotate))
        {
            return false;
        }

        _gestureStart = _target.Get();
        _gestureOrientationStart =
            _target.GetOrientation() ?? Quaternion.Identity;
        _gestureMode = mode;
        _gestureActive = true;
        return true;
    }

    public bool Translate(Vector3 delta)
    {
        if (_target == null ||
            !_gestureActive ||
            _gestureMode != CombatMetaDataTransformMode.Translate)
        {
            return false;
        }

        _target.Set(_target.Get() + _target.ToLocalTranslation(delta));
        PublishChange(CombatMetaDataEditChangeKind.Preview);
        return true;
    }

    public bool Rotate(Matrix delta)
    {
        if (_target == null ||
            !_gestureActive ||
            _gestureMode != CombatMetaDataTransformMode.Rotate ||
            !_target.CanRotate)
        {
            return false;
        }

        var current = Matrix.CreateFromQuaternion(_target.GetOrientation()!.Value);
        var localDelta = _target.ToLocalRotationDelta(delta);
        var next = Quaternion.CreateFromRotationMatrix(current * localDelta);
        next.Normalize();
        _target.SetOrientation(next);
        PublishChange(CombatMetaDataEditChangeKind.Preview);
        return true;
    }

    public bool EndGesture()
    {
        if (_target == null || _currentOwner == null || !_gestureActive)
            return false;

        _gestureActive = false;
        var target = _target;
        var owner = _currentOwner;
        Edit edit;
        if (_gestureMode == CombatMetaDataTransformMode.Rotate)
        {
            var previous = _gestureOrientationStart;
            var next = target.GetOrientation()!.Value;
            if (next == previous)
                return false;
            edit = new Edit(
                () => Apply(owner, target, () => target.SetOrientation(previous), CombatMetaDataEditChangeKind.Undone),
                () => Apply(owner, target, () => target.SetOrientation(next), CombatMetaDataEditChangeKind.Redone));
        }
        else
        {
            var previous = _gestureStart;
            var next = target.Get();
            if (next == previous)
                return false;
            edit = new Edit(
                () => Apply(owner, target, () => target.Set(previous), CombatMetaDataEditChangeKind.Undone),
                () => Apply(owner, target, () => target.Set(next), CombatMetaDataEditChangeKind.Redone));
        }

        var history = GetHistory(owner);
        history.Undo.Push(edit);
        history.Redo.Clear();
        PublishChange(CombatMetaDataEditChangeKind.Committed);
        HistoryChanged?.Invoke();
        return true;
    }

    public void CancelGesture()
    {
        if (_target == null || !_gestureActive)
            return;

        _gestureActive = false;
        if (_gestureMode == CombatMetaDataTransformMode.Rotate)
            _target.SetOrientation(_gestureOrientationStart);
        else
            _target.Set(_gestureStart);
        PublishChange(CombatMetaDataEditChangeKind.Cancelled);
    }

    public bool Undo()
    {
        if (_currentOwner == null || _gestureActive)
            return false;
        if (!_histories.TryGetValue(_currentOwner, out var history) || history.Undo.Count == 0)
            return false;

        var edit = history.Undo.Pop();
        history.Redo.Push(edit);
        edit.Undo();
        HistoryChanged?.Invoke();
        return true;
    }

    public bool Redo()
    {
        if (_currentOwner == null || _gestureActive)
            return false;
        if (!_histories.TryGetValue(_currentOwner, out var history) || history.Redo.Count == 0)
            return false;

        var edit = history.Redo.Pop();
        history.Undo.Push(edit);
        edit.Redo();
        HistoryChanged?.Invoke();
        return true;
    }

    // The document's tags were replaced (reloaded, or added, deleted or moved): what its history points at is gone.
    public void ResetDocument(object owner)
    {
        if (ReferenceEquals(_currentOwner, owner))
            ClearTarget(owner);

        _histories.Remove(owner);
        HistoryChanged?.Invoke();
    }

    private DocumentHistory GetHistory(object owner)
    {
        if (!_histories.TryGetValue(owner, out var history))
            _histories.Add(owner, history = new DocumentHistory());
        return history;
    }

    private void Apply(
        object owner,
        PointAccessor target,
        Action apply,
        CombatMetaDataEditChangeKind kind)
    {
        apply();
        ValueChanged?.Invoke(new CombatMetaDataEditChange(owner, target.Source, target.Point, kind));
    }

    private void PublishChange(CombatMetaDataEditChangeKind kind)
    {
        if (_currentOwner == null || _target == null)
            return;

        ValueChanged?.Invoke(new CombatMetaDataEditChange(_currentOwner, _target.Source, _target.Point, kind));
    }

    private sealed record Edit(Action Undo, Action Redo);

    private sealed class DocumentHistory
    {
        public Stack<Edit> Undo { get; } = new();
        public Stack<Edit> Redo { get; } = new();
    }

    private sealed class PointAccessor
    {
        private readonly Func<Vector3> _get;
        private readonly Action<Vector3> _set;
        private readonly Func<Quaternion>? _getOrientation;
        private readonly Action<Quaternion>? _setOrientation;
        private MetaDataTransformSpace _transformSpace = new();

        public ParsedMetadataAttribute Source { get; }
        public CombatMetaDataPoint Point { get; }

        private PointAccessor(
            ParsedMetadataAttribute source,
            CombatMetaDataPoint point,
            Func<Vector3> get,
            Action<Vector3> set,
            Func<Quaternion>? getOrientation = null,
            Action<Quaternion>? setOrientation = null)
        {
            Source = source;
            Point = point;
            _get = get;
            _set = set;
            _getOrientation = getOrientation;
            _setOrientation = setOrientation;
        }

        public bool CanRotate =>
            _getOrientation != null && _setOrientation != null;
        public Vector3 Get() => _get();
        public void Set(Vector3 value) => _set(value);
        public Quaternion? GetOrientation() => _getOrientation?.Invoke();
        public void SetOrientation(Quaternion value) =>
            _setOrientation?.Invoke(value);
        public Vector3 GetWorldPosition() =>
            _transformSpace.ToWorldPosition(Get());
        public Quaternion? GetWorldOrientation() =>
            _transformSpace.ToWorldOrientation(
                GetOrientation() ?? Quaternion.Identity);
        public Vector3 ToLocalTranslation(Vector3 worldTranslation) =>
            _transformSpace.ToLocalTranslation(worldTranslation);
        public Matrix ToLocalRotationDelta(Matrix worldRotationDelta) =>
            _transformSpace.ToLocalRotationDelta(worldRotationDelta);
        public void SetTransformSpace(Func<Matrix>? referenceWorldTransform) =>
            _transformSpace = new MetaDataTransformSpace(referenceWorldTransform);

        public static PointAccessor? TryCreate(
            ParsedMetadataAttribute source,
            CombatMetaDataPoint point)
        {
            PointAccessor? accessor = (source, point) switch
            {
                (ImpactPosition_v2 value, CombatMetaDataPoint.Position) =>
                    new(source, point, () => value.Position, position => value.Position = position),
                (ImpactPosition_v10 value, CombatMetaDataPoint.Position) =>
                    new(source, point, () => value.Position, position => value.Position = position),
                (TargetPos_0 value, CombatMetaDataPoint.Position) =>
                    new(source, point, () => value.Position, position => value.Position = position),
                (TargetPos_10 value, CombatMetaDataPoint.Position) =>
                    new(source, point, () => value.Position, position => value.Position = position),
                (FirePos_v0 value, CombatMetaDataPoint.Position) =>
                    new(source, point, () => value.Position, position => value.Position = position),
                (FirePos_v2 value, CombatMetaDataPoint.Position) =>
                    new(source, point, () => value.Position, position => value.Position = position),
                (FirePos_v10 value, CombatMetaDataPoint.Position) =>
                    new(source, point, () => value.Position, position => value.Position = position),
                (SplashAttack_v3 value, CombatMetaDataPoint.SplashStart) =>
                    new(source, point, () => value.StartPosition, position => value.StartPosition = position),
                (SplashAttack_v3 value, CombatMetaDataPoint.SplashEnd) =>
                    new(source, point, () => value.EndPosition, position => value.EndPosition = position),
                (SplashAttack_v10 value, CombatMetaDataPoint.SplashStart) =>
                    new(source, point, () => value.StartPosition, position => value.StartPosition = position),
                (SplashAttack_v10 value, CombatMetaDataPoint.SplashEnd) =>
                    new(source, point, () => value.EndPosition, position => value.EndPosition = position),
                (IEffectMeta value, CombatMetaDataPoint.Position) =>
                    new(
                        source,
                        point,
                        () => value.Position,
                        position => value.Position = position,
                        () => new Quaternion(value.Orientation),
                        orientation => value.Orientation = orientation.ToVector4()),
                _ => null
            };

            if (accessor != null ||
                point != CombatMetaDataPoint.Position ||
                !SpatialMetaDataCatalog.TryCreate(source, out var binding))
            {
                return accessor;
            }

            return new PointAccessor(
                source,
                point,
                () => binding.Position,
                value => binding.Position = value,
                binding.CanRotate ? () => binding.Orientation!.Value : null,
                binding.CanRotate ? value => binding.Orientation = value : null);
        }
    }
}
