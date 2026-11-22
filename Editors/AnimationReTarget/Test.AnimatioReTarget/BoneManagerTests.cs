using Editors.AnimatioReTarget.Editor.BoneHandling;
using Editors.AnimatioReTarget.Editor.BoneHandling.Presentation;
using GameWorld.Core.Services;
using Moq;
using Shared.Core.Misc;
using Shared.Core.Services;
using Shared.GameFormats.Animation;

namespace Test.AnimatioReTarget
{
    /// <summary>The selected-bone panel's "Is relative to" picker, its Clear button, and
    /// "Clear selected bone settings".</summary>
    [TestFixture]
    public class BoneManagerTests
    {
        Mock<IStandardDialogs> _dialogs = null!;
        BoneManager _manager = null!;

        [SetUp]
        public void SetUp()
        {
            var skeleton = new AnimationFile();
            skeleton.Bones =
            [
                new AnimationFile.BoneInfo { Id = 0, Name = "root", ParentId = -1 },
                new AnimationFile.BoneInfo { Id = 1, Name = "spine", ParentId = 0 },
                new AnimationFile.BoneInfo { Id = 2, Name = "hand", ParentId = 1 },
                new AnimationFile.BoneInfo { Id = 3, Name = "tail", ParentId = 0 },
            ];

            var lookUp = new Mock<ISkeletonAnimationLookUpHelper>();
            lookUp.Setup(x => x.GetSkeletonFileFromName("source")).Returns(skeleton);
            _dialogs = new Mock<IStandardDialogs>();
            _manager = new BoneManager(_dialogs.Object, Mock.Of<IAbstractFormFactory<BoneMappingWindow>>(), lookUp.Object);
            _manager.UpdateSourceSkeleton("source");
        }

        [Test]
        public void FlatBones_ListsEveryBoneDepthFirst()
        {
            Assert.That(_manager.FlatBones.Select(x => x.BoneName), Is.EqualTo(new[] { "root", "spine", "hand", "tail" }));
        }

        [Test]
        public void ClearRelativeBone_UnsetsTheSelectedBonesRelativeBone()
        {
            var hand = _manager.FlatBones[2];
            _manager.SelectedBone = hand;
            hand.SelectedRelativeBone = _manager.FlatBones[1];

            _manager.ClearRelativeBoneCommand.Execute(null);

            Assert.That(hand.SelectedRelativeBone, Is.Null);
        }

        [Test]
        public void ResetSelectedBone_RestoresDefaultsAndKeepsTheMapping()
        {
            var hand = _manager.FlatBones[2];
            _manager.SelectedBone = hand;
            hand.MappedIndex = 7;
            hand.HasMapping = true;
            hand.BoneLengthMult = 2;
            hand.RotationOffset = new(10, 20, 30);
            hand.TranslationOffset = new(1, 2, 3);
            hand.ForceSnapToWorld = true;
            hand.FreezeTranslation = true;
            hand.FreezeRotation = true;
            hand.FreezeRotationZ = true;
            hand.ApplyTranslation = false;
            hand.ApplyRotation = false;
            hand.SelectedRelativeBone = _manager.FlatBones[1];

            _manager.ResetSelectedBoneCommand.Execute(null);

            Assert.That(hand.BoneLengthMult, Is.EqualTo(1));
            Assert.That((hand.RotationOffset.X.Value, hand.RotationOffset.Y.Value, hand.RotationOffset.Z.Value), Is.EqualTo((0d, 0d, 0d)));
            Assert.That((hand.TranslationOffset.X.Value, hand.TranslationOffset.Y.Value, hand.TranslationOffset.Z.Value), Is.EqualTo((0d, 0d, 0d)));
            Assert.That((hand.ForceSnapToWorld, hand.FreezeTranslation, hand.FreezeRotation, hand.FreezeRotationZ), Is.EqualTo((false, false, false, false)));
            Assert.That((hand.ApplyTranslation, hand.ApplyRotation), Is.EqualTo((true, true)));
            Assert.That(hand.SelectedRelativeBone, Is.Null);
            Assert.That((hand.MappedIndex, hand.HasMapping), Is.EqualTo((7, true)));
            _dialogs.Verify(x => x.ShowDialogBox(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }
}
