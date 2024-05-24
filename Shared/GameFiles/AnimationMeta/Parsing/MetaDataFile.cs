using Newtonsoft.Json;

namespace Shared.GameFormats.AnimationMeta.Parsing
{
    public class MetaDataFile
    {
        public int Version { get; set; }
        public List<BaseMetaEntry> Items { get; set; } = new List<BaseMetaEntry>();

        public List<T> GetItemsOfType<T>()
        {
            return Items.Where(x => x is T).Cast<T>().ToList();
        }
    }


    public abstract class BaseMetaEntry
    {
        public string Name { get; set; }

        [JsonIgnore]
        public byte[] Data { get; set; }
        public virtual string Description { get; } = "";

        [MetaDataTag(0, "Version number of the Tag type", MetaDataTagAttribute.DisplayType.None, true)]
        public int Version { get; set; }

        public string DisplayName { get => $"{Name}_{Version}{Description}"; }

    }

    public class UnknownMetaEntry : BaseMetaEntry
    {

    }
    public abstract class DecodedMetaEntryBase_v0 : BaseMetaEntry
    {
        [MetaDataTag(1)]
        public int UnknownIntBase_v0 { get; set; }
    }

    public abstract class DecodedMetaEntryBase_v1 : DecodedMetaEntryBase_v0
    {
        [MetaDataTag(2)]
        public int UnknownIntBase_v1 { get; set; }
    }

    public abstract class DecodedMetaEntryBase_v2 : BaseMetaEntry
    {
        [MetaDataTag(1, "Time in second when the Tag takes effect")]
        public float StartTime { get; set; }

        [MetaDataTag(2, "Time in second when the Tag stops taking effect")]
        public float EndTime { get; set; }

        [MetaDataTag(3, "Filter to enable or disable, almost always empty")]
        public string Filter { get; set; } = "";
    }

    public abstract class DecodedMetaEntryBase : BaseMetaEntry
    {
        [MetaDataTag(1, "Time in second when the Tag takes effect")]
        public float StartTime { get; set; }

        [MetaDataTag(2, "Time in second when the Tag stops taking effect")]
        public float EndTime { get; set; }

        [MetaDataTag(3, "Filter to enable or disable, almost always empty")]
        public string Filter { get; set; } = "";

        [MetaDataTag(4, "Id to enable or disable, almost always 0")]
        public int Id { get; set; }
    }


    public enum MetaDataAttributePriority
    {
        High = 0,
        Low = 10,
    }

    public class MetaDataAttribute : Attribute
    {
        public string VersionName { get; private set; }
        public string Name { get; set; }
        public int Version { get; set; }
        public MetaDataAttributePriority Priority { get; set; } = MetaDataAttributePriority.High;

        public MetaDataAttribute(string name, int version, MetaDataAttributePriority priority = MetaDataAttributePriority.High)
        {
            Name = name;
            Version = version;
            VersionName = name + "_" + version;
            Priority = priority;
        }
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class MetaDataTagAttribute : Attribute
    {
        public int Order { get; private set; }
        public string Description { get; private set; }
        public DisplayType DisplayOverride { get; private set; }
        public bool IsEditable { get; set; } = true;

        public enum DisplayType
        {
            EulerVector,
            None
        }

        public MetaDataTagAttribute(int order, string description = "", DisplayType displayOverride = DisplayType.None, bool isEditable = true)
        {
            Order = order;
            Description = description;
            DisplayOverride = displayOverride;
            IsEditable = isEditable;
        }
    }
}
