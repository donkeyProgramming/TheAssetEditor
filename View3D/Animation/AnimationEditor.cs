using Filetypes.RigidModel;
using Filetypes.RigidModel.Transforms;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static View3D.Animation.AnimationClip;

namespace View3D.Animation
{
    public class AnimationEditor
    {
        public static AnimationFile ExtractPartOfSkeleton(GameSkeleton skeleton, string skeletonName, int[] bones)
        {
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

            return animFile;
            //var sample = AnimationSampler.Sample(0, 0, skeleton, new List<AnimationClip>() { animation }, true, true);
        }

        private static void CreateBoneTable(GameSkeleton skeleton, int[] bones, ref AnimationFile animFile)
        {
            // Add the bones
            for (int i = 0; i < bones.Length; i++)
            {
                var originalBoneIndex = bones[i];
                animFile.Bones[i] = new AnimationFile.BoneInfo() { Id = i, Name = skeleton.BoneNames[originalBoneIndex], ParentId = skeleton.GetParentBone(originalBoneIndex) };
            }

            for (int i = 0; i < bones.Length; i++)
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

        public static AnimationClip ExtractPartOfAnimation(AnimationClip existingAnim, int[] bones)
        {
            //var newAnim = new AnimationFile();
            //newAnim.Header.SkeletonName = skeletonName;
            //newAnim.Header.AnimationTotalPlayTimeInSec = existingAnim.Header.AnimationTotalPlayTimeInSec;
            //newAnim.Bones = new AnimationFile.BoneInfo[bones.Length];
            //newAnim.StaticFrame = new AnimationFile.Frame();
            //
            //CreateBoneTable(skeleton, bones, ref newAnim);

            ;
            var cpy = existingAnim.Clone();
            cpy.MergeStaticAndDynamicFrames();
            cpy.LimitAnimationToSelectedBones(bones);
            return cpy;

            //var boneCount = bones.Length;
            //var frameCount = clip.DynamicFrames.Count;
            //List<KeyFrame> newDynamicFrames = new List<KeyFrame>();
            //
            //for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
            //{
            //    var newKeyframe = new KeyFrame();
            //
            //    for (int boneIndex = 0; boneIndex < boneCount; boneIndex++)
            //    {
            //        var translationLookup = clip.TranslationMappings[boneIndex];
            //        if (translationLookup.IsDynamic)
            //            newKeyframe.Position.Add(clip.DynamicFrames[frameIndex].Position[translationLookup.Id]);
            //        else if (translationLookup.IsStatic)
            //            newKeyframe.Position.Add(clip.StaticFrame.Position[translationLookup.Id]);
            //
            //        var rotationLookup = clip.RotationMappings[boneIndex];
            //        if (rotationLookup.IsDynamic)
            //            newKeyframe.Rotation.Add(clip.DynamicFrames[frameIndex].Rotation[rotationLookup.Id]);
            //        else if (rotationLookup.IsStatic)
            //            newKeyframe.Rotation.Add(clip.StaticFrame.Rotation[rotationLookup.Id]);
            //    }
            //
            //    newDynamicFrames.Add(newKeyframe);
            //}
            //



            //----------------

            //CreateMappingTable(existingAnim, bones, ref newAnim);
            //CreateStaticFrameTable(existingAnim, bones, ref newAnim);


            //AnimationClip

            //
            // newAnim.DynamicFrames = new List<AnimationFile.Frame>();
            //for (int frameIndex = 0; frameIndex < existingAnim.DynamicFrames.Count; frameIndex++)
            // {
            //     var frame = new AnimationFile.Frame();
            //
            //     for (int boneIndex = 0; boneIndex < bones.Length; boneIndex++)
            //     {
            //
            //         var originalBoneIndex = bones[boneIndex];
            //         var tanslationMappingValue = existingAnim.TranslationMappings[originalBoneIndex];
            //         if (tanslationMappingValue.IsDynamic)
            //         {
            //             // Copy all?
            //             //newAnim.StaticFrame.Transforms.Add(existingAnim.StaticFrame.Transforms[tanslationMappingValue.Id]);
            //         }
            //
            //
            //
            //
            //         
            //         for (int i = 0; i < existingAnim.DynamicFrames[frameIndex].Transforms.Count; i++)
            //             frame.Transforms.Add(existingAnim.DynamicFrames[frameIndex].Transforms[i]);
            //
            //         for (int i = 0; i < existingAnim.DynamicFrames[frameIndex].Quaternion.Count; i++)
            //             frame.Quaternion.Add(existingAnim.DynamicFrames[frameIndex].Quaternion[i]);
            //
            //         newAnim.DynamicFrames.Add(frame);
            //     }
            // }
            //
            // // Remove by index
            // for (int i = 0; i < newAnim.DynamicFrames.Count; i++)
            // { 
            // 
            // }

            return null;
        }

        public static void LoopAnimation(AnimationClip newRiderAnim, int loopCounter)
        {
            var origianlFrames = newRiderAnim.DynamicFrames.ToList();

            for (int i = 0; i < loopCounter - 1; i++)
            {
                for (int frameIndex = 0; frameIndex < origianlFrames.Count; frameIndex++)
                {
                    var newFrame = origianlFrames[frameIndex].Clone();
                    newRiderAnim.DynamicFrames.Add(newFrame);
                }
            }



            //throw new NotImplementedException();
        }

        public static AnimationClip ReSample(GameSkeleton skeleton, AnimationClip newAnim, int newFrameCount, float playTime)
        {
            var output = newAnim.Clone();
            output.DynamicFrames.Clear();

            var fraction = 1.0f / (newFrameCount - 1);
            for (int i = 0; i < newFrameCount; i++)
            {
                float t = i * fraction;
                var keyframe = AnimationSampler.Sample(t, skeleton, new List<AnimationClip> { newAnim });

                KeyFrame newKeyFrame = new KeyFrame();
                for (int mappingIndex = 0; mappingIndex < output.RotationMappings.Count; mappingIndex++)
                {
                    var mapping = output.RotationMappings[mappingIndex];
                    if (mapping.HasValue)
                        newKeyFrame.Rotation.Add(keyframe.BoneTransforms[mappingIndex].Rotation);
                }

                for (int mappingIndex = 0; mappingIndex < output.TranslationMappings.Count; mappingIndex++)
                {
                    var mapping = output.TranslationMappings[mappingIndex];
                    if (mapping.HasValue)
                        newKeyFrame.Position.Add(keyframe.BoneTransforms[mappingIndex].Translation);
                }

                for (int b = 0; b < skeleton.BoneCount; b++)
                    newKeyFrame.Scale.Add(Vector3.One);

                output.DynamicFrames.Add(newKeyFrame);
            } 

            output.PlayTimeInSec = playTime;// (output.DynamicFrames.Count() - 1) / 20.0f;

            return output;
        }

        private static void CreateStaticFrameTable(AnimationFile existingAnim, int[] bones, ref AnimationFile newAnim)
        {
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
            }
        }

      
    }
}
