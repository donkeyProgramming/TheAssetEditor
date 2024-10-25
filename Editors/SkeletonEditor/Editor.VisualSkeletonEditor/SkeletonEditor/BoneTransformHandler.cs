using Editors.Shared.Core.Common.ReferenceModel;
using GameWorld.Core.Animation;
using Microsoft.Xna.Framework;
using Shared.Core.Misc;

namespace Editor.VisualSkeletonEditor.SkeletonEditor
{
    class BoneTransformHandler
    {
        public static void Translate(SkeletonBoneNode _selectedBone, GameSkeleton Skeleton, Vector3 translationValue, Vector3 rotation, bool ShowBonesAsWorldTransform)
        {
            if (_selectedBone == null)
                return;

            var boneIndex = _selectedBone.BoneIndex;

            var quaternionValue = MathUtil.EulerDegreesToQuaternion(rotation);

            if (ShowBonesAsWorldTransform)
            {
                var parentIndex = Skeleton.GetParentBoneIndex(boneIndex);
                if (parentIndex != -1)
                {
                    var parentTransform = Skeleton.GetWorldTransform(parentIndex);

                    var rotationWorld = MathUtil.EulerDegreesToQuaternion(rotation);
                    var translationWorld = translationValue;
                    var currentMatrixWorld = Matrix.CreateFromQuaternion(rotationWorld) * Matrix.CreateTranslation(translationWorld);

                    var localSpaceMatrix = currentMatrixWorld * Matrix.Invert(parentTransform);
                    localSpaceMatrix.Decompose(out _, out quaternionValue, out translationValue);
                }
            }

            Skeleton.Translation[boneIndex] = translationValue;
            Skeleton.Rotation[boneIndex] = quaternionValue;
            Skeleton.RebuildSkeletonMatrix();
        }

        public static void Scale(SkeletonBoneNode _selectedBone, GameSkeleton Skeleton, float boneScale)
        {
            if (_selectedBone == null)
                return;

            var boneIndex = _selectedBone.BoneIndex;
            Skeleton.Scale[boneIndex] = boneScale;
            Skeleton.RebuildSkeletonMatrix();
        }
    }
}
