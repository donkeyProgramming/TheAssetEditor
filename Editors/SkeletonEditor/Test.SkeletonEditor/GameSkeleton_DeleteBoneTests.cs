using GameWorld.Core.Animation;
using Shared.GameFormats.Animation;

namespace Test.SkeletonEditor
{
    internal class GameSkeleton_DeleteBoneTests
    {
        // A valid parent-before-child topological order that is NOT a depth-first pre-order
        // traversal of the tree: boneB's subtree (boneD) is not contiguous with boneB, because
        // boneC (a child of boneA) sits between boneB and boneD in the flat list.
        //   0 root
        //   ├─ 1 boneA
        //   │  └─ 3 boneC
        //   ├─ 2 boneB
        //   └─ 4 boneD (leaf, unrelated to the boneA/boneC/boneB relationship)
        static AnimationFile BuildAnimationFile()
        {
            return new AnimationFile
            {
                Bones =
                [
                    new AnimationFile.BoneInfo { Id = 0, Name = "root", ParentId = -1 },
                    new AnimationFile.BoneInfo { Id = 1, Name = "boneA", ParentId = 0 },
                    new AnimationFile.BoneInfo { Id = 2, Name = "boneB", ParentId = 0 },
                    new AnimationFile.BoneInfo { Id = 3, Name = "boneC", ParentId = 1 },
                    new AnimationFile.BoneInfo { Id = 4, Name = "boneD", ParentId = 0 },
                ]
            };
        }

        [Test]
        public void DeleteBone_PreservesRelativeOrderOfRemainingBones()
        {
            var skeleton = GameSkeleton.CreateFromAnimationFile(BuildAnimationFile(), null!);

            // Delete a leaf bone (boneD) that has no relation to the boneA/boneC/boneB bones.
            // Deleting it should not shuffle the order of the other, unrelated bones.
            skeleton.DeleteBone(skeleton.GetBoneIndexByName("boneD"));

            Assert.That(skeleton.BoneCount, Is.EqualTo(4));
            Assert.That(skeleton.BoneNames, Is.EqualTo(new[] { "root", "boneA", "boneB", "boneC" }));
            Assert.That(skeleton.GetParentBoneIndex(skeleton.GetBoneIndexByName("boneA")), Is.EqualTo(skeleton.GetBoneIndexByName("root")));
            Assert.That(skeleton.GetParentBoneIndex(skeleton.GetBoneIndexByName("boneB")), Is.EqualTo(skeleton.GetBoneIndexByName("root")));
            Assert.That(skeleton.GetParentBoneIndex(skeleton.GetBoneIndexByName("boneC")), Is.EqualTo(skeleton.GetBoneIndexByName("boneA")));
        }

        [Test]
        public void DeleteBone_CascadesToChildren()
        {
            var skeleton = GameSkeleton.CreateFromAnimationFile(BuildAnimationFile(), null!);

            // Deleting boneA should also remove its descendant, boneC.
            skeleton.DeleteBone(skeleton.GetBoneIndexByName("boneA"));

            Assert.That(skeleton.BoneCount, Is.EqualTo(3));
            Assert.That(skeleton.BoneNames, Is.EqualTo(new[] { "root", "boneB", "boneD" }));
            Assert.That(skeleton.GetParentBoneIndex(skeleton.GetBoneIndexByName("boneB")), Is.EqualTo(skeleton.GetBoneIndexByName("root")));
            Assert.That(skeleton.GetParentBoneIndex(skeleton.GetBoneIndexByName("boneD")), Is.EqualTo(skeleton.GetBoneIndexByName("root")));
        }
    }
}
