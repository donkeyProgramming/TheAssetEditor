using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CommonControls.FileTypes.MetaData
{
    public class MetaDataFile
    {
        public int Version { get; set; }
        public List<IMetaEntry> Items { get; set; } = new List<IMetaEntry>();

        public List<MetaEntry> GetItemsOfType(string type)
        {
            var result = Items
                .Where(x => x is MetaEntry)
                .Where(x => x.Name.Contains(type, StringComparison.InvariantCultureIgnoreCase))
                .Select(x => x as MetaEntry)
                .ToList();

            return result;
        }
    }

    public abstract class MetaEntryBase
    {

        [MetaDataTag(0, "Version number of the Tag type", MetaDataTagAttribute.DisplayType.None, true)]
        public int Version { get; set; }

        [MetaDataTag(1, "Time in second when the Tag takes effect")]
        public float StartTime { get; set; }

        [MetaDataTag(2, "Time in second when the Tag stops taking effect")]
        public float EndTime { get; set; }

        [MetaDataTag(3, "Filter to enable or disable, almost always empty")]
        public string Filter { get; set; } = "";

        [MetaDataTag(4, "Id to enable or disable, almost always 0")]
        public int Id { get; set; }
    }


    public class MetaDataAttribute : Attribute
    {
        public string VersionName { get; private set; }

        public MetaDataAttribute(string name, int version)
        {
            VersionName = name + "_" + version;
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


    //[AttributeUsage(AttributeTargets.Method, Inherited = false)]
    //public class YourAttribute : Attribute
    //{
    //    //...
    //}


}
