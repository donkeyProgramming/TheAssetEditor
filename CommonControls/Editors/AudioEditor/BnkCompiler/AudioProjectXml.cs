using System.Xml.Serialization;
using System.Collections.Generic;
namespace CommonControls.Editors.AudioEditor.BnkCompiler
{
    public interface IHircProjectItem
    {
        public string ForceId { get; set; }
        public string Id { get; set; }
    }

    [XmlRoot(ElementName = "Event")]
    public class Event : IHircProjectItem
    {
        [XmlAttribute(AttributeName = "Id")]
        public string Id { get; set; }

        [XmlAttribute(AttributeName = "ForceId")]
        public string ForceId { get; set; }

        
        [XmlElement(ElementName = "Action")]
        public string Action { get; set; }

 
        [XmlAttribute(AttributeName = "AudioBus")]
        public string AudioBus { get; set; }
    }



    [XmlRoot(ElementName = "Action")]
    public class Action : IHircProjectItem
    {
        [XmlElement(ElementName = "Child")]
        public List<ActionChild> ChildList { get; set; }

        [XmlAttribute(AttributeName = "Id")]
        public string Id { get; set; }

        [XmlAttribute(AttributeName = "ForceId")]
        public string ForceId { get; set; }
    }


    [XmlRoot(ElementName = "Child")]
    public class ActionChild
    {
        [XmlAttribute(AttributeName = "Type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "Id")]
        public string Id { get; set; }
    }


    [XmlRoot(ElementName = "GameSound")]
    public class GameSound : IHircProjectItem
    {
        [XmlAttribute(AttributeName = "Id")]
        public string Id { get; set; }

        [XmlAttribute(AttributeName = "ForceId")]
        public string ForceId { get; set; }

        [XmlText]
        public string Path { get; set; }



    }

    [XmlRoot(ElementName = "root")]
    public class AudioProjectXml
    {
        [XmlElement(ElementName = "Event")]
        public List<Event> Events { get; set; }

        [XmlElement(ElementName = "Action")]
        public List<Action> Actions { get; set; }

        [XmlElement(ElementName = "GameSound")]
        public List<GameSound> GameSounds { get; set; }


        [XmlAttribute(AttributeName = "OutputFile")]
        public string OutputFile { get; set; }
        [XmlAttribute(AttributeName = "Game")]
        public string OutputGame { get; set; } = "Warhammer3";
        [XmlAttribute(AttributeName = "Version")]
        public string Version { get; set; } = "1";
    }

}
