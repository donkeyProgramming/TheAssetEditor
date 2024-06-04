using Shared.Core.ByteParsing;
using Shared.GameFormats.WWise;
using System.Collections.Generic;
using System.IO;

namespace Shared.GameFormats.WWise.Hirc.Shared
{
    public class ArgumentList
    {
        public List<Argument> Arguments { get; set; } = new List<Argument>();
        public ArgumentList(ByteChunk chunk, uint numItems)
        {
            for (uint i = 0; i < numItems; i++)
                Arguments.Add(new Argument());

            for (var i = 0; i < numItems; i++)
                Arguments[i].ulGroupId = chunk.ReadUInt32();

            for (var i = 0; i < numItems; i++)
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

        public static byte[] GetCustomArgumentsAsBytes(List<Argument> arguments)
        {
            using var memStream = new MemoryStream();
            arguments.ForEach(e => memStream.Write(ByteParsers.UInt32.EncodeValue(e.ulGroupId, out _)));
            arguments.ForEach(e => memStream.Write(ByteParsers.Byte.EncodeValue((byte)e.eGroupType, out _)));
            var byteArray = memStream.ToArray();
            return byteArray;
        }
    }
}
