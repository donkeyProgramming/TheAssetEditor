using GameWorld.Core.Services;
using Shared.Core.PackFiles;
using Shared.Core.Settings;
using Shared.GameFormats.AnimationPack;
using Shared.GameFormats.AnimationPack.AnimPackFileTypes;
using Shared.GameFormats.DB;
using Shared.Ui.Editors.TextEditor;

namespace Editors.AnimationFragmentEditor.AnimationPack.Converters.AnimationFragmentConverter
{
    public class AnimationFragmentFileToXmlConverter : XmlToBinaryConverter<Animation, AnimationFragmentFile>
    {
        readonly ISkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        readonly GameTypeEnum _preferedGame;

        public AnimationFragmentFileToXmlConverter(ISkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper, GameTypeEnum preferedGame)
        {
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _preferedGame = preferedGame;
        }

        protected override ITextConverter.SaveError Validate(Animation xmlAnimation, string text, IPackFileService pfs, string filepath) => Validator.Validate(_skeletonAnimationLookUpHelper, xmlAnimation, text, pfs, filepath);
      
        protected override Animation ConvertBinaryToXml(byte[] bytes)
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
                entry.WeaponBone = ValueConverterHelper.ConvertIntToBoolArray(item.WeaponBone);

                outputBin.AnimationFragmentEntry.Add(entry);
            }

            return outputBin;
        }

        protected override string CleanUpXml(string xmlText) => xmlText.Replace("</AnimationFragmentEntry>", "</AnimationFragmentEntry>\n");

        protected override byte[] ConvertXmlToBinary(Animation animation, string fileName)
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


    }
}
