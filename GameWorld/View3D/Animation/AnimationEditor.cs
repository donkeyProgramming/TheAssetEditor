using Microsoft.Xna.Framework;
using Shared.GameFormats.Animation;
using System;
using System.Linq;
using static GameWorld.Core.Animation.AnimationClip;

namespace GameWorld.Core.Animation
{
    public class AnimationEditor
    {
        public static AnimationFile ExtractPartOfSkeleton(GameSkeleton skeleton, string skeletonName, int[] bones)
        {
            throw new NotImplementedException();
            /*
            // Header
            var animFile = new AnimationFile();
            animFile.Header.SkeletonName = skeletonName;
            animFile.Header.AnimationTotalPlayTimeInSec = 0.1f;
            animFile.Bones = new AnimationFile.BoneInfo[bones.Length];

            CreateBoneTable(skeleton, bones, ref animFile);

            // Create the keyframe
            animFile.DynamicFrames = new List<AnimationFile.Frame>();
            var frame = new AnimationFile.Frame();
            animFile.DynamicFrames.Add(frame);

            // Populate the keyframe
            for (int i = 0; i < bones.Length; i++)
            {
                var originalBoneIndex = bones[i];

                if (i == 0)
                {
                    var worldTrans = skeleton.GetWorldTransform(originalBoneIndex);
                    var res = worldTrans.Decompose(out Vector3 scale, out var rot, out var trans);
                    frame.Transforms.Add(new RmvVector3(trans.X, trans.Y, trans.Z));
                    frame.Quaternion.Add(new RmvVector4(rot.X, rot.Y, rot.Z, rot.W));
                }
                else
                {
                    var trans = skeleton.Translation[originalBoneIndex];
                    var rot = skeleton.Rotation[originalBoneIndex];

                    frame.Transforms.Add(new RmvVector3(trans.X, trans.Y, trans.Z));
                    frame.Quaternion.Add(new RmvVector4(rot.X, rot.Y, rot.Z, rot.W));
                }

            }

            return animFile;*/
            //var sample = AnimationSampler.Sample(0, 0, skeleton, new List<AnimationClip>() { animation }, true, true);
        }

        private static void CreateBoneTable(GameSkeleton skeleton, int[] bones, ref AnimationFile animFile)
        {
            // Add the bones
            for (var i = 0; i < bones.Length; i++)
            {
                var originalBoneIndex = bones[i];
                animFile.Bones[i] = new AnimationFile.BoneInfo() { Id = i, Name = skeleton.BoneNames[originalBoneIndex], ParentId = skeleton.GetParentBoneIndex(originalBoneIndex) };
            }

            for (var i = 0; i < bones.Length; i++)
            {
                if (animFile.Bones[i].ParentId == -1)
                    continue;

                var parentName = skeleton.BoneNames[animFile.Bones[i].ParentId];
                var indexOf = animFile.Bones.Select((value, index) => new { value.Name, index })
                        .Where(pair => pair.Name == parentName)
                        .Select(pair => pair.index + 1)
                        .FirstOrDefault() - 1;
                animFile.Bones[i].ParentId = indexOf;
            }
        }

        public static void LoopAnimation(AnimationClip newRiderAnim, int loopCounter)
        {
            var origianlFrames = newRiderAnim.DynamicFrames.ToList();

            for (var i = 0; i < loopCounter - 1; i++)
            {
                for (var frameIndex = 0; frameIndex < origianlFrames.Count; frameIndex++)
                {
                    var newFrame = origianlFrames[frameIndex].Clone();
                    newRiderAnim.DynamicFrames.Add(newFrame);
                }
            }
        }

        public static AnimationClip ReSample(GameSkeleton skeleton, AnimationClip newAnim, int newFrameCount, float playTime)
        {
            var output = newAnim.Clone();
            output.DynamicFrames.Clear();

            var fraction = 1.0f / (newFrameCount - 1);
            for (var i = 0; i < newFrameCount; i++)
            {
                var t = i * fraction;
                var keyframe = AnimationSampler.Sample(t, skeleton, newAnim);

                var newKeyFrame = new KeyFrame();
                for (var boneIndex = 0; boneIndex < skeleton.BoneCount; boneIndex++)
                {
                    newKeyFrame.Rotation.Add(keyframe.BoneTransforms[boneIndex].Rotation);
                    newKeyFrame.Position.Add(keyframe.BoneTransforms[boneIndex].Translation);
                }


                for (var b = 0; b < skeleton.BoneCount; b++)
                    newKeyFrame.Scale.Add(Vector3.One);

                output.DynamicFrames.Add(newKeyFrame);
            }

            output.PlayTimeInSec = playTime;// (output.DynamicFrames.Count() - 1) / 20.0f;

            return output;
        }

        private static void CreateStaticFrameTable(AnimationFile existingAnim, int[] bones, ref AnimationFile newAnim)
        {
            throw new NotImplementedException();
            /*
            newAnim.StaticFrame = new AnimationFile.Frame();
            for (int boneIndex = 0; boneIndex < bones.Length; boneIndex++)
            {
                //existingAnim.TranslationMappings.Count
                var originalBoneIndex = bones[boneIndex];
                var tanslationMappingValue = existingAnim.TranslationMappings[originalBoneIndex];
                if (tanslationMappingValue.IsStatic)
                    newAnim.StaticFrame.Transforms.Add(existingAnim.StaticFrame.Transforms[tanslationMappingValue.Id]);

                var rotationMappingValue = existingAnim.RotationMappings[originalBoneIndex];
                if (rotationMappingValue.IsStatic)
                    newAnim.StaticFrame.Quaternion.Add(existingAnim.StaticFrame.Quaternion[rotationMappingValue.Id]);
            }*/
        }


    }
}
