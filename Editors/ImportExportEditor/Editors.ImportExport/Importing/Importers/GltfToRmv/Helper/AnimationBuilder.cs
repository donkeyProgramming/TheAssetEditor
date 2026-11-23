using System.IO;
using System.Numerics;
using Editors.ImportExport.Exporting.Exporters.RmvToGltf.Helpers;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.Animation;
using Shared.GameFormats.RigidModel.Transforms;
using SharpGLTF.Schema2;
using static Shared.GameFormats.Animation.AnimationFile;
using Xna = Microsoft.Xna.Framework;

namespace Editors.ImportExport.Importing.Importers.GltfToRmv.Helper;

public record AnimationBuilderSettings(
    ModelRoot ModelRoot,
    string SkeletonName,
    float KeysPerSecond,
    IPackFileContainer PackFileContainer,
    string PackPath,
    bool AutoDetectKeysPerSecond = true);

public class AnimationBuilder
{
    private const long MaxAnimationTransformSamples = 5_000_000;

    public static AnimationFile Build(
        AnimationBuilderSettings settings,
        AnimationFile skeletonAnimFile,
        Animation animation,
        Action<int, int>? reportProgress = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!float.IsFinite(settings.KeysPerSecond) || settings.KeysPerSecond <= 0)
            throw new ArgumentOutOfRangeException(nameof(settings.KeysPerSecond), "The animation sample rate must be greater than 0.");
        if (skeletonAnimFile.Bones.Length == 0)
            throw new InvalidDataException("The target game skeleton has no bones.");
        if (skeletonAnimFile.AnimationParts.Count == 0 ||
            skeletonAnimFile.AnimationParts[0].DynamicFrames.Count == 0)
        {
            throw new InvalidDataException("The target game skeleton has no bind pose, so missing glTF animation channels can't be filled in.");
        }
        if (animation.Channels.Count == 0)
        {
            throw new InvalidDataException(
                "The glTF animation has no bone channels, so no valid .anim file can be made.");
        }

        var keysPerSecond = settings.AutoDetectKeysPerSecond
            ? DetectSamplingRate(settings.ModelRoot, animation) ?? settings.KeysPerSecond
            : settings.KeysPerSecond;
        var roundedIntervalCount = Math.Round(
            (double)animation.Duration * keysPerSecond,
            MidpointRounding.AwayFromZero);
        if (!double.IsFinite(roundedIntervalCount) ||
            roundedIntervalCount > int.MaxValue - 1)
        {
            throw new InvalidDataException(
                "The sampled glTF animation has too many frames and bones; import stopped to avoid running out of memory. Shorten the animation or lower the sample rate.");
        }

        var intervalCount = Math.Max(0, (int)roundedIntervalCount);
        var keyCount = intervalCount + 1;
        if ((long)keyCount * skeletonAnimFile.Bones.Length >
            MaxAnimationTransformSamples)
        {
            throw new InvalidDataException(
                "The sampled glTF animation has too many frames and bones; import stopped to avoid running out of memory. Shorten the animation or lower the sample rate.");
        }

        var newAnimFile = new AnimationFile
        {
            Header = new AnimationHeader
            {
                Version = 7,
                FrameRate = keysPerSecond,
                SkeletonName = settings.SkeletonName,
                // Same convention as AnimationClip: play time spans the intervals between keys.
                // A single key gets the 0.1 s AnimationClip gives static poses.
                AnimationTotalPlayTimeInSec = intervalCount == 0 ? 0.1f : (float)(intervalCount / keysPerSecond),
            },
            Bones = skeletonAnimFile.Bones,
            AnimationParts = [new AnimationPart()],
        };

        var part = newAnimFile.AnimationParts[0];
        part.DynamicFrames = [];
        DoQuantization(skeletonAnimFile, part);
        var skinRetargeter = SkinAnimationRetargeter.TryCreate(
            settings.ModelRoot,
            skeletonAnimFile,
            animation);
        reportProgress?.Invoke(0, keyCount);

        for (var keyIndex = 0; keyIndex < keyCount; keyIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var keyTime = (float)(keyIndex / (double)keysPerSecond);
            var frame = new Frame();
            FillFrame(
                settings.ModelRoot,
                animation,
                skeletonAnimFile,
                skinRetargeter,
                keyTime,
                frame);
            part.DynamicFrames.Add(frame);
            var completed = keyIndex + 1;
            if (ShouldReportProgress(completed, keyCount))
                reportProgress?.Invoke(completed, keyCount);
        }

        return newAnimFile;
    }

    private static bool ShouldReportProgress(int completed, int total)
    {
        if (completed == total)
            return true;

        var interval = Math.Max(1, total / 100);
        return completed % interval == 0;
    }

    private static float? DetectSamplingRate(ModelRoot modelRoot, Animation animation)
    {
        IReadOnlyList<float>? mostDetailedKeyTimes = null;

        void Consider<T>(IAnimationSampler<T> sampler)
        {
            var keyTimes = sampler.InterpolationMode == AnimationInterpolationMode.CUBICSPLINE
                ? sampler.GetCubicKeys().Select(key => key.Key).ToArray()
                : sampler.GetLinearKeys().Select(key => key.Key).ToArray();
            if (mostDetailedKeyTimes == null || keyTimes.Length > mostDetailedKeyTimes.Count)
                mostDetailedKeyTimes = keyTimes;
        }

        foreach (var node in modelRoot.LogicalNodes)
        {
            var rotationChannel = animation.FindRotationChannel(node);
            if (rotationChannel != null)
                Consider(rotationChannel.GetRotationSampler());

            var translationChannel = animation.FindTranslationChannel(node);
            if (translationChannel != null)
                Consider(translationChannel.GetTranslationSampler());

            var scaleChannel = animation.FindScaleChannel(node);
            if (scaleChannel != null)
                Consider(scaleChannel.GetScaleSampler());
        }

        if (mostDetailedKeyTimes == null || mostDetailedKeyTimes.Count < 3)
            return null;

        var keyInterval = mostDetailedKeyTimes[1] - mostDetailedKeyTimes[0];
        if (!float.IsFinite(keyInterval) || keyInterval <= 0)
            return null;

        var intervalTolerance = Math.Max(0.00001f, keyInterval * 0.001f);
        for (var keyIndex = 2; keyIndex < mostDetailedKeyTimes.Count; keyIndex++)
        {
            var interval = mostDetailedKeyTimes[keyIndex] - mostDetailedKeyTimes[keyIndex - 1];
            if (Math.Abs(interval - keyInterval) > intervalTolerance)
                return null;
        }

        var detectedRate = 1.0f / keyInterval;
        if (!float.IsFinite(detectedRate) || detectedRate < 1.0f || detectedRate > 240.0f)
            return null;

        var roundedRate = MathF.Round(detectedRate);
        return Math.Abs(detectedRate - roundedRate) <= 0.01f
            ? roundedRate
            : detectedRate;
    }

    private static void DoQuantization(AnimationFile skeletonAnimFile, AnimationPart part)
    {
        for (var boneIndex = 0; boneIndex < skeletonAnimFile.Bones.Length; boneIndex++)
        {
            part.TranslationMappings.Add(new AnimationBoneMapping(boneIndex));
            part.RotationMappings.Add(new AnimationBoneMapping(boneIndex));
        }
    }

    private static void FillFrame(
        ModelRoot modelRoot,
        Animation animation,
        AnimationFile skeletonAnimFile,
        SkinAnimationRetargeter? skinRetargeter,
        float currentKeyTime,
        Frame frame)
    {
        if (skinRetargeter != null)
        {
            skinRetargeter.FillFrame(animation, currentKeyTime, frame);
            return;
        }

        var translations = new RmvVector3[skeletonAnimFile.Bones.Length];
        var quaternions = new RmvVector4[skeletonAnimFile.Bones.Length];
        var bindPose = skeletonAnimFile.AnimationParts[0].DynamicFrames[0];

        for (var boneIndex = 0; boneIndex < skeletonAnimFile.Bones.Length; boneIndex++)
        {
            var bone = skeletonAnimFile.Bones[boneIndex];
            var fallbackTranslation = bindPose.Transforms[boneIndex].ToVector3();
            var fallbackRotation = bindPose.Quaternion[boneIndex].ToQuaternion();
            var translation = GltfAnimationTrackSampler.SampleTranslation(
                modelRoot,
                animation,
                bone.Name,
                currentKeyTime,
                fallbackTranslation);
            var quaternion = GltfAnimationTrackSampler.SampleQuaternion(
                modelRoot,
                animation,
                bone.Name,
                currentKeyTime,
                fallbackRotation);

            translations[boneIndex] = new RmvVector3(translation);
            quaternions[boneIndex] = new RmvVector4(
                quaternion.X,
                quaternion.Y,
                quaternion.Z,
                quaternion.W);
        }

        frame.Transforms = translations.ToList();
        frame.Quaternion = quaternions.ToList();
    }

    private sealed class SkinAnimationRetargeter
    {
        private const float EquivalentBindTransformTolerance = 0.0001f;
        private const float SourceScaleTolerance = 0.0001f;
        private const float RetargetedScaleTolerance = 0.0002f;

        private sealed record JointBinding(
            Node Joint,
            Matrix4x4 InverseBindMatrix,
            bool HasScaleChannel,
            bool HasTranslationChannel,
            bool HasRotationChannel);

        private readonly AnimationFile _skeleton;
        private readonly JointBinding?[] _jointBindings;
        private readonly Matrix4x4[] _sourceBindWorld;
        private readonly Matrix4x4[] _inverseSourceDefaultWorld;
        private readonly Matrix4x4[] _targetBindLocal;
        private readonly Matrix4x4[] _targetBindWorld;
        private readonly bool _useSourceLocalTracks;

        private SkinAnimationRetargeter(
            AnimationFile skeleton,
            Skin skin,
            Animation animation)
        {
            _skeleton = skeleton;
            _jointBindings = new JointBinding?[skeleton.Bones.Length];
            _sourceBindWorld = new Matrix4x4[skeleton.Bones.Length];
            _inverseSourceDefaultWorld = new Matrix4x4[skeleton.Bones.Length];
            _targetBindLocal = new Matrix4x4[skeleton.Bones.Length];
            _targetBindWorld = new Matrix4x4[skeleton.Bones.Length];

            var boneIndexesByName = skeleton.Bones
                .Select((bone, index) => (bone.Name, index))
                .ToDictionary(
                    item => item.Name,
                    item => item.index,
                    StringComparer.OrdinalIgnoreCase);
            for (var jointIndex = 0; jointIndex < skin.JointsCount; jointIndex++)
            {
                var joint = skin.GetJoint(jointIndex);
                if (boneIndexesByName.TryGetValue(joint.Joint.Name, out var boneIndex))
                {
                    _jointBindings[boneIndex] = new JointBinding(
                        joint.Joint,
                        joint.InverseBindMatrix,
                        animation.FindScaleChannel(joint.Joint) != null,
                        animation.FindTranslationChannel(joint.Joint) != null,
                        animation.FindRotationChannel(joint.Joint) != null);
                }
            }

            BuildSourceBindTransforms();
            BuildTargetBindMatrices();
            _useSourceLocalTracks = CanUseSourceLocalTracks();
        }

        public static SkinAnimationRetargeter? TryCreate(
            ModelRoot modelRoot,
            AnimationFile skeleton,
            Animation animation)
        {
            var boneNames = new HashSet<string>(
                skeleton.Bones.Select(bone => bone.Name),
                StringComparer.OrdinalIgnoreCase);
            var candidates = modelRoot.LogicalSkins
                .Select(skin => new
                {
                    Skin = skin,
                    Joints = Enumerable.Range(0, skin.JointsCount)
                        .Select(index => skin.GetJoint(index).Joint)
                        .ToHashSet(),
                    MatchCount = Enumerable.Range(0, skin.JointsCount)
                        .Count(index => boneNames.Contains(
                            skin.GetJoint(index).Joint.Name)),
                })
                .Where(candidate => candidate.MatchCount > 0)
                .ToList();
            if (candidates.Count == 0)
                return null;

            var selected = candidates
                .Where(candidate => animation.Channels.All(channel =>
                    IsBoneTransformChannel(channel) &&
                    candidate.Joints.Contains(channel.TargetNode!) &&
                    boneNames.Contains(channel.TargetNode!.Name)))
                .OrderByDescending(candidate => candidate.MatchCount)
                .FirstOrDefault();
            if (selected == null)
            {
                var targetNames = animation.Channels
                    .Select(GetChannelTargetName)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase);
                throw new InvalidDataException(
                    string.Format("glTF animation \"{0}\" targets nodes that don't belong to the same skeleton: {1}.",
                        GetAnimationName(animation),
                        string.Join("、", targetNames)));
            }

            return new SkinAnimationRetargeter(skeleton, selected.Skin, animation);
        }

        public void FillFrame(
            Animation animation,
            float time,
            Frame frame)
        {
            if (_useSourceLocalTracks)
            {
                FillSourceLocalFrame(animation, time, frame);
                return;
            }

            var desiredWorld = new Matrix4x4[_skeleton.Bones.Length];
            var localTranslations = new Vector3[_skeleton.Bones.Length];
            var localRotations = new Quaternion[_skeleton.Bones.Length];
            var visitStates = new byte[_skeleton.Bones.Length];

            Matrix4x4 BuildSourceAnimationDelta(int boneIndex)
            {
                var binding = _jointBindings[boneIndex];
                if (binding == null)
                    return Matrix4x4.Identity;

                return _inverseSourceDefaultWorld[boneIndex] *
                       GetVisualWorldMatrix(binding.Joint, animation, time);
            }

            Matrix4x4 BuildDesiredWorld(int boneIndex)
            {
                if (visitStates[boneIndex] == 2)
                    return desiredWorld[boneIndex];
                if (visitStates[boneIndex] == 1)
                    throw new InvalidDataException(
                        string.Format("The target game skeleton has a parent cycle at bone {0}.",
                            boneIndex));

                visitStates[boneIndex] = 1;
                var parentIndex = GetValidatedParentIndex(boneIndex);
                var parentWorld = parentIndex < 0
                    ? Matrix4x4.Identity
                    : BuildDesiredWorld(parentIndex);
                var binding = _jointBindings[boneIndex];
                var retargetedWorld = binding == null
                    ? _targetBindLocal[boneIndex] * parentWorld
                    : _targetBindWorld[boneIndex] *
                      BuildSourceAnimationDelta(boneIndex);
                var localTransform = retargetedWorld;
                if (parentIndex >= 0)
                {
                    if (!Matrix4x4.Invert(parentWorld, out var inverseParent))
                    {
                        throw new InvalidDataException(
                            $"The glTF animation has a transform that can't be inverted at bone \"{_skeleton.Bones[parentIndex].Name}\".");
                    }

                    localTransform *= inverseParent;
                }

                if (HasShear(localTransform))
                {
                    throw new InvalidDataException(
                        string.Format("glTF animation \"{0}\" shears bone \"{1}\" in the frame sampled at {2:0.###} s; game .anim files can't store shear.",
                            GetAnimationName(animation),
                            _skeleton.Bones[boneIndex].Name,
                            time));
                }
                if (!Matrix4x4.Decompose(
                        localTransform,
                        out var scale,
                        out var rotation,
                        out var translation))
                {
                    throw new InvalidDataException(
                        $"The glTF animation can't be split into translation and rotation at bone \"{_skeleton.Bones[boneIndex].Name}\".");
                }
                var hasUnitSourceScale = binding?.HasScaleChannel != true ||
                    IsUnitScale(
                        binding.Joint.GetLocalTransform(animation, time).Scale,
                        SourceScaleTolerance);
                // Blender can emit explicit unit scale keys while tiny bind-pose
                // differences force the animation through world-space retargeting.
                var retargetedScaleTolerance =
                    binding?.HasScaleChannel == true && hasUnitSourceScale
                        ? RetargetedScaleTolerance
                        : SourceScaleTolerance;
                if (!hasUnitSourceScale ||
                    !IsUnitScale(scale, retargetedScaleTolerance))
                {
                    throw new InvalidDataException(
                        string.Format("glTF animation \"{0}\" scales bone \"{1}\" in the frame sampled at {2:0.###} s; game .anim files can't store scale.",
                            GetAnimationName(animation),
                            _skeleton.Bones[boneIndex].Name,
                            time));
                }

                Matrix4x4.Decompose(
                    _targetBindLocal[boneIndex],
                    out _,
                    out var bindRotation,
                    out var bindTranslation);
                if (binding == null || !binding.HasTranslationChannel)
                    translation = bindTranslation;
                if (binding == null || !binding.HasRotationChannel)
                    rotation = bindRotation;

                rotation = Quaternion.Normalize(rotation);
                localTranslations[boneIndex] = translation;
                localRotations[boneIndex] = rotation;
                desiredWorld[boneIndex] =
                    Matrix4x4.CreateFromQuaternion(rotation) *
                    Matrix4x4.CreateTranslation(translation) *
                    parentWorld;
                visitStates[boneIndex] = 2;
                return desiredWorld[boneIndex];
            }

            for (var boneIndex = 0; boneIndex < _skeleton.Bones.Length; boneIndex++)
                BuildDesiredWorld(boneIndex);

            frame.Transforms = new List<RmvVector3>(_skeleton.Bones.Length);
            frame.Quaternion = new List<RmvVector4>(_skeleton.Bones.Length);
            for (var boneIndex = 0; boneIndex < _skeleton.Bones.Length; boneIndex++)
            {
                var translation = localTranslations[boneIndex];
                var rotation = localRotations[boneIndex];
                var gameTranslation = new Xna.Vector3(
                    -translation.X,
                    translation.Y,
                    translation.Z);
                var gameRotation = new Xna.Quaternion(
                    rotation.X,
                    -rotation.Y,
                    -rotation.Z,
                    rotation.W);
                frame.Transforms.Add(new RmvVector3(gameTranslation));
                frame.Quaternion.Add(new RmvVector4(
                    gameRotation.X,
                    gameRotation.Y,
                    gameRotation.Z,
                    gameRotation.W));
            }
        }

        private void FillSourceLocalFrame(
            Animation animation,
            float time,
            Frame frame)
        {
            frame.Transforms = new List<RmvVector3>(_skeleton.Bones.Length);
            frame.Quaternion = new List<RmvVector4>(_skeleton.Bones.Length);
            var bindFrame = _skeleton.AnimationParts[0].DynamicFrames[0];
            for (var boneIndex = 0; boneIndex < _skeleton.Bones.Length; boneIndex++)
            {
                var binding = _jointBindings[boneIndex];
                var gameTranslation = bindFrame.Transforms[boneIndex].ToVector3();
                var gameRotation = bindFrame.Quaternion[boneIndex].ToQuaternion();
                if (binding != null)
                {
                    var sourceTransform = binding.Joint.GetLocalTransform(animation, time);
                    if (binding.HasScaleChannel &&
                        !IsUnitScale(sourceTransform.Scale, SourceScaleTolerance))
                    {
                        throw new InvalidDataException(
                            string.Format("glTF animation \"{0}\" scales bone \"{1}\" in the frame sampled at {2:0.###} s; game .anim files can't store scale.",
                                GetAnimationName(animation),
                                _skeleton.Bones[boneIndex].Name,
                                time));
                    }

                    if (binding.HasTranslationChannel)
                    {
                        gameTranslation = new Xna.Vector3(
                            -sourceTransform.Translation.X,
                            sourceTransform.Translation.Y,
                            sourceTransform.Translation.Z);
                    }
                    if (binding.HasRotationChannel)
                    {
                        gameRotation = new Xna.Quaternion(
                            sourceTransform.Rotation.X,
                            -sourceTransform.Rotation.Y,
                            -sourceTransform.Rotation.Z,
                            sourceTransform.Rotation.W);
                    }
                }

                frame.Transforms.Add(new RmvVector3(gameTranslation));
                frame.Quaternion.Add(new RmvVector4(
                    gameRotation.X,
                    gameRotation.Y,
                    gameRotation.Z,
                    gameRotation.W));
            }
        }

        private bool CanUseSourceLocalTracks()
        {
            var boundJoints = _jointBindings
                .Where(binding => binding != null)
                .Select(binding => binding!.Joint)
                .ToHashSet();
            for (var boneIndex = 0; boneIndex < _skeleton.Bones.Length; boneIndex++)
            {
                var binding = _jointBindings[boneIndex];
                if (binding == null)
                {
                    return false;
                }
                if (!AreNearlyEqual(binding.Joint.LocalMatrix, _targetBindLocal[boneIndex]))
                {
                    return false;
                }
                if (!AreNearlyEqual(
                        GetVisualWorldMatrix(binding.Joint),
                        _sourceBindWorld[boneIndex]))
                {
                    return false;
                }

                var parentIndex = GetValidatedParentIndex(boneIndex);
                var expectedParentJoint = parentIndex < 0
                    ? null
                    : _jointBindings[parentIndex]?.Joint;
                var visualParent = binding.Joint.VisualParent;
                while (visualParent != expectedParentJoint)
                {
                    if (visualParent == null ||
                        boundJoints.Contains(visualParent) ||
                        !AreNearlyEqual(visualParent.LocalMatrix, Matrix4x4.Identity))
                    {
                        return false;
                    }

                    visualParent = visualParent.VisualParent;
                }
            }

            return true;
        }

        private void BuildSourceBindTransforms()
        {
            for (var boneIndex = 0; boneIndex < _skeleton.Bones.Length; boneIndex++)
            {
                var binding = _jointBindings[boneIndex];
                if (binding == null)
                    continue;
                if (!Matrix4x4.Invert(
                        binding.InverseBindMatrix,
                        out _sourceBindWorld[boneIndex]))
                {
                    throw new InvalidDataException(
                        string.Format("The inverse bind matrix of glTF bone \"{0}\" can't be inverted.",
                            binding.Joint.Name));
                }
                if (!Matrix4x4.Invert(
                        GetVisualWorldMatrix(binding.Joint),
                        out _inverseSourceDefaultWorld[boneIndex]))
                {
                    throw new InvalidDataException(
                        string.Format("The parent transform of glTF bone \"{0}\" can't be inverted.",
                            binding.Joint.Name));
                }
            }

            for (var boneIndex = 0; boneIndex < _skeleton.Bones.Length; boneIndex++)
            {
                var binding = _jointBindings[boneIndex];
                if (binding == null)
                    continue;

                var sourceBindLocal = _sourceBindWorld[boneIndex];
                var parentIndex = GetValidatedParentIndex(boneIndex);
                if (parentIndex >= 0 && _jointBindings[parentIndex] != null)
                {
                    if (!Matrix4x4.Invert(
                            _sourceBindWorld[parentIndex],
                            out var inverseParent))
                    {
                        throw new InvalidDataException(
                            string.Format("The bind transform of glTF bone \"{0}\" can't be inverted.",
                                _skeleton.Bones[parentIndex].Name));
                    }

                    sourceBindLocal *= inverseParent;
                }

                if (HasShear(sourceBindLocal))
                {
                    throw new InvalidDataException(
                        string.Format("The bind transform of glTF bone \"{0}\" contains shear, which an .anim skeleton can't store.",
                            binding.Joint.Name));
                }
                if (!Matrix4x4.Decompose(
                        sourceBindLocal,
                        out _,
                        out _,
                        out _))
                {
                    throw new InvalidDataException(
                        string.Format("The bind transform of glTF bone \"{0}\" can't be decomposed.",
                            binding.Joint.Name));
                }
            }
        }

        private void BuildTargetBindMatrices()
        {
            var bindFrame = _skeleton.AnimationParts[0].DynamicFrames[0];
            for (var boneIndex = 0; boneIndex < _skeleton.Bones.Length; boneIndex++)
            {
                var gameTranslation = bindFrame.Transforms[boneIndex].ToVector3();
                // Quantized game rotations must not introduce scale or shear into retargeting.
                var gameRotation = Xna.Quaternion.Normalize(
                    bindFrame.Quaternion[boneIndex].ToQuaternion());
                _targetBindLocal[boneIndex] =
                    Matrix4x4.CreateFromQuaternion(new Quaternion(
                        gameRotation.X,
                        -gameRotation.Y,
                        -gameRotation.Z,
                        gameRotation.W)) *
                    Matrix4x4.CreateTranslation(
                        -gameTranslation.X,
                        gameTranslation.Y,
                        gameTranslation.Z);
            }

            var visitStates = new byte[_skeleton.Bones.Length];
            Matrix4x4 BuildWorld(int boneIndex)
            {
                if (visitStates[boneIndex] == 2)
                    return _targetBindWorld[boneIndex];
                if (visitStates[boneIndex] == 1)
                    throw new InvalidDataException(
                        string.Format("The target game skeleton has a parent cycle at bone {0}.",
                            boneIndex));

                visitStates[boneIndex] = 1;
                var parentIndex = GetValidatedParentIndex(boneIndex);
                _targetBindWorld[boneIndex] = parentIndex < 0
                    ? _targetBindLocal[boneIndex]
                    : _targetBindLocal[boneIndex] * BuildWorld(parentIndex);
                visitStates[boneIndex] = 2;
                return _targetBindWorld[boneIndex];
            }

            for (var boneIndex = 0; boneIndex < _skeleton.Bones.Length; boneIndex++)
                BuildWorld(boneIndex);
        }

        private int GetValidatedParentIndex(int boneIndex)
        {
            var parentIndex = _skeleton.Bones[boneIndex].ParentId;
            if (parentIndex < -1 || parentIndex >= _skeleton.Bones.Length)
            {
                throw new InvalidDataException(
                    $"Bone {boneIndex} of the target game skeleton has an invalid parent index {parentIndex}.");
            }
            if (parentIndex == boneIndex)
            {
                throw new InvalidDataException(
                    $"Bone {boneIndex} of the target game skeleton is its own parent.");
            }

            return parentIndex;
        }

        private static bool IsBoneTransformChannel(AnimationChannel channel) =>
            channel.TargetNode != null &&
            channel.TargetNodePath is PropertyPath.scale or
                PropertyPath.rotation or
                PropertyPath.translation;

        private static string GetChannelTargetName(AnimationChannel channel) =>
            !string.IsNullOrWhiteSpace(channel.TargetNode?.Name)
                ? channel.TargetNode.Name
                : channel.TargetPointerPath ??
                  "unknown target";

        private static string GetAnimationName(Animation animation) =>
            string.IsNullOrWhiteSpace(animation.Name)
                ? $"animation_{animation.LogicalIndex + 1}"
                : animation.Name;

        private static bool HasShear(Matrix4x4 matrix)
        {
            var x = new Vector3(matrix.M11, matrix.M12, matrix.M13);
            var y = new Vector3(matrix.M21, matrix.M22, matrix.M23);
            var z = new Vector3(matrix.M31, matrix.M32, matrix.M33);
            if (x.LengthSquared() <= 0 || y.LengthSquared() <= 0 || z.LengthSquared() <= 0)
                return false;

            x = Vector3.Normalize(x);
            y = Vector3.Normalize(y);
            z = Vector3.Normalize(z);
            return Math.Abs(Vector3.Dot(x, y)) > 0.0001f ||
                   Math.Abs(Vector3.Dot(x, z)) > 0.0001f ||
                   Math.Abs(Vector3.Dot(y, z)) > 0.0001f;
        }

        private static bool IsUnitScale(Vector3 scale, float tolerance) =>
            Math.Abs(scale.X - 1) <= tolerance &&
            Math.Abs(scale.Y - 1) <= tolerance &&
            Math.Abs(scale.Z - 1) <= tolerance;

        private static bool AreNearlyEqual(
            Matrix4x4 left,
            Matrix4x4 right) =>
            Math.Abs(left.M11 - right.M11) <= EquivalentBindTransformTolerance &&
            Math.Abs(left.M12 - right.M12) <= EquivalentBindTransformTolerance &&
            Math.Abs(left.M13 - right.M13) <= EquivalentBindTransformTolerance &&
            Math.Abs(left.M14 - right.M14) <= EquivalentBindTransformTolerance &&
            Math.Abs(left.M21 - right.M21) <= EquivalentBindTransformTolerance &&
            Math.Abs(left.M22 - right.M22) <= EquivalentBindTransformTolerance &&
            Math.Abs(left.M23 - right.M23) <= EquivalentBindTransformTolerance &&
            Math.Abs(left.M24 - right.M24) <= EquivalentBindTransformTolerance &&
            Math.Abs(left.M31 - right.M31) <= EquivalentBindTransformTolerance &&
            Math.Abs(left.M32 - right.M32) <= EquivalentBindTransformTolerance &&
            Math.Abs(left.M33 - right.M33) <= EquivalentBindTransformTolerance &&
            Math.Abs(left.M34 - right.M34) <= EquivalentBindTransformTolerance &&
            Math.Abs(left.M41 - right.M41) <= EquivalentBindTransformTolerance &&
            Math.Abs(left.M42 - right.M42) <= EquivalentBindTransformTolerance &&
            Math.Abs(left.M43 - right.M43) <= EquivalentBindTransformTolerance &&
            Math.Abs(left.M44 - right.M44) <= EquivalentBindTransformTolerance;

        private static Matrix4x4 GetVisualWorldMatrix(
            Node node,
            Animation? animation = null,
            float time = 0)
        {
            Matrix4x4 GetLocalMatrix(Node current)
            {
                if (animation == null)
                    return current.LocalMatrix;

                var transform = current.GetLocalTransform(animation, time);
                return Matrix4x4.CreateScale(transform.Scale) *
                       Matrix4x4.CreateFromQuaternion(transform.Rotation) *
                       Matrix4x4.CreateTranslation(transform.Translation);
            }

            var worldMatrix = GetLocalMatrix(node);
            for (var parent = node.VisualParent; parent != null; parent = parent.VisualParent)
                worldMatrix *= GetLocalMatrix(parent);

            return worldMatrix;
        }
    }
}
