using Filetypes.ByteParsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CommonControls.FileTypes.MetaData
{
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

        public static List<string> GetSupportedTypes()
        {
            EnsureMappingTableCreated();
            return _typeTable.Select(x => x.Key).ToList();
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
            EnsureMappingTableCreated();

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

        internal static MetaEntryBase CreateDefault(string itemName)
        {
            EnsureMappingTableCreated();

            if (_typeTable.ContainsKey(itemName) == false)
                throw new Exception("Unkown metadata item " + itemName);

            var instance = Activator.CreateInstance(_typeTable[itemName]) as MetaEntryBase;

            var itemNameSplit = itemName.ToUpper().Split("_");
            instance.Version = int.Parse(itemNameSplit.Last());
            return instance;
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


    //[AttributeUsage(AttributeTargets.Method, Inherited = false)]
    //public class YourAttribute : Attribute
    //{
    //    //...
    //}


}
