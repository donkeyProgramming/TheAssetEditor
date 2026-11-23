using System.Numerics;
using Shared.GameFormats.Animation;
using Shared.GameFormats.RigidModel.Transforms;

namespace Editors.ImportExport.Importing.Importers.GltfToRmv.Helper;

internal static class GltfSourceForwardConverter
{
    private static readonly Matrix4x4 s_positiveXToGamePositiveZ =
        Matrix4x4.CreateRotationY(MathF.PI / 2);
    private static readonly Matrix4x4 s_negativeXToGamePositiveZ =
        Matrix4x4.CreateRotationY(-MathF.PI / 2);
    private static readonly Matrix4x4 s_negativeZToGamePositiveZ =
        Matrix4x4.CreateRotationY(MathF.PI);

    public static Vector3 ConvertGameVector(
        Vector3 value,
        GltfSourceForwardDirection sourceForwardDirection)
    {
        if (sourceForwardDirection == GltfSourceForwardDirection.PositiveZ)
            return value;

        return Vector3.Transform(
            value,
            GetRotation(sourceForwardDirection));
    }

    public static Quaternion ConvertGameRotation(
        Quaternion value,
        GltfSourceForwardDirection sourceForwardDirection)
    {
        if (sourceForwardDirection == GltfSourceForwardDirection.PositiveZ)
            return value;

        var basis = GetRotation(sourceForwardDirection);
        var converted = Matrix4x4.Transpose(basis) *
                        Matrix4x4.CreateFromQuaternion(value) *
                        basis;
        return Quaternion.Normalize(
            Quaternion.CreateFromRotationMatrix(converted));
    }

    public static void ConvertAnimation(
        AnimationFile animationFile,
        GltfSourceForwardDirection sourceForwardDirection)
    {
        if (sourceForwardDirection == GltfSourceForwardDirection.PositiveZ)
            return;

        foreach (var part in animationFile.AnimationParts)
        {
            if (part.StaticFrame != null)
                ConvertFrame(part.StaticFrame, sourceForwardDirection);
            foreach (var frame in part.DynamicFrames)
                ConvertFrame(frame, sourceForwardDirection);
        }
    }

    private static Matrix4x4 GetRotation(
        GltfSourceForwardDirection sourceForwardDirection) =>
        sourceForwardDirection switch
        {
            GltfSourceForwardDirection.PositiveX =>
                s_positiveXToGamePositiveZ,
            GltfSourceForwardDirection.NegativeX =>
                s_negativeXToGamePositiveZ,
            GltfSourceForwardDirection.NegativeZ =>
                s_negativeZToGamePositiveZ,
            _ => throw new ArgumentOutOfRangeException(
                nameof(sourceForwardDirection),
                sourceForwardDirection,
                null),
        };

    private static void ConvertFrame(
        AnimationFile.Frame frame,
        GltfSourceForwardDirection sourceForwardDirection)
    {
        for (var transformIndex = 0;
             transformIndex < frame.Transforms.Count;
             transformIndex++)
        {
            var converted = ConvertGameVector(
                new Vector3(
                    frame.Transforms[transformIndex].X,
                    frame.Transforms[transformIndex].Y,
                    frame.Transforms[transformIndex].Z),
                sourceForwardDirection);
            frame.Transforms[transformIndex] = new RmvVector3(
                converted.X,
                converted.Y,
                converted.Z);
        }

        for (var rotationIndex = 0;
             rotationIndex < frame.Quaternion.Count;
             rotationIndex++)
        {
            var converted = ConvertGameRotation(
                new Quaternion(
                    frame.Quaternion[rotationIndex].X,
                    frame.Quaternion[rotationIndex].Y,
                    frame.Quaternion[rotationIndex].Z,
                    frame.Quaternion[rotationIndex].W),
                sourceForwardDirection);
            frame.Quaternion[rotationIndex] = new RmvVector4(
                converted.X,
                converted.Y,
                converted.Z,
                converted.W);
        }
    }
}
