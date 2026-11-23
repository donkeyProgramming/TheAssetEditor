using System.IO;
using System.Numerics;
using Shared.GameFormats.Animation;
using Shared.GameFormats.RigidModel.Transforms;
using SharpGLTF.Schema2;

namespace Editors.ImportExport.Importing.Importers.GltfToRmv.Helper;

public static class GltfSkeletonImporter
{
    public static AnimationFile Build(
        ModelRoot modelRoot,
        string skeletonName,
        bool mirrorMesh)
    {
        ArgumentNullException.ThrowIfNull(modelRoot);
        if (string.IsNullOrWhiteSpace(skeletonName))
        {
            throw new InvalidDataException(
                "The glTF has no skeleton name, so no game skeleton can be made.");
        }

        var skin = GetSingleLogicalSkeleton(modelRoot);

        return Build(skin, skeletonName, mirrorMesh, bakeAncestorTransform: false);
    }

    internal static AnimationFile BuildExternal(
        ModelRoot modelRoot,
        string inputFile,
        string? skeletonName,
        bool mirrorMesh)
    {
        ArgumentNullException.ThrowIfNull(modelRoot);
        var skin = GetSingleLogicalSkeleton(modelRoot);

        var resolvedName = string.IsNullOrWhiteSpace(skeletonName)
            ? GetDefaultSkeletonName(skin, inputFile)
            : skeletonName.Trim();
        if (resolvedName is "." or ".." ||
            !string.Equals(Path.GetFileName(resolvedName), resolvedName, StringComparison.Ordinal) ||
            resolvedName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new InvalidDataException(
                "The new skeleton name can't contain a path or characters that aren't valid in file names.");
        }

        return Build(skin, resolvedName, mirrorMesh, bakeAncestorTransform: true);
    }

    private static Skin GetSingleLogicalSkeleton(ModelRoot modelRoot)
    {
        var skins = modelRoot.LogicalSkins.ToList();
        if (skins.Count == 0)
        {
            throw new InvalidDataException(
                "The glTF has no skin skeleton to import.");
        }

        var logicalSkeletons = skins
            .GroupBy(CreateSkeletonSignature, StringComparer.Ordinal)
            .ToList();
        if (logicalSkeletons.Count != 1)
        {
            throw new InvalidDataException(
                "The glTF contains more than one skeleton; skins with different joints or hierarchies can't be merged automatically.");
        }

        var equivalentSkins = logicalSkeletons[0].ToList();
        var skin = equivalentSkins[0];
        var referenceBindPose = ValidateExternalSkin(skin);
        foreach (var equivalentSkin in equivalentSkins.Skip(1))
        {
            var equivalentBindPose = ValidateExternalSkin(equivalentSkin);
            ValidateEquivalentBindPose(referenceBindPose, equivalentBindPose);
        }

        return skin;
    }

    private static Dictionary<Node, Matrix4x4> ValidateExternalSkin(Skin skin)
    {
        var context = GetValidatedJointContext(skin);
        foreach (var joint in context.Joints)
            ValidateBoneLocalTransform(joint, FindJointParent(joint, context.JointSet));

        return GetBindWorldMatrices(skin);
    }

    private static AnimationFile Build(
        Skin skin,
        string skeletonName,
        bool mirrorMesh,
        bool bakeAncestorTransform)
    {
        if (string.IsNullOrWhiteSpace(skeletonName))
        {
            throw new InvalidDataException(
                "The glTF has no skeleton name, so no game skeleton can be made.");
        }

        var context = GetValidatedJointContext(skin);
        var orderedJoints = context.OrderedJoints;
        var boneIndexes = orderedJoints
            .Select((joint, index) => (joint, index))
            .ToDictionary(item => item.joint, item => item.index);
        var bindWorldMatrices = bakeAncestorTransform
            ? GetBindWorldMatrices(skin)
            : null;
        var scaleFreeBindWorldMatrices = bindWorldMatrices?
            .ToDictionary(item => item.Key, item => RemoveScale(item.Key, item.Value));
        var frame = new AnimationFile.Frame();
        var bones = new AnimationFile.BoneInfo[orderedJoints.Count];

        for (var boneIndex = 0; boneIndex < orderedJoints.Count; boneIndex++)
        {
            var joint = orderedJoints[boneIndex];
            var parent = FindJointParent(joint, context.JointSet);
            Matrix4x4 localMatrix;
            if (bakeAncestorTransform)
            {
                ValidateBoneLocalTransform(joint, parent);
                localMatrix = scaleFreeBindWorldMatrices![joint];
                if (parent != null)
                {
                    if (!Matrix4x4.Invert(
                            scaleFreeBindWorldMatrices[parent],
                            out var inverseParent))
                    {
                        throw new InvalidDataException(
                            string.Format("The bind transform of glTF bone \"{0}\" can't be inverted.",
                                parent.Name));
                    }

                    localMatrix *= inverseParent;
                }
            }
            else
            {
                localMatrix = joint.WorldMatrix;
                if (parent != null)
                {
                    if (!Matrix4x4.Invert(parent.WorldMatrix, out var inverseParent))
                    {
                        throw new InvalidDataException(
                            string.Format("The bind transform of glTF bone \"{0}\" can't be inverted.",
                                parent.Name));
                    }

                    localMatrix *= inverseParent;
                }
            }

            if (!Matrix4x4.Decompose(localMatrix, out var scale, out var rotation, out var translation))
            {
                throw new InvalidDataException(
                    string.Format("The bind transform of glTF bone \"{0}\" can't be decomposed.",
                        joint.Name));
            }
            if (!IsUnitScale(scale))
            {
                throw new InvalidDataException(
                    string.Format("glTF bone \"{0}\" is scaled; an .anim skeleton can't store bone scale.",
                        joint.Name));
            }

            rotation = Quaternion.Normalize(rotation);
            bones[boneIndex] = new AnimationFile.BoneInfo
            {
                Id = boneIndex,
                Name = joint.Name,
                ParentId = parent == null
                    ? AnimationFile.BoneIndexNoParent
                    : boneIndexes[parent],
            };
            frame.Transforms.Add(new RmvVector3(
                mirrorMesh ? -translation.X : translation.X,
                translation.Y,
                translation.Z));
            frame.Quaternion.Add(new RmvVector4(
                rotation.X,
                mirrorMesh ? -rotation.Y : rotation.Y,
                mirrorMesh ? -rotation.Z : rotation.Z,
                rotation.W));
        }

        var part = new AnimationFile.AnimationPart();
        for (var boneIndex = 0; boneIndex < bones.Length; boneIndex++)
        {
            part.TranslationMappings.Add(new AnimationFile.AnimationBoneMapping(boneIndex));
            part.RotationMappings.Add(new AnimationFile.AnimationBoneMapping(boneIndex));
        }
        part.DynamicFrames.Add(frame);
        part.DynamicFrames.Add(CloneFrame(frame));

        return new AnimationFile
        {
            Header = new AnimationFile.AnimationHeader
            {
                Version = 7,
                FrameRate = 20,
                SkeletonName = skeletonName,
                AnimationTotalPlayTimeInSec = 0.1f,
            },
            Bones = bones,
            AnimationParts = [part],
        };
    }

    private static Dictionary<Node, Matrix4x4> GetBindWorldMatrices(Skin skin)
    {
        var result = new Dictionary<Node, Matrix4x4>();
        for (var jointIndex = 0; jointIndex < skin.JointsCount; jointIndex++)
        {
            var joint = skin.GetJoint(jointIndex);
            if (!Matrix4x4.Invert(joint.InverseBindMatrix, out var bindWorldMatrix))
            {
                throw new InvalidDataException(
                    string.Format("The inverse bind matrix of glTF bone \"{0}\" can't be inverted.",
                        joint.Joint.Name));
            }
            if (HasShear(bindWorldMatrix))
            {
                throw new InvalidDataException(
                    string.Format("The bind transform of glTF bone \"{0}\" contains shear, which an .anim skeleton can't store.",
                        joint.Joint.Name));
            }
            if (!Matrix4x4.Decompose(bindWorldMatrix, out _, out _, out _))
            {
                throw new InvalidDataException(
                    string.Format("The bind transform of glTF bone \"{0}\" can't be decomposed.",
                        joint.Joint.Name));
            }

            result[joint.Joint] = bindWorldMatrix;
        }

        return result;
    }

    private static Matrix4x4 RemoveScale(Node joint, Matrix4x4 matrix)
    {
        if (!Matrix4x4.Decompose(matrix, out _, out var rotation, out var translation))
        {
            throw new InvalidDataException(
                string.Format("The bind transform of glTF bone \"{0}\" can't be decomposed.",
                    joint.Name));
        }

        return Matrix4x4.CreateFromQuaternion(Quaternion.Normalize(rotation)) *
               Matrix4x4.CreateTranslation(translation);
    }

    private static void ValidateBoneLocalTransform(Node joint, Node? parentJoint)
    {
        var localMatrix = GetVisualWorldMatrix(joint);
        var visualParent = parentJoint ?? joint.VisualParent;
        if (visualParent != null)
        {
            if (!Matrix4x4.Invert(
                    GetVisualWorldMatrix(visualParent),
                    out var inverseParent))
            {
                throw new InvalidDataException(
                    string.Format("The parent transform of glTF bone \"{0}\" can't be inverted.",
                        joint.Name));
            }

            localMatrix *= inverseParent;
        }

        if (HasShear(localMatrix))
        {
            throw new InvalidDataException(
                string.Format("glTF bone \"{0}\" contains shear, which an .anim skeleton can't store.",
                    joint.Name));
        }
        if (!Matrix4x4.Invert(localMatrix, out _))
        {
            throw new InvalidDataException(
                string.Format("The local bind transform of glTF bone \"{0}\" can't be inverted.",
                    joint.Name));
        }
        if (!Matrix4x4.Decompose(localMatrix, out var scale, out _, out _))
        {
            throw new InvalidDataException(
                string.Format("The local bind transform of glTF bone \"{0}\" can't be decomposed.",
                    joint.Name));
        }
        if (!IsUnitScale(scale))
        {
            throw new InvalidDataException(
                string.Format("glTF bone \"{0}\" is scaled; an .anim skeleton can't store bone scale.",
                    joint.Name));
        }
    }

    private static Matrix4x4 GetVisualWorldMatrix(Node node)
    {
        var worldMatrix = node.LocalMatrix;
        for (var parent = node.VisualParent; parent != null; parent = parent.VisualParent)
            worldMatrix *= parent.LocalMatrix;

        return worldMatrix;
    }

    private static bool HasShear(Matrix4x4 matrix)
    {
        var x = new Vector3(matrix.M11, matrix.M12, matrix.M13);
        var y = new Vector3(matrix.M21, matrix.M22, matrix.M23);
        var z = new Vector3(matrix.M31, matrix.M32, matrix.M33);
        if (x.LengthSquared() <= 0 || y.LengthSquared() <= 0 || z.LengthSquared() <= 0)
            return false;

        x = Vector3.Normalize(x);
        y = Vector3.Normalize(y);
        z = Vector3.Normalize(z);
        return Math.Abs(Vector3.Dot(x, y)) > 0.0001f ||
               Math.Abs(Vector3.Dot(x, z)) > 0.0001f ||
               Math.Abs(Vector3.Dot(y, z)) > 0.0001f;
    }

    private static void ValidateEquivalentBindPose(
        IReadOnlyDictionary<Node, Matrix4x4> reference,
        IReadOnlyDictionary<Node, Matrix4x4> candidate)
    {
        var referenceByName = reference.ToDictionary(
            item => item.Key.Name,
            item => item.Value,
            StringComparer.OrdinalIgnoreCase);
        foreach (var item in candidate)
        {
            if (!referenceByName.TryGetValue(item.Key.Name, out var referenceMatrix) ||
                !AreNearlyEqual(referenceMatrix, item.Value))
            {
                throw new InvalidDataException(
                    string.Format("Several glTF skins share the same structure but have different bind poses, so they can't share one game skeleton (bone \"{0}\").",
                        item.Key.Name));
            }
        }
    }

    private static bool AreNearlyEqual(Matrix4x4 first, Matrix4x4 second)
    {
        var difference = first - second;
        return Math.Abs(difference.M11) <= 0.0001f &&
               Math.Abs(difference.M12) <= 0.0001f &&
               Math.Abs(difference.M13) <= 0.0001f &&
               Math.Abs(difference.M14) <= 0.0001f &&
               Math.Abs(difference.M21) <= 0.0001f &&
               Math.Abs(difference.M22) <= 0.0001f &&
               Math.Abs(difference.M23) <= 0.0001f &&
               Math.Abs(difference.M24) <= 0.0001f &&
               Math.Abs(difference.M31) <= 0.0001f &&
               Math.Abs(difference.M32) <= 0.0001f &&
               Math.Abs(difference.M33) <= 0.0001f &&
               Math.Abs(difference.M34) <= 0.0001f &&
               Math.Abs(difference.M41) <= 0.0001f &&
               Math.Abs(difference.M42) <= 0.0001f &&
               Math.Abs(difference.M43) <= 0.0001f &&
               Math.Abs(difference.M44) <= 0.0001f;
    }

    private static SkeletonJointContext GetValidatedJointContext(Skin skin)
    {
        if (skin.JointsCount == 0)
        {
            throw new InvalidDataException(
                "The glTF has a skin with no joints, so no game skeleton can be made.");
        }
        if (skin.JointsCount > 256)
        {
            throw new InvalidDataException(
                "The glTF skeleton has more than the 256 bones RMV2 can address.");
        }

        var joints = Enumerable.Range(0, skin.JointsCount)
            .Select(index => skin.GetJoint(index).Joint)
            .ToList();
        if (joints.Any(joint => string.IsNullOrWhiteSpace(joint.Name)))
        {
            throw new InvalidDataException(
                "The glTF skeleton has an unnamed bone, so it can't be imported safely.");
        }
        if (joints.Select(joint => joint.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() !=
            joints.Count)
        {
            throw new InvalidDataException(
                "The glTF skeleton has bones whose names differ only by case, so it can't be imported safely.");
        }

        var jointSet = joints.ToHashSet();
        return new SkeletonJointContext(
            joints,
            jointSet,
            OrderParentsBeforeChildren(joints, jointSet));
    }

    private static string CreateSkeletonSignature(Skin skin)
    {
        var context = GetValidatedJointContext(skin);
        return string.Join(
            "|",
            context.Joints
                .Select(joint =>
                {
                    var parent = FindJointParent(joint, context.JointSet);
                    return $"{joint.Name.ToLowerInvariant()}>{parent?.Name.ToLowerInvariant() ?? "<root>"}";
                })
                .OrderBy(value => value, StringComparer.Ordinal));
    }

    private static string GetDefaultSkeletonName(Skin skin, string inputFile)
    {
        if (!string.IsNullOrWhiteSpace(skin.Name))
            return skin.Name;
        if (!string.IsNullOrWhiteSpace(skin.Skeleton?.Name))
            return skin.Skeleton.Name;

        var ancestorName = GetNamedJointAncestor(skin);
        if (!string.IsNullOrWhiteSpace(ancestorName))
            return ancestorName;

        return Path.GetFileNameWithoutExtension(inputFile);
    }

    private static string? GetNamedJointAncestor(Skin skin)
    {
        var context = GetValidatedJointContext(skin);
        var rootJoints = context.Joints
            .Where(joint => FindJointParent(joint, context.JointSet) == null)
            .ToList();
        if (rootJoints.Count == 0)
            return null;

        var candidate = rootJoints[0].VisualParent;
        while (candidate != null)
        {
            if (!string.IsNullOrWhiteSpace(candidate.Name) &&
                rootJoints.All(root => IsVisualAncestor(candidate, root)))
            {
                return candidate.Name;
            }

            candidate = candidate.VisualParent;
        }

        return null;
    }

    private static bool IsVisualAncestor(Node candidate, Node node)
    {
        var parent = node.VisualParent;
        while (parent != null)
        {
            if (ReferenceEquals(parent, candidate))
                return true;

            parent = parent.VisualParent;
        }

        return false;
    }

    private static List<Node> OrderParentsBeforeChildren(
        IReadOnlyList<Node> joints,
        HashSet<Node> jointSet)
    {
        var ordered = new List<Node>(joints.Count);
        var visitStates = new Dictionary<Node, byte>();

        void Visit(Node joint)
        {
            if (visitStates.TryGetValue(joint, out var state))
            {
                if (state == 2)
                    return;
                if (state == 1)
                {
                    throw new InvalidDataException(
                        "The glTF skeleton has a cycle in its parent hierarchy.");
                }
            }

            visitStates[joint] = 1;
            var parent = FindJointParent(joint, jointSet);
            if (parent != null)
                Visit(parent);
            visitStates[joint] = 2;
            ordered.Add(joint);
        }

        foreach (var joint in joints)
            Visit(joint);

        return ordered;
    }

    private static Node? FindJointParent(Node joint, HashSet<Node> jointSet)
    {
        var parent = joint.VisualParent;
        while (parent != null && !jointSet.Contains(parent))
            parent = parent.VisualParent;

        return parent;
    }

    private static bool IsUnitScale(Vector3 scale) =>
        Math.Abs(scale.X - 1) <= 0.0001f &&
        Math.Abs(scale.Y - 1) <= 0.0001f &&
        Math.Abs(scale.Z - 1) <= 0.0001f;

    private sealed record SkeletonJointContext(
        IReadOnlyList<Node> Joints,
        HashSet<Node> JointSet,
        IReadOnlyList<Node> OrderedJoints);

    private static AnimationFile.Frame CloneFrame(AnimationFile.Frame frame) => new()
    {
        Transforms = frame.Transforms.ToList(),
        Quaternion = frame.Quaternion.ToList(),
    };
}
