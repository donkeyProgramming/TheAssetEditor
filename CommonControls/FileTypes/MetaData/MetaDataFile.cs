using Filetypes.ByteParsing;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace CommonControls.FileTypes.MetaData
{
    public class MetaDataFile
    {
        public int Version { get; set; }
        public List<IMetaEntry> Items { get; set; } = new List<IMetaEntry>();

        public List<MetaEntry> GetItemsOfType(string type, bool onlyCorrectlyDecoded = true)
        {
            var result = Items
                .Where(x => x is MetaEntry)
                .Where(x => x.Name.Contains(type, StringComparison.InvariantCultureIgnoreCase))
                .Select(x => x as MetaEntry)
                .ToList();

            if (onlyCorrectlyDecoded)
            {
                result = result
                    .Where(x => x.DecodedCorrectly == true)
                    .ToList();
            }

            return result;
        }

        public List<IMetaEntry> GetUnkItemsOfType(string type, bool onlyCorrectlyDecoded = true)
        {
            var result = Items
                .Where(x => x.Name.Contains(type, StringComparison.InvariantCultureIgnoreCase))
                .ToList();

            if (onlyCorrectlyDecoded)
            {
                result = result
                    .Where(x => x.DecodedCorrectly == true)
                    .ToList();
            }

            return result;
        }
    }


    public class MetaEntrySerializer
    {
        public static Dictionary<string, Type> _typeTable;

        static void EnsureMappingTableCreated()
        {
            if (_typeTable != null)
                return;

            _typeTable = new Dictionary<string, Type>();

            var typesWithMyAttribute =
                from a in AppDomain.CurrentDomain.GetAssemblies()
                from t in a.GetTypes()
                let attributes = t.GetCustomAttributes(typeof(MetaEntryAttribute), true)
                where attributes != null && attributes.Length > 0
                select new { Type = t, Attributes = attributes.Cast<MetaEntryAttribute>() };

            foreach (var instance in typesWithMyAttribute)
            {
                var type = instance.Type;
                var key = instance.Attributes.First().VersionName;
                _typeTable.Add(key, type);
            }
        }

        static Type GetTypeFromMeta(IMetaEntry entry)
        {
            EnsureMappingTableCreated();

            var key = entry.Name + "_" + entry.Version;
            if (_typeTable.ContainsKey(key) == false)
                return null;

            return _typeTable[key];
        }

        public static MetaEntryBase DeSerialize(IMetaEntry entry)
        {
            var metaDataType = GetTypeFromMeta(entry);
            if (metaDataType == null)
                return null;

            var instance = Activator.CreateInstance(metaDataType);
            var orderedPropertiesList = metaDataType.GetProperties()
                .Where(x => x.CanWrite)
                .Where(x => Attribute.IsDefined(x, typeof(MetaDataTagAttribute)))
                .OrderBy(x => x.GetCustomAttributes< MetaDataTagAttribute>(false).Single().Order);

            var bytes = entry.GetData();
            int currentIndex = 0;
            foreach (var proptery in orderedPropertiesList)
            {
                var parser = ByteParserFactory.Create(proptery.PropertyType);
                var value = parser.GetValueAsObject(bytes, currentIndex, out var bytesRead);
                currentIndex += bytesRead;
                proptery.SetValue(instance, value);
            }

            if (bytes.Length != currentIndex)
                throw new Exception("Failed to read object - bytes left");

            return instance as MetaEntryBase;
        }
    }

    public abstract class MetaEntryBase
    {

        [MetaDataTag(0)]
        public int Version { get; set; }

        [MetaDataTag(1, "Time in second when the entry takes effect")]
        public float StartTime { get; set; }

        [MetaDataTag(2, "Time in second when the entry stops taking effect")]
        public float EndTime { get; set; }

        [MetaDataTag(3, "Filter to enable or disbale, almost always empty")]
        public string Filter { get; set; }

        [MetaDataTag(4, "Almost always 0")]
        public int Id { get; set; }
    }


    


    public class MetaEntryAttribute : Attribute
    {
        public string VersionName { get; private set; }

        public MetaEntryAttribute(string name, int version)
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

        public enum DisplayType
        {
            EulerVector,
            None
        }

        public MetaDataTagAttribute(int order, string description = "", DisplayType displayOverride = DisplayType.None)
        {
            Order = order;
            Description = description;
            DisplayOverride = displayOverride;
        }
    }


    //[AttributeUsage(AttributeTargets.Method, Inherited = false)]
    //public class YourAttribute : Attribute
    //{
    //    //...
    //}


}
