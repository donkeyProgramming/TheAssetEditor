using Shared.Core.PackFiles;
using Shared.GameFormats.AnimationPack.AnimPackFileTypes;
using Shared.Ui.Editors.TextEditor;

namespace Editors.AnimationFragmentEditor.AnimationPack.Converters.AnimationBinConverter
{
    public partial class AnimationBinFileToXmlConverter : XmlToBinaryConverter<AnimationBinFileToXmlConverter.Bin, AnimationBin>
    {
        protected override string CleanUpXml(string xmlText)
        {
            xmlText = xmlText.Replace("</BinEntry>", "</BinEntry>\n");
            xmlText = xmlText.Replace("<Bin>", "<Bin>\n");
            return xmlText;
        }

        protected override Bin ConvertBinaryToXml(byte[] bytes)
        {
            AnimationBin binFile = new AnimationBin("", bytes);
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

        protected override byte[] ConvertXmlToBinary(Bin bin, string fileName)
        {
            var output = new AnimationBin(fileName);
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


        protected override ITextConverter.SaveError Validate(Bin type, string s, IPackFileService pfs, string filepath) => null;
    }
}
