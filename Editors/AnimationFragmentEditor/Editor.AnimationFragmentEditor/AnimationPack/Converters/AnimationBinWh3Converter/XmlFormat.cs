using System.Xml;
using System.Xml.Serialization;

namespace Editors.AnimationFragmentEditor.AnimationPack.Converters.AnimationBinWh3Converter
{


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
