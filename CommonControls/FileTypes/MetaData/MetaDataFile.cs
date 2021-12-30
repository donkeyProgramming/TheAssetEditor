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
                let attributes = t.GetCustomAttributes(typeof(MetaDataAttribute), true)
                where attributes != null && attributes.Length > 0
                select new { Type = t, Attributes = attributes.Cast<MetaDataAttribute>() };

            foreach (var instance in typesWithMyAttribute)
            {
                var type = instance.Type;
                var key = instance.Attributes.First().VersionName;
                _typeTable.Add(key, type);

                var orderedPropertiesList = type.GetProperties()
                    .Where(x => x.CanWrite)
                    .Where(x => Attribute.IsDefined(x, typeof(MetaDataTagAttribute)))
                    .OrderBy(x => x.GetCustomAttributes<MetaDataTagAttribute>(false).Single().Order)
                    .Select(x => x.GetCustomAttributes<MetaDataTagAttribute>(false).Single());

                var allNumbers = orderedPropertiesList.Select(x => x.Order).ToArray();
                if (IsSequential(allNumbers) == false)
                    throw new Exception("Invalid ids");
            }
        }

        static bool IsSequential(int[] array)
        {
            return array.Zip(array.Skip(1), (a, b) => (a + 1) == b).All(x => x);
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
            var entryInfo = GetEntryInformation(entry);
            var instance = Activator.CreateInstance(entryInfo.type);
            var bytes = entry.GetData();
            int currentIndex = 0;
            foreach (var proptery in entryInfo.Properties)
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

        public static List<(string Header, string Value)> DeSerializeToStrings(IMetaEntry entry)
        {
            var entryInfo = GetEntryInformation(entry);
            var bytes = entry.GetData();
            int currentIndex = 0;
            var output = new List<(string, string)>();

            foreach (var proptery in entryInfo.Properties)
            {
                var parser = ByteParserFactory.Create(proptery.PropertyType);
                var result = parser.TryDecode(bytes, currentIndex, out var value, out var bytesRead, out var error);
                if (result == false)
                    throw new Exception($"Failed to serialize {proptery.Name} - {error}");
                currentIndex += bytesRead;

                output.Add((proptery.Name, value));
            }

            if (bytes.Length != currentIndex)
                throw new Exception("Failed to read object - bytes left");

            return output;
        }

        static (Type type, List<PropertyInfo> Properties) GetEntryInformation(IMetaEntry entry)
        {
            var metaDataType = GetTypeFromMeta(entry);
            if (metaDataType == null)
                throw new Exception($"Unable to find decoder for {entry.Name} _ {entry.Version}");

            var instance = Activator.CreateInstance(metaDataType);
            var orderedPropertiesList = metaDataType.GetProperties()
                .Where(x => x.CanWrite)
                .Where(x => Attribute.IsDefined(x, typeof(MetaDataTagAttribute)))
                .OrderBy(x => x.GetCustomAttributes<MetaDataTagAttribute>(false).Single().Order)
                .ToList();

            return (metaDataType, orderedPropertiesList);
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
