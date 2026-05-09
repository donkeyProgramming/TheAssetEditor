using Editor.CampaignAnimationCreator.CampaignAnimationCreator.Commands;
using Editors.Shared.Core.Common.ReferenceModel;
using GameWorld.Core.Animation;
using Microsoft.Xna.Framework;

namespace Test.CampaignAnimationCreator
{
    public class ConvertCampaignAnimationCommandTests
    {
        [Test]
        public void TryExecute_ValidBone_ResetsOnlySelectedBoneTransform()
        {
            var command = new ConvertCampaignAnimationCommand();
            var clip = CreateSampleClip();
            var selectedBone = new SkeletonBoneNode { BoneIndex = 1, BoneName = "animroot" };

            var result = command.Execute(clip, selectedBone, out var converted, out var errorText);

            Assert.That(result, Is.True);
            Assert.That(errorText, Is.Null);
            Assert.That(converted, Is.Not.Null);

            foreach (var frame in converted!.DynamicFrames)
            {
                Assert.That(frame.Position[1], Is.EqualTo(Vector3.Zero));
                Assert.That(frame.Rotation[1], Is.EqualTo(Quaternion.Identity));

                Assert.That(frame.Position[0], Is.Not.EqualTo(Vector3.Zero));
                Assert.That(frame.Rotation[0], Is.Not.EqualTo(Quaternion.Identity));
            }
        }

        [Test]
        public void TryExecute_OutOfRangeBone_ReturnsError()
        {
            var command = new ConvertCampaignAnimationCommand();
            var clip = CreateSampleClip();
            var selectedBone = new SkeletonBoneNode { BoneIndex = 5, BoneName = "invalid" };

            var result = command.Execute(clip, selectedBone, out var converted, out var errorText);

            Assert.That(result, Is.False);
            Assert.That(converted, Is.Null);
            Assert.That(errorText, Is.Not.Null.And.Contains("out of range"));
        }

        private static AnimationClip CreateSampleClip()
        {
            var clip = new AnimationClip();
            clip.PlayTimeInSec = 0.2f;

            clip.DynamicFrames.Add(CreateFrame(new Vector3(10, 11, 12), new Vector3(20, 21, 22)));
            clip.DynamicFrames.Add(CreateFrame(new Vector3(30, 31, 32), new Vector3(40, 41, 42)));

            return clip;
        }

        private static AnimationClip.KeyFrame CreateFrame(Vector3 bone0Position, Vector3 bone1Position)
        {
            return new AnimationClip.KeyFrame
            {
                Position = new List<Vector3> { bone0Position, bone1Position },
                Rotation = new List<Quaternion>
                {
                    Quaternion.CreateFromYawPitchRoll(0.1f, 0.2f, 0.3f),
                    Quaternion.CreateFromYawPitchRoll(0.4f, 0.5f, 0.6f)
                },
                Scale = new List<Vector3> { Vector3.One, Vector3.One }
            };
        }
    }
}