using Microsoft.Xna.Framework;
using Shared.GameFormats.Animation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;

namespace GameWorld.Core.Animation
{
    public class GameSkeleton
    {
        List<Matrix> _worldTransform { get; set; }
        List<int> _parentBoneIds { get; set; }

        public List<Vector3> Translation { get; private set; }
        public List<Quaternion> Rotation { get; private set; }
        public List<float> Scale { get; private set; }
        public List<string> BoneNames { get; private set; }
        public int BoneCount { get => BoneNames.Count; }
        public string SkeletonName { get; set; }

        public AnimationPlayer AnimationPlayer { get; private set; }

        public GameSkeleton(AnimationFile skeletonFile, AnimationPlayer animationPlayer)
        {
            var boneCount = skeletonFile.Bones.Count();
            Translation = new List<Vector3>(new Vector3[boneCount]);
            Rotation = new List<Quaternion>(new Quaternion[boneCount]);
            Scale = new List<float>(new float[boneCount]);
            _parentBoneIds = new List<int>(new int[boneCount]);
            BoneNames = new List<string>(new string[boneCount]);

            SkeletonName = skeletonFile.Header.SkeletonName;
            AnimationPlayer = animationPlayer;

            var skeletonAnimFrameIndex = 0;
            for (var i = 0; i < boneCount; i++)
            {
                _parentBoneIds[i] = skeletonFile.Bones[i].ParentId;
                BoneNames[i] = skeletonFile.Bones[i].Name;
                Rotation[i] = new Quaternion(
                    skeletonFile.AnimationParts[0].DynamicFrames[skeletonAnimFrameIndex].Quaternion[i].X,
                    skeletonFile.AnimationParts[0].DynamicFrames[skeletonAnimFrameIndex].Quaternion[i].Y,
                    skeletonFile.AnimationParts[0].DynamicFrames[skeletonAnimFrameIndex].Quaternion[i].Z,
                    skeletonFile.AnimationParts[0].DynamicFrames[skeletonAnimFrameIndex].Quaternion[i].W);

                Translation[i] = new Vector3(
                    skeletonFile.AnimationParts[0].DynamicFrames[skeletonAnimFrameIndex].Transforms[i].X,
                    skeletonFile.AnimationParts[0].DynamicFrames[skeletonAnimFrameIndex].Transforms[i].Y,
                    skeletonFile.AnimationParts[0].DynamicFrames[skeletonAnimFrameIndex].Transforms[i].Z);

                Scale[i] = 1;
            }

            RebuildSkeletonMatrix();
        }

        GameSkeleton() { }

        public void RebuildSkeletonMatrix()
        {
            _worldTransform = new List<Matrix>(new Matrix[BoneCount]);
            for (var i = 0; i < BoneCount; i++)
            {
                var translationMatrix = Matrix.CreateTranslation(Translation[i]);
                var rotationMatrix = Matrix.CreateFromQuaternion(Rotation[i]);
                var scaleMatrix = Matrix.CreateScale(Scale[i]);
                var transform = scaleMatrix * rotationMatrix * translationMatrix;
                _worldTransform[i] = transform;
            }

            for (var i = 0; i < BoneCount; i++)
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
                Scale = Scale.ToList(),
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

        public string GetBoneNameByIndex(int index)
        {
            if (index < 0 || index >= BoneCount) return "";

            return BoneNames[index];
        }

        public int GetBoneIndexByName(string name)
        {
            for (var i = 0; i < BoneNames.Count(); i++)
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
            for (var i = 0; i < _parentBoneIds.Count; i++)
            {
                if (_parentBoneIds[i] == parentBoneIndex)
                    output.Add(i);
            }
            return output;
        }

        public List<int> GetAllChildBones(int parentBoneIndex)
        {
            var output = new List<int>();
            for (var i = 0; i < _parentBoneIds.Count; i++)
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

        public AnimationFrame ConvertToAnimationFrame()
        {
            var currentFrame = new AnimationFrame();
            for (var i = 0; i < BoneCount; i++)
            {
                currentFrame.BoneTransforms.Add(new AnimationFrame.BoneKeyFrame()
                {
                    Translation = Translation[i],
                    Rotation = Rotation[i],
                    Scale = new Vector3(Scale[i]),
                    BoneIndex = i,
                    ParentBoneIndex = GetParentBoneIndex(i),
                    WorldTransform = _worldTransform[i]
                }); ;
            }

            return currentFrame;
        }

        public AnimInvMatrixFile CreateInvMatrixFile()
        {
            if (HasBoneScale())
                throw new Exception("Skeleton contains scale and can not be saved. Bake first");

            var output = new AnimInvMatrixFile();

            output.Version = 1;
            output.MatrixList = new Matrix[_worldTransform.Count];
            for (var i = 0; i < _worldTransform.Count; i++)
                output.MatrixList[i] = Matrix.Transpose(Matrix.Invert(_worldTransform[i]));

            return output;
        }

        public void CreateChildBone(int parentBoneIndex)
        {
            _parentBoneIds.Add(parentBoneIndex);
            BoneNames.Add("new_bone");
            Translation.Add(Vector3.Zero);
            Rotation.Add(Quaternion.Identity);
            Scale.Add(1);
            RebuildSkeletonMatrix();
        }

        public void DeleteBone(int boneIndex, bool rebuildMatrix = true)
        {
            var boneGraph = SkeletonBoneGraph.Create(this);
            boneGraph.DeleteBone(boneIndex);
            boneGraph.FlattenInto(this);

            if (rebuildMatrix)
                RebuildSkeletonMatrix();
        }

        public bool HasBoneScale()
        {
            foreach (var value in Scale)
            {
                if (value != 1)
                    return true;
            }
            return false;
        }

        public void BakeScaleIntoSkeleton()
        {
            for (var i = 0; i < BoneCount; i++)
            {
                var scale = GetAccumulatedBoneScale(i);
                Translation[i] = Translation[i] * scale;
            }

            for (var i = 0; i < BoneCount; i++)
                Scale[i] = 1;
            RebuildSkeletonMatrix();
        }

        float GetAccumulatedBoneScale(int boneIndex)
        {
            var parentIndex = GetParentBoneIndex(boneIndex);
            if (parentIndex == -1)
                return 1;

            return GetAccumulatedBoneScale(parentIndex) * Scale[boneIndex];
        }

        [DebuggerDisplay("SkeletonBoneGraph = {BoneId}-{UpdatedBoneId}")]
        public class SkeletonBoneGraph
        {
            GameSkeleton _source;
            public List<SkeletonBoneGraph> Children { get; set; } = new List<SkeletonBoneGraph>();
            public int BoneId { get; set; }
            public SkeletonBoneGraph Parent { get; set; }
            public int UpdatedBoneId { get; set; }

            public static SkeletonBoneGraph Create(GameSkeleton skeleton)
            {
                var output = new SkeletonBoneGraph()
                {
                    _source = skeleton,
                    BoneId = 0,
                    Parent = null,
                };
                for (var i = 1; i < skeleton.BoneCount; i++)
                {
                    Console.WriteLine($"Adding bone {i}");
                    var parentBoneId = skeleton.GetParentBoneIndex(i);
                    var parentBone = output.GetBone(parentBoneId);
                    parentBone.Children.Add(new SkeletonBoneGraph() { _source = skeleton, BoneId = i, Parent = parentBone });
                }

                var startId = 0;
                output.UpdateBoneId(ref startId);

                return output;
            }

            public void DeleteBone(int boneIndex)
            {
                var bone = GetBone(boneIndex);
                var parent = bone.Parent;
                parent.Children.Remove(bone);
                var startId = 0;
                UpdateBoneId(ref startId);
            }

            public SkeletonBoneGraph GetBone(int id)
            {
                if (BoneId == id)
                    return this;
                foreach (var child in Children)
                {
                    if (child.BoneId == id)
                        return child;

                    var result = child.GetBone(id);
                    if (result != null)
                        return result;
                }
                return null;
            }

            void UpdateBoneId(ref int currentId)
            {
                UpdatedBoneId = currentId++;
                Console.WriteLine($"Current = {BoneId} new = {UpdatedBoneId}");

                foreach (var child in Children)
                    child.UpdateBoneId(ref currentId);
            }

            public void FlattenInto(GameSkeleton gameSkeleton)
            {
                var names = new List<string>();
                var parentBones = new List<int>();
                var translation = new List<Vector3>();
                var rotations = new List<Quaternion>();
                var scale = new List<float>();
                Flatten(ref names, ref parentBones, ref translation, ref rotations, ref scale);

                gameSkeleton.BoneNames = names;
                gameSkeleton._parentBoneIds = parentBones;
                gameSkeleton.Translation = translation;
                gameSkeleton.Rotation = rotations;
                gameSkeleton.Scale = scale;
            }

            public void Flatten(ref List<string> boneNames, ref List<int> parentBones, ref List<Vector3> translation, ref List<Quaternion> rotations, ref List<float> scale)
            {
                if (Parent == null)
                    parentBones.Add(-1);
                else
                    parentBones.Add(Parent.UpdatedBoneId);
                boneNames.Add(_source.BoneNames[BoneId]);
                translation.Add(_source.Translation[BoneId]);
                rotations.Add(_source.Rotation[BoneId]);
                scale.Add(_source.Scale[BoneId]);

                foreach (var child in Children)
                    child.Flatten(ref boneNames, ref parentBones, ref translation, ref rotations, ref scale);
            }
        }
    }
}
