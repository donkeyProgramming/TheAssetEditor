using System;
using System.Collections.Generic;
using Matrix = Microsoft.Xna.Framework.Matrix;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Quaternion = Microsoft.Xna.Framework.Quaternion;

namespace CommonControls.ModelFiles.FBX
{
    public class SceneContainer
    {
        public List<Mesh.PackedMesh> Meshes { get; set; } = new List<Mesh.PackedMesh>();
        public List<BoneInfo> Bones { get; set; } = new List<BoneInfo>();
        public List<AnimationClip> Animations { get; set; } = new List<AnimationClip>();
        public Node? RootNode { get; set; }
    }

    
    public class Node
    {
        public string Mame { get; set; }

        public List<Node> Children { get; set; }

        public Node Parent { get; set; }

        public Matrix Transform { get; set; }
    }

    public struct BoneInfo
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public int ParentId { get; set; }
        Quaternion LocalRotation { get; set; }
        Vector3 localTranslations { get; set; }
        Matrix InverseBindPoseMatrix { get; set; }
    }

    public struct AnimationKey
    {
        public Quaternion LocalRotation { get; set; }
        public Vector3 localTranslations { get; set; }
        public Double TimeStamp { get; set; }
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
                // TODO: reprocess timestamps in keys                
            }
        }
    }


};