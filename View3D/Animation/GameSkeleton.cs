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

        AnimationPlayer AnimationPlayer { get; set; }

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

            for (int i = 0; i < BoneCount; i++)
            {
                ParentBoneId[i] = skeletonFile.Bones[i].ParentId;
                BoneNames[i] = skeletonFile.Bones[i].Name;
            }

            int skeletonWeirdIndex = 0;
            for (int i = 0; i < BoneCount; i++)
            {
                float scale = 1;

                Rotation[i] = new Quaternion(
                    skeletonFile.DynamicFrames[skeletonWeirdIndex].Quaternion[i].X,
                    skeletonFile.DynamicFrames[skeletonWeirdIndex].Quaternion[i].Y,
                    skeletonFile.DynamicFrames[skeletonWeirdIndex].Quaternion[i].Z,
                    skeletonFile.DynamicFrames[skeletonWeirdIndex].Quaternion[i].W);

                var translationMatrix = Matrix.CreateTranslation(
                    skeletonFile.DynamicFrames[skeletonWeirdIndex].Transforms[i].X * scale,
                    skeletonFile.DynamicFrames[skeletonWeirdIndex].Transforms[i].Y,
                    skeletonFile.DynamicFrames[skeletonWeirdIndex].Transforms[i].Z);

                Translation[i] = new Vector3(
                    skeletonFile.DynamicFrames[skeletonWeirdIndex].Transforms[i].X * scale,
                    skeletonFile.DynamicFrames[skeletonWeirdIndex].Transforms[i].Y,
                    skeletonFile.DynamicFrames[skeletonWeirdIndex].Transforms[i].Z);

                var rotationMatrix = Matrix.CreateFromQuaternion(Rotation[i]);
                var transform = rotationMatrix * translationMatrix;

                _worldTransform[i] = transform;
            }

            for (int i = 0; i < BoneCount; i++)
            {
                var parentIndex = skeletonFile.Bones[i].ParentId;
                if (parentIndex == -1)
                    continue;
                _worldTransform[i] = _worldTransform[i] * _worldTransform[parentIndex];
            }
        }

        public void Update()
        {
            if (AnimationPlayer != null)
            {
                var frame = AnimationPlayer.GetCurrentFrame();
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
    }
}
