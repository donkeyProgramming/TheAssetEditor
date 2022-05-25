using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace AnimationSlotAnal
{
    class Program
    {
        static void Main(string[] args)
        {
            var catagories = LoadXmlFile<Dataroot_Categories>(@"C:\Users\ole_k\Downloads\animation_categories.xml").Animation_categories;
            var slots = LoadXmlFile<Dataroot_Slots>(@"C:\Users\ole_k\Downloads\animation_slot_categories.xml").Animation_slot_categories;

            var orderedCatagories = catagories
                .OrderBy(x => x.Order)
                .ToDictionary(x => x.Name, x => x.Order);

            var orderedCatagoriesCount = orderedCatagories
                .Select(x => $"{x.Key}_{x.Value}")
                .ToList();

            var slotsOrderedByGroup = slots
                .OrderBy(x => orderedCatagories[x.Category])
                .GroupBy(x => x.Category);

            var slotsOrderedByGroupWithCount = slotsOrderedByGroup
                .Select(x => $"{x.Key}_{x.Count()}")
                .ToList();

            var slotsOrderedByGroupList = slotsOrderedByGroup.ToList();
        }

        static T LoadXmlFile<T>(string path) where T : class
        {
            var ser = new XmlSerializer(typeof(T));
            using var fileStream = new FileStream(path, FileMode.Open);
            var data = ser.Deserialize(fileStream) as T;
            return data;
        }
    }

    [XmlRoot(ElementName = "animation_categories")]
    public class Animation_categories
    {
        [XmlElement(ElementName = "name")]
        public string Name { get; set; }
        [XmlElement(ElementName = "order")]
        public int Order { get; set; }
        [XmlAttribute(AttributeName = "record_uuid")]
        public string Record_uuid { get; set; }
        [XmlAttribute(AttributeName = "record_timestamp")]
        public string Record_timestamp { get; set; }
        [XmlAttribute(AttributeName = "record_key")]
        public string Record_key { get; set; }
    }

    [XmlRoot(ElementName = "dataroot")]
    public class Dataroot_Categories
    {
        [XmlElement(ElementName = "edit_uuid")]
        public string Edit_uuid { get; set; }
        [XmlElement(ElementName = "animation_categories")]
        public List<Animation_categories> Animation_categories { get; set; }
        [XmlAttribute(AttributeName = "od", Namespace = "http://www.w3.org/2000/xmlns/")]
        public string Od { get; set; }
        [XmlAttribute(AttributeName = "xsi", Namespace = "http://www.w3.org/2000/xmlns/")]
        public string Xsi { get; set; }
        [XmlAttribute(AttributeName = "noNamespaceSchemaLocation", Namespace = "http://www.w3.org/2001/XMLSchema-instance")]
        public string NoNamespaceSchemaLocation { get; set; }
        [XmlAttribute(AttributeName = "export_time")]
        public string Export_time { get; set; }
        [XmlAttribute(AttributeName = "revision")]
        public string Revision { get; set; }
        [XmlAttribute(AttributeName = "export_branch")]
        public string Export_branch { get; set; }
        [XmlAttribute(AttributeName = "export_user")]
        public string Export_user { get; set; }
    }

    [XmlRoot(ElementName = "animation_slot_categories")]
    public class Animation_slot_categories
    {
        [XmlElement(ElementName = "category")]
        public string Category { get; set; }
        [XmlElement(ElementName = "slot")]
        public string Slot { get; set; }
        [XmlAttribute(AttributeName = "record_uuid")]
        public string Record_uuid { get; set; }
        [XmlAttribute(AttributeName = "record_timestamp")]
        public string Record_timestamp { get; set; }
        [XmlAttribute(AttributeName = "record_key")]
        public string Record_key { get; set; }
    }

    [XmlRoot(ElementName = "dataroot")]
    public class Dataroot_Slots
    {
        [XmlElement(ElementName = "edit_uuid")]
        public string Edit_uuid { get; set; }
        [XmlElement(ElementName = "animation_slot_categories")]
        public List<Animation_slot_categories> Animation_slot_categories { get; set; }
        [XmlAttribute(AttributeName = "od", Namespace = "http://www.w3.org/2000/xmlns/")]
        public string Od { get; set; }
        [XmlAttribute(AttributeName = "xsi", Namespace = "http://www.w3.org/2000/xmlns/")]
        public string Xsi { get; set; }
        [XmlAttribute(AttributeName = "noNamespaceSchemaLocation", Namespace = "http://www.w3.org/2001/XMLSchema-instance")]
        public string NoNamespaceSchemaLocation { get; set; }
        [XmlAttribute(AttributeName = "export_time")]
        public string Export_time { get; set; }
        [XmlAttribute(AttributeName = "revision")]
        public string Revision { get; set; }
        [XmlAttribute(AttributeName = "export_branch")]
        public string Export_branch { get; set; }
        [XmlAttribute(AttributeName = "export_user")]
        public string Export_user { get; set; }
    }
}
