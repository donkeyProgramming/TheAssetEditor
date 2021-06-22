using Filetypes.RigidModel;
using Filetypes.RigidModel.Transforms;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Filetypes.RigidModel.AnimationFile;

namespace View3D.Animation
{
    public class AnimationClip
    {
        public class KeyFrame
        {
            public List<Vector3> Position { get; set; } = new List<Vector3>();
            public List<Quaternion> Rotation { get; set; } = new List<Quaternion>();

            public override string ToString()
            {
                return $"PosCount = {Position.Count}, RotCount = {Rotation.Count}";
            }

            public KeyFrame Clone()
            {
                return new KeyFrame()
                {
                    Position = new List<Vector3>(Position),
                    Rotation = new List<Quaternion>(Rotation)
                };
            }
        }


        public KeyFrame StaticFrame { get; set; } = null;
        public List<KeyFrame> DynamicFrames = new List<KeyFrame>();
        public float PlayTimeInSec { get; set; } = -1;

        public List<AnimationBoneMapping> RotationMappings { get; set; } = new List<AnimationBoneMapping>();
        public List<AnimationBoneMapping> TranslationMappings { get; set; } = new List<AnimationBoneMapping>();

        public AnimationClip() { }

        public AnimationClip(AnimationFile file)
        {
            RotationMappings = file.RotationMappings.ToList();
            TranslationMappings = file.TranslationMappings.ToList();
            PlayTimeInSec = file.Header.AnimationTotalPlayTimeInSec;

            if (file.StaticFrame != null)
                StaticFrame = CreateKeyFrame(file.StaticFrame);

            foreach (var frame in file.DynamicFrames)
                DynamicFrames.Add(CreateKeyFrame(frame));
        }

        KeyFrame CreateKeyFrame(AnimationFile.Frame frame)
        {
            var output = new KeyFrame();
            foreach (var translation in frame.Transforms)
                output.Position.Add(new Vector3(translation.X, translation.Y, translation.Z));

            foreach (var rotation in frame.Quaternion)
                output.Rotation.Add(new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W));

            return output;
        }

        public AnimationFile ConvertToFileFormat(GameSkeleton skeleton)
        {
            AnimationFile output = new AnimationFile();
            output.Header.AnimationType = 7;
            output.Header.AnimationTotalPlayTimeInSec = (DynamicFrames.Count() - 1) / output.Header.FrameRate;
            output.Header.SkeletonName = skeleton.SkeletonName;

            output.Bones = new BoneInfo[skeleton.BoneCount];
            for (int i = 0; i < skeleton.BoneCount; i++)
            {
                output.Bones[i] = new BoneInfo()
                {
                    Id = i,
                    Name = skeleton.BoneNames[i],
                    ParentId = skeleton.GetParentBone(i)
                };
            }

            // Mappings
            output.RotationMappings = RotationMappings.ToList();
            output.TranslationMappings = TranslationMappings.ToList();

            // Static
            if (StaticFrame != null)
                output.StaticFrame = CreateFrameFromKeyFrame(StaticFrame);

            // Dynamic
            foreach (var frame in DynamicFrames)
                output.DynamicFrames.Add(CreateFrameFromKeyFrame(frame));

            return output;
        }

        Frame CreateFrameFromKeyFrame(KeyFrame keyFrame)
        {
            var frame = new Frame();
            foreach (var trans in keyFrame.Position)
                frame.Transforms.Add(new RmvVector3(trans.X, trans.Y, trans.Z));

            foreach (var rot in keyFrame.Rotation)
                frame.Quaternion.Add(new RmvVector4(rot.X, rot.Y, rot.Z, rot.W));

            return frame;
        }

        public bool IsPoseClip()
        {
            var hasDynamicRoation = RotationMappings.Count(x => x.IsDynamic);
            var hasDynamicTranslation = TranslationMappings.Count(x => x.IsDynamic);
            return hasDynamicRoation == 0 && hasDynamicTranslation == 0;
        }

        public AnimationClip Clone()
        {
            AnimationClip copy = new AnimationClip();
            copy.PlayTimeInSec = PlayTimeInSec;

            foreach (var item in RotationMappings)
                copy.RotationMappings.Add(item.Clone());

            foreach (var item in TranslationMappings)
                copy.TranslationMappings.Add(item.Clone());
            
            if(StaticFrame != null)
                copy.StaticFrame = StaticFrame.Clone();

            foreach (var item in DynamicFrames)
                copy.DynamicFrames.Add(item.Clone());

            return copy;
        }


        public void MergeStaticAndDynamicFrames()
        {
            List<KeyFrame> newDynamicFrames = new List<KeyFrame>();

            var boneCount = RotationMappings.Count;
            var frameCount = DynamicFrames.Count;

            for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                var newKeyframe = new KeyFrame();

                for (int boneIndex = 0; boneIndex < boneCount; boneIndex++)
                {
                    var translationLookup = TranslationMappings[boneIndex];
                    if (translationLookup.IsDynamic)
                        newKeyframe.Position.Add(DynamicFrames[frameIndex].Position[translationLookup.Id]);
                    else if (translationLookup.IsStatic)
                        newKeyframe.Position.Add(StaticFrame.Position[translationLookup.Id]);

                    var rotationLookup = RotationMappings[boneIndex];
                    if (rotationLookup.IsDynamic)
                        newKeyframe.Rotation.Add(DynamicFrames[frameIndex].Rotation[rotationLookup.Id]);
                    else if (rotationLookup.IsStatic)
                        newKeyframe.Rotation.Add(StaticFrame.Rotation[rotationLookup.Id]);
                }

                newDynamicFrames.Add(newKeyframe);
            }

            // Update data
            var newRotMapping = new List<AnimationBoneMapping>();
            var newTransMappings = new List<AnimationBoneMapping>();
            var rotCounter = 0;
            var transCounter = 0;

            for (int i = 0; i < boneCount; i++)
            {
                var rotationLookup = RotationMappings[i];
                if (rotationLookup.HasValue)
                    newRotMapping.Add(new AnimationBoneMapping(rotCounter++));
                else
                    newRotMapping.Add(new AnimationBoneMapping(-1));

                var translationLookup = TranslationMappings[i];
                if (translationLookup.HasValue)
                    newTransMappings.Add(new AnimationBoneMapping(transCounter++));
                else
                    newTransMappings.Add(new AnimationBoneMapping(-1));
            }

            TranslationMappings = newTransMappings;
            RotationMappings = newRotMapping;
            DynamicFrames = newDynamicFrames;
            StaticFrame = new KeyFrame();
        }

        /// <summary>
        /// This function assumes that there are only dynamic frames
        /// </summary>
        /// <param name="bones"></param>
        public void LimitAnimationToSelectedBones(int[] bones)
        {
            bool hasStatic = (StaticFrame.Position.Count + StaticFrame.Rotation.Count) != 0;
            if (hasStatic)
                throw new Exception("This function does not work for animations that contains a static component");

            CreateMappingTable(this, bones, out var newRotMapping, out var newTransMapping);
            CreateDynamicFrames(bones, out var newDynamicFrames);

            RotationMappings = newRotMapping;
            TranslationMappings = newTransMapping;
            DynamicFrames = newDynamicFrames;

        }

        private void CreateDynamicFrames(int[] bones, out List<KeyFrame> newDynamicFrames)
        {
            newDynamicFrames = new List<KeyFrame>();

            var boneCount = bones.Length;
            var frameCount = DynamicFrames.Count;

            for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                var newKeyframe = new KeyFrame();

                for (int boneIndex = 0; boneIndex < boneCount; boneIndex++)
                {
                    // Find the org bone and look at the mapping
                    var orgBoneIndex = bones[boneIndex];

                    var translationLookup = TranslationMappings[orgBoneIndex];
                    if (translationLookup.IsDynamic)
                        newKeyframe.Position.Add(DynamicFrames[frameIndex].Position[translationLookup.Id]);
                    else if (translationLookup.IsStatic)
                        newKeyframe.Position.Add(StaticFrame.Position[translationLookup.Id]);

                    var rotationLookup = RotationMappings[orgBoneIndex];
                    if (rotationLookup.IsDynamic)
                        newKeyframe.Rotation.Add(DynamicFrames[frameIndex].Rotation[rotationLookup.Id]);
                    else if (rotationLookup.IsStatic)
                        newKeyframe.Rotation.Add(StaticFrame.Rotation[rotationLookup.Id]);
                }

                newDynamicFrames.Add(newKeyframe);
            }
        }

        private void CreateMappingTable(AnimationClip existingAnim, int[] bones, out List<AnimationBoneMapping> rotationMapping, out List<AnimationBoneMapping> translationMapping)
        {
            translationMapping = new List<AnimationBoneMapping>();
            rotationMapping = new List<AnimationBoneMapping>();
            
            int transDynamicCounter = 0;
            int rotDynamicCounter = 0;

            for (int boneIndex = 0; boneIndex < bones.Length; boneIndex++)
            {
                var originalBoneIndex = bones[boneIndex];

                // Translation
                var tanslationMappingValue = existingAnim.TranslationMappings[originalBoneIndex];

                if (tanslationMappingValue.IsStatic)
                    throw new Exception();
                else if (tanslationMappingValue.IsDynamic)
                    translationMapping.Add(new AnimationBoneMapping(transDynamicCounter++));
                else
                    translationMapping.Add(new AnimationBoneMapping(-1));

                var rotationMappingValue = existingAnim.RotationMappings[originalBoneIndex];
                if (rotationMappingValue.IsStatic)
                    throw new Exception();
                else if (rotationMappingValue.IsDynamic)
                    rotationMapping.Add(new AnimationBoneMapping(rotDynamicCounter++));
                else
                    rotationMapping.Add(new AnimationBoneMapping(-1));
            }
        }
    }
}
