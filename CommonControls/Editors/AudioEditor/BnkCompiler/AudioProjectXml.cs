using System.Xml.Serialization;
using System.Collections.Generic;
namespace CommonControls.Editors.AudioEditor.BnkCompiler
{
    [XmlRoot(ElementName = "Event")]
    public class Event
    {
        [XmlElement(ElementName = "AudioBus")]
        public string AudioBus { get; set; }
        [XmlElement(ElementName = "Action")]
        public string Action { get; set; }
        [XmlAttribute(AttributeName = "Id")]
        public string Id { get; set; }
    }

    [XmlRoot(ElementName = "Child")]
    public class ActionChild
    {
        [XmlAttribute(AttributeName = "Type")]
        public string Type { get; set; }
        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "Action")]
    public class Action
    {
        [XmlElement(ElementName = "Child")]
        public List<ActionChild> ChildList { get; set; }
        [XmlAttribute(AttributeName = "Id")]
        public string Id { get; set; }
    }

    [XmlRoot(ElementName = "SwitchCase")]
    public class SwitchCase
    {
        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "SwitchContainer")]
    public class SwitchContainer
    {
        [XmlElement(ElementName = "SwitchEnum")]
        public string SwitchEnum { get; set; }
        [XmlElement(ElementName = "DefaultValue")]
        public string DefaultValue { get; set; }
        [XmlElement(ElementName = "SwitchCase")]
        public List<SwitchCase> SwitchCase { get; set; }
        [XmlAttribute(AttributeName = "Id")]
        public string Id { get; set; }
    }

    [XmlRoot(ElementName = "Child")]
    public class RandContainerChild
    {
        [XmlAttribute(AttributeName = "Weight")]
        public string Weight { get; set; }
        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "RandContainer")]
    public class RandContainer
    {
        [XmlElement(ElementName = "Child")]
        public List<RandContainerChild> Child { get; set; }
        [XmlAttribute(AttributeName = "Id")]
        public string Id { get; set; }
    }

    [XmlRoot(ElementName = "CustomSound")]
    public class CustomSound
    {
        [XmlAttribute(AttributeName = "Id")]
        public string Id { get; set; }
        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "GameSound")]
    public class GameSound
    {
        [XmlAttribute(AttributeName = "Id")]
        public string Id { get; set; }
        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "FileSound")]
    public class FileSound
    {
        [XmlAttribute(AttributeName = "Id")]
        public string Id { get; set; }
        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "root")]
    public class AudioProjectXml
    {
        [XmlElement(ElementName = "Event")]
        public List<Event> Events { get; set; }

        [XmlElement(ElementName = "Action")]
        public List<Action> Actions { get; set; }

        [XmlElement(ElementName = "SwitchContainer")]
        public List<SwitchContainer> SwitchContainers { get; set; }

        [XmlElement(ElementName = "RandContainer")]
        public List<RandContainer> RandContainers { get; set; }

        [XmlElement(ElementName = "CustomSound")]
        public List<CustomSound> CustomSounds { get; set; }

        [XmlElement(ElementName = "GameSound")]
        public List<GameSound> GameSounds { get; set; }

        [XmlElement(ElementName = "FileSound")]
        public List<FileSound> FileSounds { get; set; }

        [XmlAttribute(AttributeName = "OutputFile")]
        public string OutputFile { get; set; }
        [XmlAttribute(AttributeName = "Game")]
        public string OutputGame { get; set; } = "wh3";
    }

}
