using System.Xml;
using System.Xml.Serialization;
using CommonControls.BaseDialogs.ErrorListDialog;
using GameWorld.Core.Services;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.Settings;
using Shared.GameFormats.AnimationPack;
using Shared.GameFormats.AnimationPack.AnimPackFileTypes;
using Shared.GameFormats.DB;
using Shared.Ui.Editors.TextEditor;

namespace CommonControls.Editors.AnimationPack.Converters
{

    public class AnimationFragmentFileToXmlConverter
        : BaseAnimConverter<AnimationFragmentFileToXmlConverter.Animation, AnimationFragmentFile>
    {
        private ISkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        GameTypeEnum _preferedGame;

        public AnimationFragmentFileToXmlConverter(ISkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, GameTypeEnum preferedGame)
        {
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _preferedGame = preferedGame;
        }

        protected override ITextConverter.SaveError Validate(Animation xmlAnimation, string text, IPackFileService pfs, string filepath)
        {
            if (string.IsNullOrWhiteSpace(xmlAnimation.Skeleton))
                return new ITextConverter.SaveError() { ErrorLength = 0, ErrorLineNumber = 1, ErrorPosition = 0, Text = "Missing skeleton item on root" };

            var lastIndex = 0;

            for (int i = 0; i < xmlAnimation.AnimationFragmentEntry.Count; i++)
            {
                var item = xmlAnimation.AnimationFragmentEntry[i];
                lastIndex = text.IndexOf("<AnimationFragmentEntry", lastIndex + 1, StringComparison.InvariantCultureIgnoreCase);

                if (item.Slot == null)
                    return GenerateError(text, lastIndex, "No slot provided");

                var slot = DefaultAnimationSlotTypeHelper.GetfromValue(item.Slot);
                if (slot == null)
                    return GenerateError(text, lastIndex, $"{item.Slot} is an invalid animation slot.");

                if (item.File == null)
                    return GenerateError(text, lastIndex, "No file item provided");

                if (item.Meta == null)
                    return GenerateError(text, lastIndex, "No meta item provided");

                if (item.Sound == null)
                    return GenerateError(text, lastIndex, "No sound item provided");

                if (item.BlendInTime == null)
                    return GenerateError(text, lastIndex, "No BlendInTime item provided");

                if (item.SelectionWeight == null)
                    return GenerateError(text, lastIndex, "No SelectionWeight item provided");

                if (item.WeaponBone == null)
                    return GenerateError(text, lastIndex, "No WeaponBone item provided");

                if (ValidateBoolArray(item.WeaponBone) == false)
                    return GenerateError(text, lastIndex, "WeaponBone bool array contains invalid values. Should contain 6 true/false values");
            }

            var errorList = new ErrorList();
            if (_skeletonAnimationLookUpHelper.GetSkeletonFileFromName(xmlAnimation.Skeleton) == null)
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

        protected override Animation ConvertBytesToXmlClass(byte[] bytes)
        {
            var fragmentFile = new AnimationFragmentFile("", bytes, _preferedGame);
            var outputBin = new Animation();
            outputBin.AnimationFragmentEntry = new List<AnimationEntry>();
            outputBin.Skeleton = fragmentFile.Skeletons.Values.FirstOrDefault();

            foreach (var item in fragmentFile.Fragments)
            {
                var entry = new AnimationEntry();
                entry.Slot = item.Slot.Value;
                entry.File = new ValueItem() { Value = item.AnimationFile };
                entry.Meta = new ValueItem() { Value = item.MetaDataFile };
                entry.Sound = new ValueItem() { Value = item.SoundMetaDataFile };
                entry.BlendInTime = new BlendInTime() { Value = item.BlendInTime };
                entry.SelectionWeight = new SelectionWeight() { Value = item.SelectionWeight };
                entry.Unknown = item.Unknown0;
                entry.WeaponBone = ConvertIntToBoolArray(item.WeaponBone);

                outputBin.AnimationFragmentEntry.Add(entry);
            }

            return outputBin;
        }

        protected override string CleanUpXml(string xmlText) => xmlText.Replace("</AnimationFragmentEntry>", "</AnimationFragmentEntry>\n");

        protected override byte[] ConvertToAnimClassBytes(Animation animation, string fileName)
        {
            var output = new AnimationFragmentFile(fileName, null, _preferedGame);
            output.Skeletons = new StringArrayTable(animation.Skeleton, animation.Skeleton);

            foreach (var item in animation.AnimationFragmentEntry)
            {
                var entry = new AnimationSetEntry()
                {
                    AnimationFile = item.File.Value,
                    MetaDataFile = item.Meta.Value,
                    SoundMetaDataFile = item.Sound.Value,
                    Comment = "",
                    BlendInTime = item.BlendInTime.Value,
                    Ignore = false,
                    SelectionWeight = item.SelectionWeight.Value,
                    Slot = _preferedGame == GameTypeEnum.Troy ? AnimationSlotTypeHelperTroy.GetfromValue(item.Slot) : DefaultAnimationSlotTypeHelper.GetfromValue(item.Slot),
                    Skeleton = animation.Skeleton,
                    Unknown0 = item.Unknown,
                };

                var unknown1Flags = item.WeaponBone.Split(",");
                for (int i = 0; i < 6; i++)
                    entry.SetWeaponBoneFlags(i, bool.Parse(unknown1Flags[i]));

                output.Fragments.Add(entry);
            }

            return output.ToByteArray();
        }


        [XmlRoot(ElementName = "BlendInTime")]
        public class BlendInTime
        {
            [XmlAttribute(AttributeName = "Value")]
            public float Value { get; set; }
        }

        [XmlRoot(ElementName = "SelectionWeight")]
        public class SelectionWeight
        {
            [XmlAttribute(AttributeName = "Value")]
            public float Value { get; set; }
        }

        public class ValueItem
        {
            [XmlAttribute(AttributeName = "Value")]
            public string Value { get; set; }
        }

        [XmlRoot(ElementName = "AnimationEntry")]
        public class AnimationEntry
        {
            [XmlElement(ElementName = "File")]
            public ValueItem File { get; set; }

            [XmlElement(ElementName = "Meta")]
            public ValueItem Meta { get; set; }

            [XmlElement(ElementName = "Sound")]
            public ValueItem Sound { get; set; }


            [XmlElement(ElementName = "BlendInTime")]
            public BlendInTime BlendInTime { get; set; }
            [XmlElement(ElementName = "SelectionWeight")]
            public SelectionWeight SelectionWeight { get; set; }

            [XmlElement(ElementName = "Unknown")]
            public int Unknown { get; set; }

            [XmlElement(ElementName = "WeaponBone")]
            public string WeaponBone { get; set; }

            [XmlAttribute(AttributeName = "Slot")]
            public string Slot { get; set; }
        }

        [XmlRoot(ElementName = "Animation")]
        public class Animation
        {
            [XmlElement(ElementName = "AnimationFragmentEntry")]
            public List<AnimationEntry> AnimationFragmentEntry { get; set; }
            [XmlAttribute(AttributeName = "skeleton")]
            public string Skeleton { get; set; }
        }
    }
}
