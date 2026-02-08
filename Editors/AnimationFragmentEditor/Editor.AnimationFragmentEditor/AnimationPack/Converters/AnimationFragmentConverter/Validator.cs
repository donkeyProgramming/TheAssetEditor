using CommonControls.BaseDialogs.ErrorListDialog;
using GameWorld.Core.Services;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.GameFormats.AnimationPack;
using Shared.Ui.Editors.TextEditor;

namespace Editors.AnimationFragmentEditor.AnimationPack.Converters.AnimationFragmentConverter
{
    public static class Validator
    {
        public static ITextConverter.SaveError Validate(ISkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, Animation xmlAnimation, string text, IPackFileService pfs, string filepath)
        {
            if (string.IsNullOrWhiteSpace(xmlAnimation.Skeleton))
                return new ITextConverter.SaveError() { ErrorLength = 0, ErrorLineNumber = 1, ErrorPosition = 0, Text = "Missing skeleton item on root" };

            var lastIndex = 0;

            for (int i = 0; i < xmlAnimation.AnimationFragmentEntry.Count; i++)
            {
                var item = xmlAnimation.AnimationFragmentEntry[i];
                lastIndex = text.IndexOf("<AnimationFragmentEntry", lastIndex + 1, StringComparison.InvariantCultureIgnoreCase);

                if (item.Slot == null)
                    return ITextConverter.GenerateError(text, lastIndex, "No slot provided");

                var slot = DefaultAnimationSlotTypeHelper.GetfromValue(item.Slot);
                if (slot == null)
                    return ITextConverter.GenerateError(text, lastIndex, $"{item.Slot} is an invalid animation slot.");

                if (item.File == null)
                    return ITextConverter.GenerateError(text, lastIndex, "No file item provided");

                if (item.Meta == null)
                    return ITextConverter.GenerateError(text, lastIndex, "No meta item provided");

                if (item.Sound == null)
                    return ITextConverter.GenerateError(text, lastIndex, "No sound item provided");

                if (item.BlendInTime == null)
                    return ITextConverter.GenerateError(text, lastIndex, "No BlendInTime item provided");

                if (item.SelectionWeight == null)
                    return ITextConverter.GenerateError(text, lastIndex, "No SelectionWeight item provided");

                if (item.WeaponBone == null)
                    return ITextConverter.GenerateError(text, lastIndex, "No WeaponBone item provided");

                if (ValueConverterHelper.ValidateBoolArray(item.WeaponBone) == false)
                    return ITextConverter.GenerateError(text, lastIndex, "WeaponBone bool array contains invalid values. Should contain 6 true/false values");
            }

            var errorList = new ErrorList();
            if (skeletonAnimationLookUpHelper.GetSkeletonFileFromName(xmlAnimation.Skeleton) == null)
                errorList.Warning("Root", $"Skeleton {xmlAnimation.Skeleton} is not found");

            foreach (var item in xmlAnimation.AnimationFragmentEntry)
            {
                if (string.IsNullOrWhiteSpace(item.File.Value))
                    errorList.Warning(item.Slot, "Item does not have an animation");

                if (pfs.FindFile(item.File.Value) == null)
                    errorList.Warning(item.Slot, $"Animation {item.File.Value} is not found");

                if (item.Meta.Value != "" && pfs.FindFile(item.Meta.Value) == null)
                    errorList.Warning(item.Slot, $"Meta {item.Meta.Value} is not found");

                if (item.Sound.Value != "" && pfs.FindFile(item.Sound.Value) == null)
                    errorList.Warning(item.Slot, $"Sound {item.Sound.Value} is not found");
            }

            if (errorList.Errors.Count != 0)
                ErrorListWindow.ShowDialog("Errors", errorList, false);

            return null;
        }
    }
}
