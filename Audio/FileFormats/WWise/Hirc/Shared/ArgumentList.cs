using Filetypes.ByteParsing;
using System.Collections.Generic;
using System.IO;

namespace Audio.FileFormats.WWise.Hirc.Shared
{
    public class ArgumentList
    {
        public List<Argument> Arguments { get; set; } = new List<Argument>();
        public ArgumentList(ByteChunk chunk, uint numItems)
        {
            for (uint i = 0; i < numItems; i++)
                Arguments.Add(new Argument());

            for (int i = 0; i < numItems; i++)
                Arguments[i].ulGroupId = chunk.ReadUInt32();

            for (int i = 0; i < numItems; i++)
                Arguments[i].eGroupType = (AkGroupType)chunk.ReadByte();
        }

        public class Argument
        {
            public uint ulGroupId { get; set; }
            public AkGroupType eGroupType { get; set; }
        }

        public byte[] GetAsBytes()
        {
            using var memStream = new MemoryStream();
            Arguments.ForEach(e => memStream.Write(ByteParsers.UInt32.EncodeValue(e.ulGroupId, out _)));
            Arguments.ForEach(e => memStream.Write(ByteParsers.Byte.EncodeValue((byte)e.eGroupType, out _)));
            var byteArray = memStream.ToArray();
            return byteArray;
        }
    }
}
