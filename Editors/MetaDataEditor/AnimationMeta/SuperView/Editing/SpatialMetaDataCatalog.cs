using System.Reflection;
using Microsoft.Xna.Framework;
using Shared.GameFormats.AnimationMeta.Definitions;
using Shared.GameFormats.AnimationMeta.Parsing;

namespace Editors.AnimationMeta.SuperView.Editing;

public enum SpatialMetaDataKind
{
    Effect,
    Prop,
    Blood,
    CameraShake,
    CrewLocation,
    SoundTrigger,
    SoundBuilding,
    Transform
}

public sealed class SpatialMetaDataBinding
{
    private readonly Func<Vector3> _getPosition;
    private readonly Action<Vector3> _setPosition;
    private readonly Func<Quaternion>? _getOrientation;
    private readonly Action<Quaternion>? _setOrientation;
    private readonly Func<int?>? _getBoneIndex;

    public ParsedMetadataAttribute Source { get; }
    public SpatialMetaDataKind Kind { get; }
    public string PositionPropertyName { get; }
    public string? OrientationPropertyName { get; }
    public string? AttachmentBoneName { get; }
    public bool AttachToBone { get; }
    public bool UsesGenericMarker =>
        (Kind == SpatialMetaDataKind.Prop &&
            Source is AnimatedProp_v0 or AnimatedProp_v2 &&
            Source is not IAnimatedPropMeta) ||
        Kind is
            SpatialMetaDataKind.Blood or
            SpatialMetaDataKind.CameraShake or
            SpatialMetaDataKind.CrewLocation or
            SpatialMetaDataKind.SoundTrigger or
            SpatialMetaDataKind.SoundBuilding or
            SpatialMetaDataKind.Transform;
    public bool CanRotate =>
        _getOrientation != null && _setOrientation != null;
    public int? BoneIndex => _getBoneIndex?.Invoke();
    public Vector3 Position
    {
        get => _getPosition();
        set => _setPosition(value);
    }
    public Quaternion? Orientation
    {
        get => _getOrientation?.Invoke();
        set
        {
            if (value.HasValue)
                _setOrientation?.Invoke(value.Value);
        }
    }

    public SpatialMetaDataBinding(
        ParsedMetadataAttribute source,
        SpatialMetaDataKind kind,
        string positionPropertyName,
        Func<Vector3> getPosition,
        Action<Vector3> setPosition,
        string? orientationPropertyName = null,
        Func<Quaternion>? getOrientation = null,
        Action<Quaternion>? setOrientation = null,
        Func<int?>? getBoneIndex = null,
        string? attachmentBoneName = null,
        bool attachToBone = false)
    {
        Source = source;
        Kind = kind;
        PositionPropertyName = positionPropertyName;
        OrientationPropertyName = orientationPropertyName;
        _getPosition = getPosition;
        _setPosition = setPosition;
        _getOrientation = getOrientation;
        _setOrientation = setOrientation;
        _getBoneIndex = getBoneIndex;
        AttachmentBoneName = attachmentBoneName;
        AttachToBone = attachToBone;
    }
}

public static class SpatialMetaDataCatalog
{
    public static bool TryCreate(
        ParsedMetadataAttribute source,
        out SpatialMetaDataBinding binding)
    {
        if (source is IEffectMeta effect)
        {
            binding = new SpatialMetaDataBinding(
                source,
                SpatialMetaDataKind.Effect,
                nameof(IEffectMeta.Position),
                () => effect.Position,
                value => effect.Position = value,
                nameof(IEffectMeta.Orientation),
                () => new Quaternion(effect.Orientation),
                value => effect.Orientation = value.ToVector4(),
                () => effect.NodeIndex,
                attachToBone: effect.Tracking);
            return true;
        }

        if (source is
            Prop_v2 or
            Prop_v10 or
            Prop_v14 or
            Prop_v15 or
            Prop_v12_3K or
            AnimatedProp_v0 or
            AnimatedProp_v2)
        {
            return TryCreateReflected(
                source,
                SpatialMetaDataKind.Prop,
                nameof(IEffectMeta.Position),
                nameof(IEffectMeta.Orientation),
                "BoneId",
                attachToBone: true,
                out binding);
        }

        if (source is Blood_v5 or Blood_v11)
        {
            var attachToBone = source is Blood_v5 ||
                ReadBoolean(source, "Tracking");
            return TryCreateReflected(
                source,
                SpatialMetaDataKind.Blood,
                "Position",
                "Orientation",
                "NodeIndex",
                attachToBone,
                out binding);
        }

        if (source is CameraShakePos)
        {
            return TryCreateReflected(
                source,
                SpatialMetaDataKind.CameraShake,
                "Position",
                null,
                null,
                false,
                out binding);
        }

        if (source is CrewLocation_v2 or CrewLocation_v10)
        {
            return TryCreateReflected(
                source,
                SpatialMetaDataKind.CrewLocation,
                "Position",
                "Orientation",
                null,
                false,
                out binding);
        }

        if (source is SoundTrigger_v4 or SoundTrigger_v10)
        {
            return TryCreateReflected(
                source,
                SpatialMetaDataKind.SoundTrigger,
                "Position",
                null,
                "BoneIndex",
                true,
                out binding);
        }

        if (source is SoundBuilding_v2)
        {
            return TryCreateReflected(
                source,
                SpatialMetaDataKind.SoundBuilding,
                "Position",
                null,
                null,
                false,
                out binding);
        }

        if (source is Transform_v10)
        {
            return TryCreateReflected(
                source,
                SpatialMetaDataKind.Transform,
                "Position",
                "Orientation",
                "TargetNode",
                true,
                out binding);
        }

        binding = null!;
        return false;
    }

    public static bool IsPositionProperty(
        ParsedMetadataAttribute source,
        string propertyName) =>
        TryCreate(source, out var binding) &&
        binding.PositionPropertyName == propertyName;

    private static bool TryCreateReflected(
        ParsedMetadataAttribute source,
        SpatialMetaDataKind kind,
        string positionPropertyName,
        string? orientationPropertyName,
        string? boneIndexPropertyName,
        bool attachToBone,
        out SpatialMetaDataBinding binding)
    {
        var position = FindTaggedProperty(source, positionPropertyName);
        if (position?.PropertyType != typeof(Vector3))
        {
            binding = null!;
            return false;
        }

        var orientation = orientationPropertyName == null
            ? null
            : FindTaggedProperty(source, orientationPropertyName);
        var boneIndex = boneIndexPropertyName == null
            ? null
            : FindTaggedProperty(source, boneIndexPropertyName);

        binding = new SpatialMetaDataBinding(
            source,
            kind,
            positionPropertyName,
            () => (Vector3)position.GetValue(source)!,
            value => position.SetValue(source, value),
            orientation?.Name,
            orientation?.PropertyType == typeof(Vector4)
                ? () => new Quaternion((Vector4)orientation.GetValue(source)!)
                : null,
            orientation?.PropertyType == typeof(Vector4)
                ? value => orientation.SetValue(source, value.ToVector4())
                : null,
            boneIndex?.PropertyType == typeof(int)
                ? () => (int)boneIndex.GetValue(source)!
                : null,
            attachToBone: attachToBone);
        return true;
    }

    private static PropertyInfo? FindTaggedProperty(
        ParsedMetadataAttribute source,
        string propertyName) =>
        source.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property =>
                property.Name == propertyName &&
                property.CanRead &&
                property.CanWrite &&
                property.GetCustomAttribute<MetaDataTagAttribute>(false) != null)
            .OrderByDescending(property =>
                GetInheritanceDepth(property.DeclaringType))
            .FirstOrDefault();

    private static bool ReadBoolean(
        ParsedMetadataAttribute source,
        string propertyName)
    {
        var property = FindTaggedProperty(source, propertyName);
        return property?.PropertyType == typeof(bool) &&
            (bool)property.GetValue(source)!;
    }

    private static int GetInheritanceDepth(Type? type)
    {
        var depth = 0;
        while (type != null)
        {
            depth++;
            type = type.BaseType;
        }
        return depth;
    }
}
