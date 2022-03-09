using CommonControls.Editors.TextEditor;
using CommonControls.FileTypes.AnimationPack;
using CommonControls.FileTypes.AnimationPack.AnimPackFileTypes;
using CommonControls.Services;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace CommonControls.Editors.AnimationPack.Converters
{
    public class AnimationBinWh3FileToXmlConverter : BaseAnimConverter<AnimationBinWh3FileToXmlConverter.XmlFormat, FileTypes.AnimationPack.AnimPackFileTypes.Wh3.AnimationBin>
    {
        protected override string CleanUpXml(string xmlText)
        {
            xmlText = xmlText.Replace("</BinEntry>", "</BinEntry>\n");
            xmlText = xmlText.Replace("<Bin>", "<Bin>\n");
            xmlText = xmlText.Replace("</GeneralBinData>", "</GeneralBinData>\n");
            return xmlText;
        }

        protected override XmlFormat ConvertBytesToXmlClass(byte[] bytes)
        {
            var binFile = new FileTypes.AnimationPack.AnimPackFileTypes.Wh3.AnimationBin("", bytes);
            var outputBin = new XmlFormat();

            outputBin.Version = "Wh3";
            outputBin.Data = new GeneralBinData()
            {
                TableVersion = binFile.TableVersion,
                TableSubVersion = binFile.TableSubVersion,
                Name = binFile.Name,
                MountBin = binFile.MountBin,
                SkeletonName = binFile.SkeletonName,
                LocomotionGraph = binFile.LocomotionGraph,
                UnknownValue1_RelatedToFlight = binFile.UnknownValue1
            };

            foreach (var animation in binFile.AnimationTableEntries)
            {
                outputBin.Animations.Add(new Animation()
                {
                    Slot = AnimationSlotTypeHelperWh3.GetFromId((int)animation.AnimationId).Value,
                    BlendId = animation.BlendIn,
                    BlendOut = animation.BlendOut,
                    Unk = animation.Unk,
                    WeaponBone = ConvertIntToBoolArray((int)animation.WeaponBools),
                });

                foreach (var animationRef in animation.AnimationRefs)
                {
                    outputBin.Animations.Last().Ref.Add(new Instance()
                    {
                        File = animationRef.AnimationFile,
                        Meta = animationRef.AnimationMetaFile,
                        Sound = animationRef.AnimationSoundMetaFile
                    });
                }
            }

            return outputBin;
        }

        protected override byte[] ConvertToAnimClassBytes(XmlFormat xmlBin, string fileName)
        {
            var binFile = new FileTypes.AnimationPack.AnimPackFileTypes.Wh3.AnimationBin("", null);

            binFile.TableVersion = xmlBin.Data.TableVersion;
            binFile.TableSubVersion = xmlBin.Data.TableSubVersion;
            binFile.Name = xmlBin.Data.Name;
            binFile.MountBin = xmlBin.Data.MountBin;
            binFile.SkeletonName = xmlBin.Data.SkeletonName;
            binFile.LocomotionGraph = xmlBin.Data.LocomotionGraph;
            binFile.UnknownValue1 = xmlBin.Data.UnknownValue1_RelatedToFlight;
            binFile.UnknownValue0 = "";

            foreach (var animationEntry in xmlBin.Animations)
            {
                binFile.AnimationTableEntries.Add(new FileTypes.AnimationPack.AnimPackFileTypes.Wh3.AnimationBinEntry() 
                { 
                    AnimationId = (uint)AnimationSlotTypeHelperWh3.GetfromValue(animationEntry.Slot).Id,
                    BlendIn = animationEntry.BlendId,
                    BlendOut = animationEntry.BlendOut,
                    WeaponBools = CreateWeaponFlagInt(animationEntry.WeaponBone),
                    Unk = animationEntry.Unk,
                });

                foreach (var animationInstance in animationEntry.Ref)
                {
                    binFile.AnimationTableEntries.Last().AnimationRefs.Add(new FileTypes.AnimationPack.AnimPackFileTypes.Wh3.AnimationBinEntry.AnimationRef()
                    {
                        AnimationFile = animationInstance.File,
                        AnimationMetaFile = animationInstance.Meta,
                        AnimationSoundMetaFile = animationInstance.Sound
                    });
                }
            }

            return binFile.ToByteArray();
        }


        protected override ITextConverter.SaveError Validate(XmlFormat type, string s, PackFileService pfs)
        {
            return null;
        }


        [XmlRoot(ElementName = "Instance")]
        public class Instance
        {
            [XmlAttribute(AttributeName = "File")]
            public string File { get; set; }
            [XmlAttribute(AttributeName = "Meta")]
            public string Meta { get; set; }
            [XmlAttribute(AttributeName = "Sound")]
            public string Sound { get; set; }
        }

        [XmlRoot(ElementName = "Animation")]
        public class Animation
        {
            [XmlElement(ElementName = "Instance")]
            public List<Instance> Ref { get; set; } = new List<Instance>();
            [XmlAttribute(AttributeName = "Slot")]
            public string Slot { get; set; }
            [XmlAttribute(AttributeName = "BlendId")]
            public float BlendId { get; set; }
            [XmlAttribute(AttributeName = "SelectionWeight")]
            public float BlendOut { get; set; }
            [XmlAttribute(AttributeName = "WeaponBone")]
            public string WeaponBone { get; set; }
            [XmlAttribute(AttributeName = "Unk")]
            public bool Unk { get; set; }
        }


        [XmlRoot(ElementName = "GeneralBinData")]
        public class GeneralBinData
        {
            public uint TableVersion { get; set; }
            public uint TableSubVersion { get; set; }

            public string Name { get; set; }
            public string MountBin { get; set; }
            public string SkeletonName { get; set; }
            public string LocomotionGraph { get; set; }
            public short UnknownValue1_RelatedToFlight { get; set; }
        }

        [XmlRoot(ElementName = "Bin")]
        public class XmlFormat
        {
            [XmlElement(ElementName = "Version")]
            public string Version { get; set; }

            [XmlElement(ElementName = "GeneralBinData")]
            public GeneralBinData Data { get; set; }

            [XmlElement(ElementName = "Animation")]
            public List<Animation> Animations { get; set; } = new List<Animation>();
        }
    }
}
