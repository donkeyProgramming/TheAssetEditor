using System.ComponentModel.DataAnnotations;
using Shared.Core.ByteParsing;
using Shared.Core.PackFiles.Models;

namespace Utility.DatabaseSchemaGenerator.Examples
{
    public class battle_skeleton_parts  // Remove _tables
    {
        // --- Auto generate
        [Key] public string variant_name { get; set; }
        public string skeleton { get; set; }
        public string root_joint { get; set; }
        // --- 

        public static string TableName => "battle_skeleton_parts_table";    // 
        public static int TableVersion => 0;                                //

        static battle_skeleton_parts Create(ByteChunk byteChunk)
        {
            var instance = new battle_skeleton_parts();
            instance.variant_name = byteChunk.ReadString();
            instance.skeleton = byteChunk.ReadString();
            instance.root_joint = byteChunk.ReadOptionalString();
            return instance;
        }

        public static List<battle_skeleton_parts> Deserialize(PackFile dbFile)
        {
            var ouput = new List<battle_skeleton_parts>();
            var byteChunk = dbFile.DataSource.ReadDataAsChunk();
            var numItems = byteChunk.ReadUInt32();
            var version = byteChunk.ReadUInt32();

            if (version != TableVersion)
                throw new Exception($"Failed to deserialize {TableName}. Version mismatch. Expected {TableVersion}, Actual {version}");

            for (var i = 0; i < numItems; i++)
            {
                var instance = Create(byteChunk);
                ouput.Add(instance);
            }

            var bytesLeft = byteChunk.BytesLeft;
            if (bytesLeft != 0)
                throw new Exception($"Failed to deserialize {TableName}. {bytesLeft} bytes left after deserialization");

            return ouput;
        }
    }
}
