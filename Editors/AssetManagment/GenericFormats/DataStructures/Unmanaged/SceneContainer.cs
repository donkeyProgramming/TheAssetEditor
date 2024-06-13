using System.Collections.Generic;
using Matrix = Microsoft.Xna.Framework.Matrix;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Quaternion = Microsoft.Xna.Framework.Quaternion;
using AssetManagement.GenericFormats.DataStructures.Managed;
using Shared.Core.Misc;

namespace AssetManagement.GenericFormats.DataStructures.Unmanaged
{
    public class SceneContainer
    {
        public List<PackedMesh> Meshes { get; set; } = new List<PackedMesh>();
        public List<ExtBoneInfo> Bones { get; set; } = new List<ExtBoneInfo>();
        public List<AnimationClip> Animations { get; set; } = new List<AnimationClip>();
        public SceneNode RootNode { get; set; }
        public string SkeletonName { get; set; }
        public FBXFileInfo FileInfoData { get; set; } = new FBXFileInfo();
    }

    public class TransformData
    {
        public Vector3 RotationEuler
        {// TODO: test that this actually works
            get { return MathUtil.QuaternionToEuler(Rotation); }
            set { Rotation = MathUtil.EulerToQuaternions(value.X, value.Y, value.Z); }
        }

        public Quaternion Rotation { get; set; }
        public Vector3 Translation { get; set; }
        public Vector3 Scale { get; set; }

        public Matrix Transform
        {
            // TODO: test that this actually works
            get { return CalculateMatrix(); }
            set { SetTranformValues(value); }
        }

        private void SetTranformValues(Matrix value)
        {
            value.Decompose(out var scale, out var rotation, out var translation);
            Scale = scale;
            Rotation = rotation;
            Scale = translation;
        }

        private Matrix CalculateMatrix()
        {
            var translationMatrix = Matrix.CreateTranslation(Translation);
            var scaleMatrix = Matrix.CreateScale(Scale);
            var rotationMatrix = Matrix.CreateFromQuaternion(Rotation);
            var transformationMatrix = scaleMatrix * rotationMatrix * translationMatrix;

            return transformationMatrix;
        }
    }
    public class SceneNode
    {
        public string Name { get; set; }
        public TransformData TransForm { get; set; }
        public SceneNode Parent { get; set; } = null;
        public List<SceneNode> Children { get; set; }
    }

    public class BoneInfo
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public int ParentId { get; set; }
        public Quaternion LocalRotation { get; set; }
        public Vector3 localTranslations { get; set; }
        public Matrix InverseBindPoseMatrix { get; set; }
    }

    public class AnimationKey
    {
        public Quaternion LocalRotation { get; set; }
        public Vector3 localTranslations { get; set; }
        public double TimeStamp { get; set; }
    }
};
