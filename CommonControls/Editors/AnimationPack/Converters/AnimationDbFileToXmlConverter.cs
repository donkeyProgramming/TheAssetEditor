using CommonControls.Common;
using CommonControls.Editors.TextEditor;
using CommonControls.FileTypes.AnimationPack.AnimPackFileTypes;
using CommonControls.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Xml;
using System.Xml.Serialization;

namespace CommonControls.Editors.AnimationPack.Converters
{
    public class AnimationDbFileToXmlConverter : ITextConverter
    {
        public string GetText(byte[] bytes)
        {
            try
            {
                var binFile = new AnimationDbFile("", bytes);
                var xmlBin = ConvertBinFileToXmlBin(binFile);

                var xmlserializer = new XmlSerializer(typeof(Bin));
                var stringWriter = new StringWriter();
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("", "");
                using (var writer = XmlWriter.Create(stringWriter, new XmlWriterSettings() { Indent = true, OmitXmlDeclaration=true }))
                {
                    xmlserializer.Serialize(writer, xmlBin, ns);
                    var str = stringWriter.ToString();
                    str = str.Replace("</BinEntry>", "</BinEntry>\n");
                    str = str.Replace("<Bin>", "<Bin>\n");
                    return str;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Unable to open file\n" + e.Message);
                return "";
            }
        }

        public byte[] ToBytes(string text, string filePath, PackFileService pfs, out ITextConverter.SaveError error)
        {
            var xmlserializer = new XmlSerializer(typeof(Bin));
            using var sr = new StringReader(text);
            using var reader = XmlReader.Create(sr);

            try
            {
                var errorHandler = new XmlSerializationErrorHandler();
                var obj = xmlserializer.Deserialize(reader, errorHandler.EventHandler) as Bin;

                if (errorHandler.Error != null)
                {
                    error = errorHandler.Error;
                    return null;
                }

                var binFile = ConvertXmlBinToBinFile(obj, filePath);

                error = null;
                return binFile.ToByteArray();
            }
            catch (Exception e)
            {
                var inner = ExceptionHelper.GetInnerMostException(e);
                if (inner is XmlException xmlException)
                    error = new ITextConverter.SaveError() { Text = xmlException.Message, ErrorLineNumber = xmlException.LineNumber, ErrorPosition = xmlException.LinePosition, ErrorLength = 0 };
                else if (inner != null)
                    error = new ITextConverter.SaveError() { Text = e.Message + " - " + inner.Message, ErrorLineNumber = 1 }; 
                else
                    error = new ITextConverter.SaveError() { Text = e.Message };
               
                return null;
            }
            
        }

        Bin ConvertBinFileToXmlBin(AnimationDbFile binFile)
        {
            var outputBin = new Bin();
            outputBin.BinEntry = new List<BinEntry>();

            // Version

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

        AnimationDbFile ConvertXmlBinToBinFile(Bin bin, string fileName)
        {
            var output = new AnimationDbFile(fileName);
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
            return output;
        }

        public bool ShouldShowLineNumbers() => true;
        public string GetSyntaxType() => "XML";
        public bool CanSaveOnError() => false;


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
