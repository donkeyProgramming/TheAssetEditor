using CommonControls.Editors.TextEditor;
using CommonControls.FileTypes.AnimationPack.AnimPackFileTypes;
using CommonControls.Services;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

namespace CommonControls.Editors.AnimationPack.Converters
{
    public class AnimationSet3kFileToXmlConverter : BaseAnimConverter<AnimationSet3kFileToXmlConverter.AnimationSet, AnimationSet3kFile>  
    {
        private SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;

        public AnimationSet3kFileToXmlConverter(SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper)
        {
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
        }

        protected override string CleanUpXml(string xmlText)
        {
            xmlText = xmlText.Replace("Game=\"3k\">", "Game=\"3k\">\n");
            xmlText = xmlText.Replace("</Config>", "</Config>\n");
            xmlText = xmlText.Replace("</Fragment>", "</Fragment>\n");

            return xmlText;
        }

        protected override ITextConverter.SaveError Validate(AnimationSet type, string s, PackFileService pfs)
        {
            return null;
        }

        protected override AnimationSet ConvertBytesToXmlClass(byte[] bytes)
        {
            var animSetFile = new AnimationSet3kFile("", bytes);
            var output = new AnimationSet();
            output.Game = "3k";
            output.Version = animSetFile.Version;
            output.Config = new Config()
            {
                IsSimpleFlight = animSetFile.IsSimpleFlight,
                MountSkeleton = animSetFile.MountSkeleton,
                FragmentName = animSetFile.FragmentName,
                SkeletonName = animSetFile.SkeletonName,
                MountFragment = animSetFile.MountFragment
            };

            output.Fragment = new List<Fragment>();
            foreach (var animSetRow in animSetFile.Entries)
            {
                var frg = new Fragment()
                {
                    BlendInTime = animSetRow.BlendWeight,
                    SelectionWeight = animSetRow.SelectionWeight,
                    Slot = animSetRow.Slot,
                    WeaponBone = ConvertIntToBoolArray(animSetRow.WeaponBone),
                    Flag = animSetRow.Flag,
                    Entry = new List<Entry>()
                };

                foreach (var animation in animSetRow.Animations)
                {
                    frg.Entry.Add(new Entry()
                    {
                        File = new ValueItem() { Value = animation.AnimationFile },
                        Meta = new ValueItem() { Value = animation.MetaFile },
                        Sound = new ValueItem() { Value = animation.SoundMeta }
                    });
                }

                output.Fragment.Add(frg);
            }

            return output;
        }

        protected override byte[] ConvertToAnimClassBytes(AnimationSet xmlType, string path)
        {
            var animFile = new AnimationSet3kFile("", null)
            {
                Version = xmlType.Version,
                IsSimpleFlight = xmlType.Config.IsSimpleFlight,
                MountFragment = xmlType.Config.MountFragment,
                MountSkeleton = xmlType.Config.MountSkeleton,
                FragmentName = xmlType.Config.FragmentName,
                SkeletonName = xmlType.Config.SkeletonName,
                Entries = new List<AnimationSet3kFile.AnimationSetEntry>()
            };

            foreach (var item in xmlType.Fragment)
            {
                var entry = new AnimationSet3kFile.AnimationSetEntry()
                {
                    Flag = item.Flag,
                    BlendWeight = item.BlendInTime,
                    Slot = item.Slot,
                    SelectionWeight = item.SelectionWeight,
                };
 
                var unknown1Flags = item.WeaponBone.Split(",");
                for (int i = 0; i < 6; i++)
                    entry.SetWeaponBoneFlags(i, bool.Parse(unknown1Flags[i]));

                foreach (var anim in item.Entry)
                {
                    entry.Animations.Add(new AnimationSet3kFile.AnimationSetEntry.AnimationEntry()
                    { 
                        AnimationFile = anim.File.Value,
                        MetaFile = anim.Meta.Value,
                        SoundMeta = anim.Meta.Value
                    });
                }
                animFile.Entries.Add(entry);
            }

            return animFile.ToByteArray(); ;
        }


        // -----------------

        [XmlRoot(ElementName = "Config")]
        public class Config
        {
            [XmlElement(ElementName = "MountSkeleton")]
            public string MountSkeleton { get; set; }

            [XmlElement(ElementName = "FragmentName")]
            public string FragmentName { get; set; }

            [XmlElement(ElementName = "SkeletonName")]
            public string SkeletonName { get; set; }

            [XmlElement(ElementName = "IsSimpleFlight")]
            public bool IsSimpleFlight { get; set; }

            [XmlElement(ElementName = "MountFragment")]
            public string MountFragment { get; set; }
        }

        public class ValueItem
        {
            [XmlAttribute(AttributeName = "Value")]
            public string Value { get; set; }
        }


        [XmlRoot(ElementName = "Entry")]
        public class Entry
        {
            [XmlElement(ElementName = "File")]
            public ValueItem File { get; set; }
            [XmlElement(ElementName = "Meta")]
            public ValueItem Meta { get; set; }
            [XmlElement(ElementName = "Sound")]
            public ValueItem Sound { get; set; }
        }

        [XmlRoot(ElementName = "Fragment")]
        public  class Fragment
        {
            [XmlElement(ElementName = "WeaponBone")]
            public string WeaponBone { get; set; }
            [XmlElement(ElementName = "Entry")]
            public List<Entry> Entry { get; set; }
            [XmlAttribute(AttributeName = "Slot")]
            public uint Slot { get; set; }
            [XmlAttribute(AttributeName = "BlendInTime")]
            public float BlendInTime { get; set; }
            [XmlAttribute(AttributeName = "SelectionWeight")]
            public float SelectionWeight { get; set; }
            [XmlAttribute(AttributeName = "Flag")]
            public bool Flag { get; set; }
        }

        [XmlRoot(ElementName = "AnimationSet")]
        public class AnimationSet
        {
            [XmlElement(ElementName = "Config")]
            public Config Config { get; set; }

            [XmlElement(ElementName = "Fragment")]
            public List<Fragment> Fragment { get; set; }

            [XmlAttribute(AttributeName = "Version")]
            public uint Version { get; set; }

            [XmlAttribute(AttributeName = "Game")]
            public string Game { get; set; }
        }

    }
}
