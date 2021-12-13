using Filetypes.ByteParsing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CommonControls.FileTypes.DB
{
    public class StringArrayTable
    {
        public List<string> Values { get; set; } = new List<string>();

        public StringArrayTable(params string[] items)
        {
            if (items != null)
            {
                foreach (var item in items)
                    Values.Add(item);
            }
        }

        public StringArrayTable() { }

        public StringArrayTable(ByteChunk data)
        {
            var count = data.ReadInt32();
            Values = new List<string>(count);
            for (int i = 0; i < count; i++)
                Values.Add(data.ReadString());
        }

        public byte[] ToByteArray()
        {
            using MemoryStream memStream = new MemoryStream();
            memStream.Write(ByteParsers.Int32.EncodeValue(Values.Count, out _));
            foreach (var item in Values)
                memStream.Write(ByteParsers.String.WriteCaString(item.ToLower()));

            return memStream.ToArray();
        }
    }
}
