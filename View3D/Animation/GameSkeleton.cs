using Filetypes.RigidModel;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace View3D.Animation
{
    public class GameSkeleton
    {
        public Vector3[] Translation { get; private set; }
        public Quaternion[] Rotation { get; private set; }
        public string[] BoneNames { get; private set; }

        Matrix[] _worldTransform { get; set; }
        int[] ParentBoneId { get; set; }

        public int BoneCount { get; set; }
        public string SkeletonName { get; set; }

        public AnimationPlayer AnimationPlayer { get; private set; }

        public GameSkeleton(AnimationFile skeletonFile, AnimationPlayer animationPlayer)
        {
            BoneCount = skeletonFile.Bones.Count();
            Translation = new Vector3[BoneCount];
            Rotation = new Quaternion[BoneCount];
            _worldTransform = new Matrix[BoneCount];
            ParentBoneId = new int[BoneCount];
            BoneNames = new string[BoneCount];
            SkeletonName = skeletonFile.Header.SkeletonName;
            AnimationPlayer = animationPlayer;

            int skeletonWeirdIndex = 0;
            for (int i = 0; i < BoneCount; i++)
            {
                ParentBoneId[i] = skeletonFile.Bones[i].ParentId;
                BoneNames[i] = skeletonFile.Bones[i].Name;
                Rotation[i] = new Quaternion(
                    skeletonFile.DynamicFrames[skeletonWeirdIndex].Quaternion[i].X,
                    skeletonFile.DynamicFrames[skeletonWeirdIndex].Quaternion[i].Y,
                    skeletonFile.DynamicFrames[skeletonWeirdIndex].Quaternion[i].Z,
                    skeletonFile.DynamicFrames[skeletonWeirdIndex].Quaternion[i].W);



                Translation[i] = new Vector3(
                    skeletonFile.DynamicFrames[skeletonWeirdIndex].Transforms[i].X * 1,
                    skeletonFile.DynamicFrames[skeletonWeirdIndex].Transforms[i].Y,
                    skeletonFile.DynamicFrames[skeletonWeirdIndex].Transforms[i].Z);
            }

            RebuildSkeletonMatrix();
        }

        public void RebuildSkeletonMatrix()
        {
            for (int i = 0; i < BoneCount; i++)
            {
                var translationMatrix = Matrix.CreateTranslation(Translation[i]);
                var rotationMatrix = Matrix.CreateFromQuaternion(Rotation[i]);
                var transform = rotationMatrix * translationMatrix;
                _worldTransform[i] = transform;
            }

            for (int i = 0; i < BoneCount; i++)
            {
                var parentIndex = GetParentBone(i);
                if (parentIndex == -1)
                    continue;
                _worldTransform[i] = _worldTransform[i] * _worldTransform[parentIndex];
            }
        }

        public void SetBoneTransform(int id, Quaternion rotation, Vector3 position, bool rebuild = true)
        {
            Rotation[id] = rotation;
            Translation[id] = position;
            if(rebuild)
                RebuildSkeletonMatrix();
        }

        public void SetBoneTransform(int id, Vector3 position, bool rebuild = true)
        {
            Translation[id] = position;
            if (rebuild)
                RebuildSkeletonMatrix();
        }

        public void Update()
        {
            if (AnimationPlayer != null)
            {
                var frame = AnimationPlayer.GetCurrentAnimationFrame();
                SetAnimationFrame(frame);
            }
        }

        AnimationFrame _frame;
        public void SetAnimationFrame(AnimationFrame frame)
        {
            _frame = frame;
        }

        public int GetBoneIndexByName(string name)
        {
            for (int i = 0; i < BoneNames.Count(); i++)
            {
                if (BoneNames[i] == name)
                    return i;
            }

            return -1;
        }

        public Matrix GetWorldTransform(int boneIndex)
        {
            return _worldTransform[boneIndex];
        }

        public Matrix GetAnimatedWorldTranform(int boneIndex)
        {
            if (_frame != null)
                return _frame.GetSkeletonAnimatedWorld(this, boneIndex);

            return GetWorldTransform(boneIndex); ;
        }

        public int GetParentBone(int boneIndex)
        {
            return ParentBoneId[boneIndex];
        }

        public List<int> GetDirectChildBones(int parentBoneIndex)
        {
            var output = new List<int>();
            for (int i = 0; i < ParentBoneId.Length; i++)
            {
                if (ParentBoneId[i] == parentBoneIndex)
                    output.Add(i);
            }
            return output;
        }

        public List<int> GetAllChildBones(int parentBoneIndex)
        {
            var output = new List<int>();
            for (int i = 0; i < ParentBoneId.Length; i++)
            {
                if (ParentBoneId[i] == parentBoneIndex)
                {
                    output.Add(i);
                    var res = GetAllChildBones(i);
                    output.AddRange(res);
                }
            }
            return output;
        }

        public AnimationFrame CreateAnimationFrame()
        {
            var currentFrame = new AnimationFrame();
            for (int i = 0; i < BoneCount; i++)
            {
                currentFrame.BoneTransforms.Add(new AnimationFrame.BoneKeyFrame()
                {
                    Translation = Translation[i],
                    Rotation = Rotation[i],
                    Scale = Vector3.One,
                    BoneIndex = i,
                    ParentBoneIndex = GetParentBone(i),
                    WorldTransform = _worldTransform[i]
                }); ;
            }

            return currentFrame;
        }
    }
}
