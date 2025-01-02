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

        public static string TableName => "battle_skeleton_parts_tables";    // 
        public static int TableVersion => 0;                                //

        static battle_skeleton_parts Create(ByteChunk byteChunk)
        {
            var instance = new battle_skeleton_parts();
            instance.variant_name = byteChunk.ReadString();
            instance.skeleton = byteChunk.ReadString();
            instance.root_joint = byteChunk.ReadOptionalString();
            return instance;
        }


        /// If this sequence is found, the DB Table has a GUID after it.
        //const GUID_MARKER: &[u8] = &[253, 254, 252, 255];

        /// If this sequence is found, the DB Table has a version number after it.
        //const VERSION_MARKER: &[u8] = &[252, 253, 254, 255];

        //! # DB Structure
        //!
        //! ## Header
        //!
        //! | Bytes  | Type            | Data                                                         |
        //! | ------ | --------------- | ------------------------------------------------------------ |
        //! | 4      | &\[[u8]\]       | GUID Marker. Optional.                                       |
        //! | 2 + 72 | Sized StringU16 | GUID. Only present if GUID Marker is present too.            |
        //! | 4      | &\[[u8]\]       | Version Marker. Optional.                                    |
        //! | 4      | [u32]           | Version of the table. Only present if Version Marker is too. |
        //! | 1      | [bool]          | Unknown. Probably a bool because it's always either 0 or 1.  |
        //! | 4      | [u32]           | Amount of entries on the table.                              |

        public static List<battle_skeleton_parts> Deserialize(PackFile dbFile)
        {


            //https://github.com/Frodo45127/rpfm/blob/master/rpfm_lib/src/files/db/mod.rs#L213
            // Version u8
            // unkonw 1 (bool)
            // entries = u32

            var ouput = new List<battle_skeleton_parts>();
            var byteChunk = dbFile.DataSource.ReadDataAsChunk();


            //var marker0 = byteChunk.ReadByte();
            //var marker1 = byteChunk.ReadByte();
            //var marker2 = byteChunk.ReadByte();
            //var marker3 = byteChunk.ReadByte();

            var marker = byteChunk.ReadUInt32();
            if ((uint)4294770429 == marker)
            {
                var guid = byteChunk.ReadStringAscii();
            }

            var versionMarker = byteChunk.ReadUInt32(); // (uint)4294901244
            var version = byteChunk.ReadUInt32();
            var unkownBool = byteChunk.ReadBool();
            var numItems = byteChunk.ReadUInt32();

            //var version = byteChunk.ReadUShort();
            //var u = byteChunk.ReadBool();
            //var numItems = byteChunk.ReadUInt32();
            

            //if (version != TableVersion)
            //    throw new Exception($"Failed to deserialize {TableName}. Version mismatch. Expected {TableVersion}, Actual {version}");

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
