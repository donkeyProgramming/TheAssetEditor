using System.Collections.Generic;
using Matrix = Microsoft.Xna.Framework.Matrix;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Quaternion = Microsoft.Xna.Framework.Quaternion;
using System;
using AssetManagement.GenericFormats.Unmanaged;
using AssetManagement.GenericFormats.DataStructures.Managed;

namespace AssetManagement.GenericFormats
{
    public class SceneContainer
    {
        public List<PackedMesh> Meshes { get; set; } = new List<PackedMesh>();
        public List<BoneInfo> Bones { get; set; } = new List<BoneInfo>();
        public List<AnimationClip> Animations { get; set; } = new List<AnimationClip>();
        public Node RootNode { get; set; }
        public String SkeletonName { get; set; }
    }

    public class Node
    {
        public string Name { get; set; }
        public List<Node> Children { get; set; }
        public Node Parent { get; set; }
        public Matrix Transform { get; set; }        
    }

    public class BoneInfo
    {
        public List<VertexWeight> VertexWeights { get; set; }
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

    public class NodeAnimation
    {
        public List<AnimationKey> Keys { get; set; }
    }

    public class AnimationClip
    {
        public List<NodeAnimation> BoneAnimations { get; set; }

        private double _framesPerSecond;
        public double FramesPerSecond
        {
            get { return _framesPerSecond; }
            set
            {
                FramesPerSecond = value;                
            }
        }
    }
};
