using Microsoft.Xna.Framework;
using Shared.GameFormats.Animation;
using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Builds a skeleton directly from an animation file's own bone table, using the anim's
        /// first frame as the bind pose. This is how ad-hoc skeletons work ("building" destruction
        /// animations and other rigidmodel anims): they have no matching skeleton file under
        /// animations/skeletons - the .anim itself is the only definition of the bone hierarchy.
        /// </summary>
        public static GameSkeleton CreateFromAnimationFile(AnimationFile animFile, AnimationPlayer animationPlayer)
        {
            var boneCount = animFile.Bones.Length;
            var skeleton = new GameSkeleton
            {
                SkeletonName = animFile.Header.SkeletonName,
                AnimationPlayer = animationPlayer,
                Translation = new List<Vector3>(new Vector3[boneCount]),
                Rotation = new List<Quaternion>(new Quaternion[boneCount]),
                Scale = new List<float>(new float[boneCount]),
                _parentBoneIds = new List<int>(new int[boneCount]),
                BoneNames = new List<string>(new string[boneCount]),
            };

            var part = animFile.AnimationParts.FirstOrDefault();
            var firstFrame = part?.DynamicFrames.FirstOrDefault() ?? part?.StaticFrame;

            for (var i = 0; i < boneCount; i++)
            {
                skeleton._parentBoneIds[i] = animFile.Bones[i].ParentId;
                skeleton.BoneNames[i] = animFile.Bones[i].Name;
                skeleton.Translation[i] = Vector3.Zero;
                skeleton.Rotation[i] = Quaternion.Identity;
                skeleton.Scale[i] = 1;

                if (part == null || i >= part.TranslationMappings.Count || i >= part.RotationMappings.Count)
                    continue;

                var translationMapping = part.TranslationMappings[i];
                if (translationMapping.IsDynamic && firstFrame != null && translationMapping.Id < firstFrame.Transforms.Count)
                    skeleton.Translation[i] = ToVector3(firstFrame.Transforms[translationMapping.Id]);
                else if (translationMapping.IsStatic && part.StaticFrame != null && translationMapping.Id < part.StaticFrame.Transforms.Count)
                    skeleton.Translation[i] = ToVector3(part.StaticFrame.Transforms[translationMapping.Id]);

                var rotationMapping = part.RotationMappings[i];
                if (rotationMapping.IsDynamic && firstFrame != null && rotationMapping.Id < firstFrame.Quaternion.Count)
                    skeleton.Rotation[i] = ToQuaternion(firstFrame.Quaternion[rotationMapping.Id]);
                else if (rotationMapping.IsStatic && part.StaticFrame != null && rotationMapping.Id < part.StaticFrame.Quaternion.Count)
                    skeleton.Rotation[i] = ToQuaternion(part.StaticFrame.Quaternion[rotationMapping.Id]);
            }

            skeleton.RebuildSkeletonMatrix();
            return skeleton;
        }

        static Vector3 ToVector3(Shared.GameFormats.RigidModel.Transforms.RmvVector3 v) => new(v.X, v.Y, v.Z);

        static Quaternion ToQuaternion(Shared.GameFormats.RigidModel.Transforms.RmvVector4 v)
        {
            var q = new Quaternion(v.X, v.Y, v.Z, v.W);
            q.Normalize();
            return q;
        }

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
            // Delete the bone and all of its descendants, keeping the relative order of the
            // remaining bones unchanged. Rebuilding the list via a tree traversal (as before)
            // reindexed every bone in DFS order, which doesn't necessarily match the original
            // bone order and caused all bones to appear reordered after a single deletion.
            var boneIndexesToRemove = new HashSet<int>(GetAllChildBones(boneIndex)) { boneIndex };

            var oldToNewIndex = new Dictionary<int, int>();
            var newIndex = 0;
            for (var i = 0; i < BoneCount; i++)
            {
                if (boneIndexesToRemove.Contains(i))
                    continue;
                oldToNewIndex[i] = newIndex++;
            }

            var names = new List<string>();
            var parentBones = new List<int>();
            var translation = new List<Vector3>();
            var rotations = new List<Quaternion>();
            var scale = new List<float>();

            for (var i = 0; i < BoneCount; i++)
            {
                if (boneIndexesToRemove.Contains(i))
                    continue;

                var oldParentIndex = GetParentBoneIndex(i);
                var newParentIndex = oldParentIndex == -1 ? -1 : oldToNewIndex[oldParentIndex];

                names.Add(BoneNames[i]);
                parentBones.Add(newParentIndex);
                translation.Add(Translation[i]);
                rotations.Add(Rotation[i]);
                scale.Add(Scale[i]);
            }

            BoneNames = names;
            _parentBoneIds = parentBones;
            Translation = translation;
            Rotation = rotations;
            Scale = scale;

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
    }
}
