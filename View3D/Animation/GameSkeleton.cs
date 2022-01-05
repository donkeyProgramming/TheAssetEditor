using CommonControls.FileTypes.Animation;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;

namespace View3D.Animation
{
    public class GameSkeleton
    {
        List<Matrix> _worldTransform { get; set; }
        List<int> _parentBoneIds { get; set; }

        public List<Vector3> Translation { get; private set; }
        public List<Quaternion>Rotation { get; private set; }
        public List<string> BoneNames { get; private set; }
        public int BoneCount { get => BoneNames.Count; }
        public string SkeletonName { get; set; }

        public AnimationPlayer AnimationPlayer { get; private set; }

        public GameSkeleton(AnimationFile skeletonFile, AnimationPlayer animationPlayer)
        {
            var boneCount = skeletonFile.Bones.Count();
            Translation = new List<Vector3>(new Vector3[boneCount]);
            Rotation = new List<Quaternion>( new Quaternion[boneCount]);
            _parentBoneIds = new List<int>(new int[boneCount]);
            BoneNames = new List<string>(new string[boneCount]);

            SkeletonName = skeletonFile.Header.SkeletonName;
            AnimationPlayer = animationPlayer;

            int skeletonAnimFrameIndex = 0;
            for (int i = 0; i < boneCount; i++)
            {
                _parentBoneIds[i] = skeletonFile.Bones[i].ParentId;
                BoneNames[i] = skeletonFile.Bones[i].Name;
                Rotation[i] = new Quaternion(
                    skeletonFile.DynamicFrames[skeletonAnimFrameIndex].Quaternion[i].X,
                    skeletonFile.DynamicFrames[skeletonAnimFrameIndex].Quaternion[i].Y,
                    skeletonFile.DynamicFrames[skeletonAnimFrameIndex].Quaternion[i].Z,
                    skeletonFile.DynamicFrames[skeletonAnimFrameIndex].Quaternion[i].W);

                Translation[i] = new Vector3(
                    skeletonFile.DynamicFrames[skeletonAnimFrameIndex].Transforms[i].X * 1,
                    skeletonFile.DynamicFrames[skeletonAnimFrameIndex].Transforms[i].Y,
                    skeletonFile.DynamicFrames[skeletonAnimFrameIndex].Transforms[i].Z);
            }

            RebuildSkeletonMatrix();
        }

        GameSkeleton() { }

        public void RebuildSkeletonMatrix()
        {
            _worldTransform = new List<Matrix>(new Matrix[BoneCount]);
            for (int i = 0; i < BoneCount; i++)
            {
                var translationMatrix = Matrix.CreateTranslation(Translation[i]);
                var rotationMatrix = Matrix.CreateFromQuaternion(Rotation[i]);
                var transform = rotationMatrix * translationMatrix;
                _worldTransform[i] = transform;
            }

            for (int i = 0; i < BoneCount; i++)
            {
                var parentIndex = GetParentBoneIndex(i);
                if (parentIndex == -1)
                    continue;
                _worldTransform[i] = _worldTransform[i] * _worldTransform[parentIndex];
            }
        }

        public GameSkeleton Clone()
        {
            var clone = new GameSkeleton()
            {
                _worldTransform = _worldTransform.ToList(),
                _parentBoneIds = _parentBoneIds.ToList(),
                Translation = Translation.ToList(),
                Rotation = Rotation.ToList(),
                BoneNames = BoneNames.ToList(),
                SkeletonName = SkeletonName,
                AnimationPlayer = AnimationPlayer,
                _frame = _frame
            };

            return clone;
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
        public Matrix GetAnimatedTranform(int boneIndex)
        {
            if (_frame != null)
                return _frame.BoneTransforms[boneIndex].WorldTransform;

            return GetWorldTransform(boneIndex); ;
        }

        public int GetParentBoneIndex(int boneIndex)
        {
            return _parentBoneIds[boneIndex];
        }

        public List<int> GetDirectChildBones(int parentBoneIndex)
        {
            var output = new List<int>();
            for (int i = 0; i < _parentBoneIds.Count; i++)
            {
                if (_parentBoneIds[i] == parentBoneIndex)
                    output.Add(i);
            }
            return output;
        }

        public List<int> GetAllChildBones(int parentBoneIndex)
        {
            var output = new List<int>();
            for (int i = 0; i < _parentBoneIds.Count; i++)
            {
                if (_parentBoneIds[i] == parentBoneIndex)
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
                    ParentBoneIndex = GetParentBoneIndex(i),
                    WorldTransform = _worldTransform[i]
                }); ;
            }

            return currentFrame;
        }

        public AnimInvMatrixFile CreateInvMatrixFile()
        {
            var output = new AnimInvMatrixFile();

            output.Version = 1;
            output.MatrixList = new Matrix[_worldTransform.Count];
            for (int i = 0; i < _worldTransform.Count; i++)
                output.MatrixList[i] = Matrix.Transpose(Matrix.Invert(_worldTransform[i]));

            return output;
        }

        public void CreateChildBone(int parentBoneIndex)
        {
            _parentBoneIds.Add(parentBoneIndex);
            BoneNames.Add("new_bone");
            Translation.Add(Vector3.Zero);
            Rotation.Add(Quaternion.Identity);
            RebuildSkeletonMatrix();
        }

        public void DeleteBone(int boneIndex, bool rebuildMatrix = true)
        {
            var children = GetDirectChildBones(boneIndex);
            foreach (var child in children)
                DeleteBone(child, false);

            _parentBoneIds.RemoveAt(boneIndex);
            BoneNames.RemoveAt(boneIndex);
            Translation.RemoveAt(boneIndex);
            Rotation.RemoveAt(boneIndex);

            if(rebuildMatrix)
                RebuildSkeletonMatrix();
        }
    }
}
