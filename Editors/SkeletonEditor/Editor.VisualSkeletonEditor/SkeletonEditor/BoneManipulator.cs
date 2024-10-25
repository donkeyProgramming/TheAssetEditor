using System.Windows;
using Editors.Shared.Core.Common.ReferenceModel;
using GameWorld.Core.Animation;
using Microsoft.Xna.Framework;
using Shared.Core.Misc;

namespace Editor.VisualSkeletonEditor.SkeletonEditor
{
    class BoneCopyPasteItem : ICopyPastItem
    {
        public string Description { get; set; } = "Copy object for bones";
        public GameSkeleton SourceSkeleton { get; set; }
        public int BoneIndex { get; set; }
    }

    class BoneManipulator
    {
        public static void Duplicate(SkeletonBoneNode selectedBone, GameSkeleton skeleton)
        {
            if (selectedBone == null)
                return;

            // Create the bone
            var parentBoneIndex = skeleton.GetParentBoneIndex(selectedBone.BoneIndex);
            if (parentBoneIndex == -1)
                return;

            skeleton.CreateChildBone(parentBoneIndex);

            // Copy data
            var copyIndex = selectedBone.BoneIndex;
            var newBoneIndex = skeleton.BoneCount - 1;
            skeleton.BoneNames[newBoneIndex] = skeleton.BoneNames[copyIndex] + "_cpy";
            skeleton.Translation[newBoneIndex] = skeleton.Translation[copyIndex];
            skeleton.Rotation[newBoneIndex] = skeleton.Rotation[copyIndex];
            skeleton.RebuildSkeletonMatrix();

        }

        public static void Delete(SkeletonBoneNode selectedBone, GameSkeleton skeleton)
        {
            if (selectedBone == null)
                return;

            skeleton.DeleteBone(selectedBone.BoneIndex);
        }


        public static void Copy(CopyPasteManager copyPasteManager, SkeletonBoneNode selectedBone, GameSkeleton skeleton)
        {
            if (selectedBone == null)
            {
                MessageBox.Show("No bone selected");
                return;
            }

            var copyItem = new BoneCopyPasteItem()
            {
                BoneIndex = selectedBone.BoneIndex,
                SourceSkeleton = skeleton.Clone()
            };
            copyPasteManager.SetCopyItem(copyItem);
        }

        public static void Paste(CopyPasteManager copyPasteManager, SkeletonBoneNode selectedBone, GameSkeleton skeleton)
        {
            if (selectedBone == null)
                return;

            var pasteObject = copyPasteManager.GetPasteObject<BoneCopyPasteItem>();
            if (pasteObject == null)
            {
                MessageBox.Show("No valid object found to paste");
                return;
            }

            PasteBones(pasteObject.SourceSkeleton, pasteObject.BoneIndex, skeleton, selectedBone.BoneIndex, true);
            skeleton.RebuildSkeletonMatrix();
        }

        static void PasteBones(GameSkeleton source, int sourceIndex, GameSkeleton target, int targetIndex, bool setUsingWorldTransform = false)
        {
            target.CreateChildBone(targetIndex);
            var newBoneIndex = target.BoneCount - 1;

            target.BoneNames[newBoneIndex] = source.BoneNames[sourceIndex];
            if (setUsingWorldTransform == false)
            {
                target.Translation[newBoneIndex] = source.Translation[sourceIndex];
                target.Rotation[newBoneIndex] = source.Rotation[sourceIndex];
            }
            else
            {
                var parentTransform = target.GetWorldTransform(targetIndex);
                var world = source.GetWorldTransform(sourceIndex);

                var localSpaceMatrix = world * Matrix.Invert(parentTransform);
                localSpaceMatrix.Decompose(out _, out var quaternionValue, out var translationValue);

                target.Translation[newBoneIndex] = Vector3.Zero;
                target.Rotation[newBoneIndex] = quaternionValue;
            }

            var sourceChildBones = source.GetDirectChildBones(sourceIndex);
            foreach (var childBone in sourceChildBones)
                PasteBones(source, childBone, target, newBoneIndex);
        }
    }
}
