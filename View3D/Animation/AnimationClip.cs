using CommonControls.FileTypes.Animation;
using CommonControls.FileTypes.RigidModel.Transforms;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using static CommonControls.FileTypes.Animation.AnimationFile;


namespace View3D.Animation
{
    public class AnimationClip
    {
        public class KeyFrame
        {
            public List<Vector3> Position { get; set; } = new List<Vector3>();
            public List<Quaternion> Rotation { get; set; } = new List<Quaternion>();
            public List<Vector3> Scale { get; set; } = new List<Vector3>();

            public override string ToString()
            {
                return $"PosCount = {Position.Count}, RotCount = {Rotation.Count}, ScaleCount = {Scale.Count}";
            }

            public KeyFrame Clone()
            {
                return new KeyFrame()
                {
                    Position = new List<Vector3>(Position),
                    Rotation = new List<Quaternion>(Rotation),
                    Scale = new List<Vector3>(Scale)
                };
            }

            public int GetBoneCountFromFrame()
            {
                if (Position.Count == Rotation.Count && Rotation.Count == Scale.Count)
                    return Position.Count;
                throw new Exception($"Not all attribues have the same count P: {Position.Count} R:{Rotation.Count} S:{Scale.Count}");
            }
        }

        private const long MicrosecondsPerSecond = 1_000_000;

        private long _playTimeUs = -1 * MicrosecondsPerSecond;

        public List<KeyFrame> DynamicFrames = new List<KeyFrame>();
        
        public long PlayTimeUs => _playTimeUs;

        public long MicrosecondsPerFrame
        {
            get
            {
                if (DynamicFrames.Count == 0)
                {
                    return -1;
                }
                return _playTimeUs / DynamicFrames.Count;
            }
        }

        public float PlayTimeInSec
        {
            get => (float) _playTimeUs / MicrosecondsPerSecond;
            set
            {
                if (DynamicFrames.Count == 0)
                {
                    _playTimeUs = (long)(value * MicrosecondsPerSecond);
                    return;
                }

                // make sure we have whole number of microsecond per frame
                long framePlayTimeUs = (long) Math.Ceiling(value / DynamicFrames.Count * MicrosecondsPerSecond);
                _playTimeUs = framePlayTimeUs * DynamicFrames.Count;
            }
        }

        public int AnimationBoneCount
        {
            get 
            {
                var dynamicBones = 0;
                if (DynamicFrames.Count != 0)
                    return DynamicFrames[0].Position.Count;
                return dynamicBones;
            }
        }


        public AnimationClip() { }

        public AnimationClip(AnimationFile file, GameSkeleton skeleton)
        {
            foreach (var animationPart in file.AnimationParts)
            {
                var frames = CreateKeyFramesFromAnimationPart(animationPart, skeleton);
                DynamicFrames.AddRange(frames);
            }

            PlayTimeInSec = file.Header.AnimationTotalPlayTimeInSec;
        }


        List<KeyFrame> CreateKeyFramesFromAnimationPart(AnimationPart animationPart, GameSkeleton skeleton)
        {
            List<KeyFrame> newDynamicFrames = new List<KeyFrame>();

            var animationSkeletonBoneCount = animationPart.RotationMappings.Count;
            var frameCount = animationPart.DynamicFrames.Count;

            if (frameCount == 0 && animationPart.StaticFrame != null)
                frameCount = 1; // Poses

            for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                var newKeyframe = new KeyFrame();

                for (int animationSkeletonBoneIndex = 0; animationSkeletonBoneIndex < animationSkeletonBoneCount; animationSkeletonBoneIndex++)
                {
                    // We can apply animations to a skeleton where the skeleton of the animation is different then the skeleton we are applying it to
                    // If that is the case we just discard the information.
                    bool isBoneIndexValid = animationSkeletonBoneIndex < skeleton.BoneCount;
                    if (isBoneIndexValid)
                    {
                        var translationLookup = animationPart.TranslationMappings[animationSkeletonBoneIndex];
                        if (translationLookup.IsDynamic)
                            newKeyframe.Position.Add(animationPart.DynamicFrames[frameIndex].Transforms[translationLookup.Id].ToVector3());
                        else if (translationLookup.IsStatic)
                            newKeyframe.Position.Add(animationPart.StaticFrame.Transforms[translationLookup.Id].ToVector3());
                        else
                            newKeyframe.Position.Add(skeleton.Translation[animationSkeletonBoneIndex]);

                        var rotationLookup = animationPart.RotationMappings[animationSkeletonBoneIndex];
                        if (rotationLookup.IsDynamic)
                            newKeyframe.Rotation.Add(animationPart.DynamicFrames[frameIndex].Quaternion[rotationLookup.Id].ToQuaternion());
                        else if (rotationLookup.IsStatic)
                            newKeyframe.Rotation.Add(animationPart.StaticFrame.Quaternion[rotationLookup.Id].ToQuaternion());
                        else
                            newKeyframe.Rotation.Add(skeleton.Rotation[animationSkeletonBoneIndex]);

                        newKeyframe.Scale.Add(Vector3.One);
                    }
                }

                newDynamicFrames.Add(newKeyframe);
            }

            return newDynamicFrames;
        }

        public AnimationFile ConvertToFileFormat(GameSkeleton skeleton)
        {
            var output = new AnimationFile();

            var fRate = (DynamicFrames.Count() - 1) / PlayTimeInSec;
            output.Header.FrameRate = (float)Math.Round(fRate);
            if (output.Header.FrameRate <= 0)
                output.Header.FrameRate = 20;

            output.Header.Version = 7;
            output.Header.AnimationTotalPlayTimeInSec = PlayTimeInSec;
            output.Header.SkeletonName = skeleton.SkeletonName;
            output.AnimationParts.Add(new AnimationPart());

            output.Bones = new BoneInfo[skeleton.BoneCount];
            for (int i = 0; i < skeleton.BoneCount; i++)
            {
                output.Bones[i] = new BoneInfo()
                {
                    Id = i,
                    Name = skeleton.BoneNames[i],
                    ParentId = skeleton.GetParentBoneIndex(i)
                };

                output.AnimationParts[0].RotationMappings.Add(new AnimationBoneMapping(i));
                output.AnimationParts[0].TranslationMappings.Add(new AnimationBoneMapping(i));
            }
        

            for(int i = 0; i < DynamicFrames.Count; i++)
                output.AnimationParts[0].DynamicFrames.Add(CreateFrameFromKeyFrame(i, skeleton));

            return output; 
        }

        private Frame CreateFrameFromKeyFrame(int frameIndex, GameSkeleton skeleton)
        {
            KeyFrame frame = DynamicFrames[frameIndex];
            var output = new Frame();

            for (int boneIndex = 0; boneIndex < frame.Position.Count(); boneIndex++)
            {
                var scale = GetAccumulatedBoneScale(boneIndex, frameIndex, skeleton);
                var transform = frame.Position[boneIndex] * scale;
                output.Transforms.Add(new RmvVector3(transform));

                var rot = frame.Rotation[boneIndex];
                output.Quaternion.Add(new RmvVector4(rot.X, rot.Y, rot.Z, rot.W));
            }

            return output;
        }

        float GetAccumulatedBoneScale(int boneIndex, int frameIndex, GameSkeleton skeleton)
        {
            var parentIndex = skeleton.GetParentBoneIndex(boneIndex);
            if (parentIndex == -1)
                return DynamicFrames[frameIndex].Scale[boneIndex].X;

            return GetAccumulatedBoneScale(parentIndex, frameIndex, skeleton) * DynamicFrames[frameIndex].Scale[boneIndex].X;
        }

        public AnimationClip Clone()
        {
            AnimationClip copy = new AnimationClip();
            foreach (var item in DynamicFrames)
                copy.DynamicFrames.Add(item.Clone());
            copy.PlayTimeInSec = PlayTimeInSec;
            
            return copy;
        }

        public static AnimationClip CreateSkeletonAnimation(GameSkeleton skeleton)
        {
            var clip = new AnimationClip();

            var frame = new KeyFrame();
            for (int i = 0; i < skeleton.BoneCount; i++)
            {
                frame.Position.Add(skeleton.Translation[i]);
                frame.Rotation.Add(skeleton.Rotation[i]);
                frame.Scale.Add(Vector3.One);
            }

            // Skeletons have two identical frames, dont know why
            clip.DynamicFrames.Add(frame.Clone());
            clip.DynamicFrames.Add(frame.Clone());

            clip.PlayTimeInSec = 0.1f;
            return clip;
        }

        public void ScaleAnimation(float scale)
        {
            foreach (var frame in DynamicFrames)
            {
                for (int i = 0; i < AnimationBoneCount; i++)
                    frame.Scale[i] = new Vector3(scale);
            }
        }
    }
}
