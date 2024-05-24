namespace Shared.GameFormats.DB
{
    /* public class SimpleSchema
     {
         public List<SimpleSchemaObject> ObjectDefinitions { get; set; } = new List<SimpleSchemaObject>();

         public void Decode(ByteChunk byteChunk, string[] stringList)
         {
             foreach (var obj in ObjectDefinitions)
                 obj.Decode(byteChunk, stringList);
         }

         public SimpleSchemaObject GetObjectDefinition(string name, int version)
         {
             return ObjectDefinitions.FirstOrDefault(x => x.Name == name && x.Version == version);
         }
     }

     public class SimpleSchemaObject
     {
         public string Name { get; set; }
         public int Version { get; set; }
         public List<SimpleSchemaField> Fields { get; set; } = new List<SimpleSchemaField>();

         public static SimpleSchemaObject Create(string name, int version)
         {
             return new SimpleSchemaObject() { Name = name, Version = version };
         }

         public SimpleSchemaObject AddItem(string name, DbTypesEnum dbTypesEnum)
         {
             var newItem = new SimpleSchemaField(name, dbTypesEnum);
             Fields.Add(newItem);
             return this;
         }

         public SimpleSchemaObject Decode(ByteChunk byteChunk, string[] stringList)
         {
             var instance = Clone();
             foreach (var item in instance.Fields)
             {
                 var peak = byteChunk.PeakUnknown();

                 if (item.ValueType == DbTypesEnum.StringLookup)
                 {
                     var stringIndex = byteChunk.ReadInt32();
                     item.Value = $"[{stringIndex}] " + stringList[stringIndex];
                 }
                 else
                 {
                     var converter = ByteParserFactory.Create(item.ValueType);
                     byteChunk.Read(converter, out var value, out var error);

                     if (string.IsNullOrWhiteSpace(error) == false)
                         throw new Exception($"Unable to read Field: {item.Name} - Error: {error}");
                     item.Value = value;
                 }
             }

             return instance;
         }

         public SimpleSchemaObject Clone()
         {
             var clone = new SimpleSchemaObject() { Name = Name, Version = Version };
             foreach (var item in Fields)
                 clone.AddItem(item.Name, item.ValueType);
             return clone;
         }
     }

     [DebuggerDisplay("Field Name = {Name} Value = {Value}")]
     public class SimpleSchemaField
     {
         public SimpleSchemaField(string fieldName, DbTypesEnum valueType)
         {
             Name = fieldName;
             ValueType = valueType;
         }

         public SimpleSchemaField()
         { }

         public string Name { get; set; }
         public string Value { get; set; }
         public DbTypesEnum ValueType { get; set; }
     }*/
}
