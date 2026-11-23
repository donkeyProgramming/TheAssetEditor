using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Shared.GameFormats.Animation;
using Shared.GameFormats.RigidModel.Transforms;
using static Shared.GameFormats.Animation.AnimationFile;


namespace GameWorld.Core.Animation
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
            get => (float)_playTimeUs / MicrosecondsPerSecond;
            set
            {
                if (DynamicFrames.Count == 0)
                {
                    _playTimeUs = (long)(value * MicrosecondsPerSecond);
                    return;
                }

                // make sure we have whole number of microsecond per frame
                var framePlayTimeUs = (long)Math.Ceiling(value / DynamicFrames.Count * MicrosecondsPerSecond);
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
            var targetBoneIndices = MatchBonesByName(file, skeleton);
            foreach (var animationPart in file.AnimationParts)
            {
                var frames = CreateKeyFramesFromAnimationPart(animationPart, skeleton, targetBoneIndices);
                DynamicFrames.AddRange(frames);
            }

            PlayTimeInSec = file.Header.AnimationTotalPlayTimeInSec;
        }

        // For each of the file's bones, the skeleton bone with the same name (-1 if it has none), so a track lands on
        // its own bone even when the file's bone order differs from the skeleton's. Null when the file's bones have
        // no names to go by; tracks then follow bone order, as before.
        static int[]? MatchBonesByName(AnimationFile file, GameSkeleton skeleton)
        {
            if (file.Bones == null || file.Bones.Length == 0 || file.Bones.Any(bone => string.IsNullOrEmpty(bone.Name)))
                return null;
            return file.Bones.Select(bone => skeleton.GetBoneIndexByName(bone.Name)).ToArray();
        }

        // Every frame has one entry per skeleton bone. Bones the file has no track for keep the skeleton's bind pose.
        List<KeyFrame> CreateKeyFramesFromAnimationPart(AnimationPart animationPart, GameSkeleton skeleton, int[]? targetBoneIndices)
        {
            var newDynamicFrames = new List<KeyFrame>();

            var animationSkeletonBoneCount = animationPart.RotationMappings.Count;
            var frameCount = animationPart.DynamicFrames.Count;

            if (frameCount == 0 && animationPart.StaticFrame != null)
                frameCount = 1; // Poses

            for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                var newKeyframe = new KeyFrame
                {
                    Position = new List<Vector3>(skeleton.Translation),
                    Rotation = new List<Quaternion>(skeleton.Rotation),
                    Scale = Enumerable.Repeat(Vector3.One, skeleton.BoneCount).ToList(),
                };

                for (var animationSkeletonBoneIndex = 0; animationSkeletonBoneIndex < animationSkeletonBoneCount; animationSkeletonBoneIndex++)
                {
                    // A file bone the skeleton doesn't have is discarded
                    var targetBoneIndex = targetBoneIndices == null
                        ? animationSkeletonBoneIndex
                        : animationSkeletonBoneIndex < targetBoneIndices.Length ? targetBoneIndices[animationSkeletonBoneIndex] : -1;
                    if (targetBoneIndex < 0 || targetBoneIndex >= skeleton.BoneCount)
                        continue;

                    var translationLookup = animationPart.TranslationMappings[animationSkeletonBoneIndex];
                    if (translationLookup.IsDynamic)
                        newKeyframe.Position[targetBoneIndex] = animationPart.DynamicFrames[frameIndex].Transforms[translationLookup.Id].ToVector3();
                    else if (translationLookup.IsStatic)
                        newKeyframe.Position[targetBoneIndex] = animationPart.StaticFrame.Transforms[translationLookup.Id].ToVector3();

                    var rotationLookup = animationPart.RotationMappings[animationSkeletonBoneIndex];
                    if (rotationLookup.IsDynamic)
                        newKeyframe.Rotation[targetBoneIndex] = animationPart.DynamicFrames[frameIndex].Quaternion[rotationLookup.Id].ToQuaternion();
                    else if (rotationLookup.IsStatic)
                        newKeyframe.Rotation[targetBoneIndex] = animationPart.StaticFrame.Quaternion[rotationLookup.Id].ToQuaternion();
                }

                newDynamicFrames.Add(newKeyframe);
            }

            return newDynamicFrames;
        }

        public AnimationFile ConvertToFileFormat(GameSkeleton skeleton) => ConvertToFileFormat(skeleton, 7);

        // Version 7 or 8. For 8, source is the file the clip was loaded from, if there is one. Its header word,
        // unknown_v8 and flags are kept and, while it has the same bones, so are its parts, its static/dynamic choices
        // and its packing, so an unedited clip saves back as it was read. unknownValueV8 and flagVariables override the source.
        public AnimationFile ConvertToFileFormat(GameSkeleton skeleton, uint version, uint? unknownValueV8 = null, IReadOnlyList<string>? flagVariables = null, AnimationFile? source = null)
        {
            if (version is not 7 and not 8)
                throw new ArgumentOutOfRangeException(nameof(version), version, "Only versions 7 and 8 can be created from a clip");

            var output = new AnimationFile();

            var fRate = (DynamicFrames.Count() - 1) / PlayTimeInSec;
            output.Header.FrameRate = (float)Math.Round(fRate);
            if (!(output.Header.FrameRate > 0))   // also NaN, from a single frame with no play time
                output.Header.FrameRate = 20;

            output.Header.Version = version;
            output.Header.AnimationTotalPlayTimeInSec = PlayTimeInSec;
            output.Header.SkeletonName = skeleton.SkeletonName;
            if (source != null)
            {
                output.Header.Unknown0_alwaysOne = source.Header.Unknown0_alwaysOne;
                output.Header.UnknownValue_v8 = source.Header.UnknownValue_v8;
                output.Header.FlagVariables = source.Header.FlagVariables.ToList();
            }
            if (unknownValueV8.HasValue)
                output.Header.UnknownValue_v8 = unknownValueV8.Value;
            if (flagVariables != null)
                output.Header.FlagVariables = flagVariables.ToList();
            output.Header.FlagCount = (uint)output.Header.FlagVariables.Count;

            output.Bones = new BoneInfo[skeleton.BoneCount];
            for (var i = 0; i < skeleton.BoneCount; i++)
            {
                output.Bones[i] = new BoneInfo()
                {
                    Id = i,
                    Name = skeleton.BoneNames[i],
                    ParentId = skeleton.GetParentBoneIndex(i)
                };
            }

            if (version == 8)
            {
                // A single frame becomes a static-only pose, which plays for 0.1s like the version 7 poses below
                if (DynamicFrames.Count == 1 && output.Header.AnimationTotalPlayTimeInSec <= 0)
                    output.Header.AnimationTotalPlayTimeInSec = 0.1f;
                AddVersionEightParts(output, skeleton, source);
                return output;
            }

            output.AnimationParts.Add(new AnimationPart());
            // One dynamic frame crashes the game. Vanilla poses keep every bone in the static frame and play for 0.1s
            var isPose = DynamicFrames.Count == 1;
            for (var i = 0; i < skeleton.BoneCount; i++)
            {
                var mappingValue = isPose ? StaticMappingOffset + i : i;
                output.AnimationParts[0].RotationMappings.Add(new AnimationBoneMapping(mappingValue));
                output.AnimationParts[0].TranslationMappings.Add(new AnimationBoneMapping(mappingValue));
            }

            if (isPose)
            {
                output.AnimationParts[0].StaticFrame = CreateFrameFromKeyFrame(0, skeleton);
                if (output.Header.AnimationTotalPlayTimeInSec <= 0)
                    output.Header.AnimationTotalPlayTimeInSec = 0.1f;
                return output;
            }

            for (var i = 0; i < DynamicFrames.Count; i++)
                output.AnimationParts[0].DynamicFrames.Add(CreateFrameFromKeyFrame(i, skeleton));

            return output;
        }

        const int StaticMappingOffset = 10000;

        void AddVersionEightParts(AnimationFile output, GameSkeleton skeleton, AnimationFile? source)
        {
            if (DynamicFrames.Count == 0)
                throw new InvalidOperationException("A version 8 animation needs at least one frame.");

            var frames = Enumerable.Range(0, DynamicFrames.Count).Select(i => CreateFrameFromKeyFrame(i, skeleton)).ToList();

            // The source's parts are only reused while it describes the same bones
            var sourceParts = source?.Header.Version == 8 && source.Bones.Select(b => b.Name).SequenceEqual(skeleton.BoneNames) ? source.AnimationParts : null;

            // The clip appends the parts' frames in order (a part with only a static frame gives one frame), so while the
            // frame count is unchanged it splits back the same way
            var segments = sourceParts?.Select(FramesInClip).ToList();
            if (sourceParts != null && segments!.Sum() == frames.Count)
            {
                // PlayTimeInSec rounds to whole microseconds per frame; keep the file's own timing while it still agrees
                if (Math.Abs(PlayTimeInSec - source!.Header.AnimationTotalPlayTimeInSec) < 0.001f)
                {
                    output.Header.AnimationTotalPlayTimeInSec = source.Header.AnimationTotalPlayTimeInSec;
                    output.Header.FrameRate = source.Header.FrameRate;
                }

                var start = 0;
                for (var i = 0; i < sourceParts.Count; i++)
                {
                    output.AnimationParts.Add(CreateVersionEightPart(frames.GetRange(start, segments[i]), skeleton, sourceParts[i], output.Header));
                    start += segments[i];
                }
                return;
            }

            // A one-part source keeps its packing even when the frame count changed: its rates and ranges are per bone
            var single = sourceParts?.Count == 1 ? sourceParts[0] : null;
            output.AnimationParts.Add(CreateVersionEightPart(frames, skeleton, single, output.Header));
        }

        static int FramesInClip(AnimationPart part) => part.DynamicFrames.Count != 0 ? part.DynamicFrames.Count : (part.StaticFrame != null ? 1 : 0);

        enum TrackKind { None, Static, Dynamic }

        // A track is static when it holds still across the frames and dynamic otherwise. With a source part, the part's
        // own choice wins wherever the data still allows it, including "none" for a track held at the bind pose.
        static AnimationPart CreateVersionEightPart(List<Frame> frames, GameSkeleton skeleton, AnimationPart? source, AnimationHeader header)
        {
            var boneCount = skeleton.BoneCount;
            var translationKinds = new TrackKind[boneCount];
            var rotationKinds = new TrackKind[boneCount];
            for (var bone = 0; bone < boneCount; bone++)
            {
                var translationStill = frames.Skip(1).All(f => NearlyEqual(frames[0].Transforms[bone], f.Transforms[bone]));
                var atBindTranslation = translationStill && NearlyEqual(frames[0].Transforms[bone], new RmvVector3(skeleton.Translation[bone]));
                translationKinds[bone] = ChooseKind(source?.TranslationMappings[bone], translationStill, atBindTranslation);

                var rotationStill = frames.Skip(1).All(f => NearlyEqual(frames[0].Quaternion[bone], f.Quaternion[bone]));
                var bind = skeleton.Rotation[bone];
                var atBindRotation = rotationStill && NearlyEqual(frames[0].Quaternion[bone], new RmvVector4(bind.X, bind.Y, bind.Z, bind.W));
                rotationKinds[bone] = ChooseKind(source?.RotationMappings[bone], rotationStill, atBindRotation);
            }

            // A part that moves needs at least one dynamic track, or it reads back as a single pose
            var moves = source == null ? frames.Count > 1 : source.DynamicFrames.Count != 0;
            if (moves && boneCount != 0 && !translationKinds.Contains(TrackKind.Dynamic) && !rotationKinds.Contains(TrackKind.Dynamic))
                translationKinds[0] = TrackKind.Dynamic;

            var part = new AnimationPart();
            if (translationKinds.Contains(TrackKind.Static) || rotationKinds.Contains(TrackKind.Static))
                part.StaticFrame = new Frame();
            if (translationKinds.Contains(TrackKind.Dynamic) || rotationKinds.Contains(TrackKind.Dynamic))
                part.DynamicFrames.AddRange(frames.Select(_ => new Frame()));
            else // CA writes the length the animation would have had
                part.DeclaredFrameCount = source?.DeclaredFrameCount ?? (int)Math.Round(header.AnimationTotalPlayTimeInSec * header.FrameRate) + 1;

            int staticTranslations = 0, staticRotations = 0, dynamicTranslations = 0, dynamicRotations = 0;
            for (var bone = 0; bone < boneCount; bone++)
            {
                switch (translationKinds[bone])
                {
                    case TrackKind.Static:
                        part.TranslationMappings.Add(new AnimationBoneMapping(StaticMappingOffset + staticTranslations++));
                        part.StaticFrame!.Transforms.Add(frames[0].Transforms[bone]);
                        break;
                    case TrackKind.Dynamic:
                        part.TranslationMappings.Add(new AnimationBoneMapping(dynamicTranslations++));
                        for (var f = 0; f < frames.Count; f++)
                            part.DynamicFrames[f].Transforms.Add(frames[f].Transforms[bone]);
                        break;
                    default:
                        part.TranslationMappings.Add(new AnimationBoneMapping(-1));
                        break;
                }

                switch (rotationKinds[bone])
                {
                    case TrackKind.Static:
                        part.RotationMappings.Add(new AnimationBoneMapping(StaticMappingOffset + staticRotations++));
                        part.StaticFrame!.Quaternion.Add(frames[0].Quaternion[bone]);
                        break;
                    case TrackKind.Dynamic:
                        part.RotationMappings.Add(new AnimationBoneMapping(dynamicRotations++));
                        for (var f = 0; f < frames.Count; f++)
                            part.DynamicFrames[f].Quaternion.Add(frames[f].Quaternion[bone]);
                        break;
                    default:
                        part.RotationMappings.Add(new AnimationBoneMapping(-1));
                        break;
                }
            }

            // The file's rates and ranges; the writer refits a range only where the values left it
            part.V8Packing = source?.V8Packing;
            return part;
        }

        static TrackKind ChooseKind(AnimationBoneMapping? source, bool still, bool atBindPose)
        {
            if (source != null)
            {
                if (source.IsDynamic)
                    return TrackKind.Dynamic;
                if (!source.HasValue && atBindPose)
                    return TrackKind.None;
                if (source.IsStatic && still)
                    return TrackKind.Static;
            }
            return still ? TrackKind.Static : TrackKind.Dynamic;
        }

        const float StillTolerance = 0.000001f;

        static bool NearlyEqual(RmvVector3 first, RmvVector3 second) =>
            MathF.Abs(first.X - second.X) <= StillTolerance && MathF.Abs(first.Y - second.Y) <= StillTolerance && MathF.Abs(first.Z - second.Z) <= StillTolerance;

        // q and -q are the same rotation
        static bool NearlyEqual(RmvVector4 first, RmvVector4 second)
        {
            var sameSign = MathF.Abs(first.X - second.X) <= StillTolerance && MathF.Abs(first.Y - second.Y) <= StillTolerance &&
                           MathF.Abs(first.Z - second.Z) <= StillTolerance && MathF.Abs(first.W - second.W) <= StillTolerance;
            var oppositeSign = MathF.Abs(first.X + second.X) <= StillTolerance && MathF.Abs(first.Y + second.Y) <= StillTolerance &&
                               MathF.Abs(first.Z + second.Z) <= StillTolerance && MathF.Abs(first.W + second.W) <= StillTolerance;
            return sameSign || oppositeSign;
        }

        private Frame CreateFrameFromKeyFrame(int frameIndex, GameSkeleton skeleton)
        {
            var frame = DynamicFrames[frameIndex];
            var output = new Frame();

            for (var boneIndex = 0; boneIndex < frame.Position.Count(); boneIndex++)
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
            var copy = new AnimationClip();
            foreach (var item in DynamicFrames)
                copy.DynamicFrames.Add(item.Clone());
            copy.PlayTimeInSec = PlayTimeInSec;

            return copy;
        }

        public static AnimationClip CreateSkeletonAnimation(GameSkeleton skeleton)
        {
            var clip = new AnimationClip();

            var frame = new KeyFrame();
            for (var i = 0; i < skeleton.BoneCount; i++)
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
                for (var i = 0; i < AnimationBoneCount; i++)
                    frame.Scale[i] = new Vector3(scale);
            }
        }
    }
}
