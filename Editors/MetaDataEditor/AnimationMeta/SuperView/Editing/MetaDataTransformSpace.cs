using Microsoft.Xna.Framework;

namespace Editors.AnimationMeta.SuperView.Editing;

public sealed class MetaDataTransformSpace
{
    private readonly Func<Matrix> _referenceWorldTransform;

    public MetaDataTransformSpace(Func<Matrix>? referenceWorldTransform = null)
    {
        _referenceWorldTransform = referenceWorldTransform ??
            (() => Matrix.Identity);
    }

    public Vector3 ToWorldPosition(Vector3 localPosition) =>
        Vector3.Transform(localPosition, _referenceWorldTransform());

    public Quaternion ToWorldOrientation(Quaternion localOrientation)
    {
        var worldRotation = Matrix.CreateFromQuaternion(localOrientation) *
            GetReferenceRotation();
        var result = Quaternion.CreateFromRotationMatrix(worldRotation);
        result.Normalize();
        return result;
    }

    public Vector3 ToLocalTranslation(Vector3 worldTranslation) =>
        Vector3.TransformNormal(
            worldTranslation,
            Matrix.Invert(_referenceWorldTransform()));

    public Matrix ToLocalRotationDelta(Matrix worldRotationDelta)
    {
        var referenceRotation = GetReferenceRotation();
        return referenceRotation *
            worldRotationDelta *
            Matrix.Invert(referenceRotation);
    }

    private Matrix GetReferenceRotation()
    {
        var reference = _referenceWorldTransform();
        if (!reference.Decompose(out _, out var rotation, out _))
            return Matrix.Identity;

        rotation.Normalize();
        return Matrix.CreateFromQuaternion(rotation);
    }
}
