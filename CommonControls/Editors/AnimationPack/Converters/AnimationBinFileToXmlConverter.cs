using CommonControls.Editors.TextEditor;
using CommonControls.FileTypes.AnimationPack.AnimPackFileTypes;
using CommonControls.Services;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace CommonControls.Editors.AnimationPack.Converters
{
    public class AnimationBinFileToXmlConverter : BaseAnimConverter<AnimationBinFileToXmlConverter.Bin, FileTypes.AnimationPack.AnimPackFileTypes.AnimationBin>  
    {
        protected override string CleanUpXml(string xmlText)
        {
            xmlText = xmlText.Replace("</BinEntry>", "</BinEntry>\n");
            xmlText = xmlText.Replace("<Bin>", "<Bin>\n");
            return xmlText;
        }

        protected override  Bin ConvertBytesToXmlClass(byte[] bytes)
        {
            FileTypes.AnimationPack.AnimPackFileTypes.AnimationBin binFile = new FileTypes.AnimationPack.AnimPackFileTypes.AnimationBin("", bytes);
            var outputBin = new Bin();
            outputBin.BinEntry = new List<BinEntry>();

            foreach (var item in binFile.AnimationTableEntries)
            {
                var entry = new BinEntry();
                entry.Name = item.Name;
                entry.Fragments = string.Join(", ", item.FragmentReferences.Select(x => x.Name));
                entry.Skeleton = new Skeleton() { Value = item.SkeletonName };
                entry.MountSkeleton = new MountSkeleton() { Value = item.MountName };
                entry.Unknown = new Unknown() { Value = item.Unknown };
                outputBin.BinEntry.Add(entry);
            }

            return outputBin;
        }

        protected override byte[] ConvertToAnimClassBytes(Bin bin, string fileName)
        {
            var output = new FileTypes.AnimationPack.AnimPackFileTypes.AnimationBin(fileName);
            foreach (var item in bin.BinEntry)
            {
                var entry = new AnimationBinEntry(item.Name, item.Skeleton.Value, item.MountSkeleton.Value)
                {
                    Unknown = item.Unknown.Value
                };

                var refs = item.Fragments.Split(",");
                foreach (var refInstance in refs)
                {
                    var str = refInstance.Trim();
                    if (string.IsNullOrEmpty(str) == false)
                        entry.FragmentReferences.Add(new AnimationBinEntry.FragmentReference() { Name = str, Unknown = 0 });
                }

                output.AnimationTableEntries.Add(entry);
            }
            return output.ToByteArray();
        }


        protected override ITextConverter.SaveError Validate(Bin type, string s, PackFileService pfs)
        {
            return null;
        }

        [XmlRoot(ElementName = "Skeleton")]
        public class Skeleton
        {
            [XmlAttribute(AttributeName = "value")]
            public string Value { get; set; }
        }

        [XmlRoot(ElementName = "MountSkeleton")]
        public class MountSkeleton
        {
            [XmlAttribute(AttributeName = "value")]
            public string Value { get; set; }
        }

        [XmlRoot(ElementName = "Unknown")]
        public class Unknown
        {
            [XmlAttribute(AttributeName = "value")]
            public short Value { get; set; }
        }

        [XmlRoot(ElementName = "BinEntry")]
        public class BinEntry
        {
            [XmlElement(ElementName = "Skeleton")]
            public Skeleton Skeleton { get; set; }
            [XmlElement(ElementName = "MountSkeleton")]
            public MountSkeleton MountSkeleton { get; set; }
            [XmlElement(ElementName = "Fragments")]
            public string Fragments { get; set; }
            [XmlElement(ElementName = "Unknown")]
            public Unknown Unknown { get; set; }
            [XmlAttribute(AttributeName = "name")]
            public string Name { get; set; }
        }

        [XmlRoot(ElementName = "Bin")]
        public class Bin
        {
            [XmlElement(ElementName = "BinEntry")]
            public List<BinEntry> BinEntry { get; set; }
        }
    }
}
