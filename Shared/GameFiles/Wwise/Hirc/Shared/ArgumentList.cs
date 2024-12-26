using Shared.Core.ByteParsing;

namespace Shared.GameFormats.Wwise.Hirc.Shared
{
    public class ArgumentList
    {
        public List<Argument> Arguments { get; set; } = [];
        public ArgumentList(ByteChunk chunk, uint numItems)
        {
            for (uint i = 0; i < numItems; i++)
                Arguments.Add(new Argument());

            for (var i = 0; i < numItems; i++)
                Arguments[i].UlGroupId = chunk.ReadUInt32();

            for (var i = 0; i < numItems; i++)
                Arguments[i].EGroupType = (AkGroupType)chunk.ReadByte();
        }

        public class Argument
        {
            public uint UlGroupId { get; set; }
            public AkGroupType EGroupType { get; set; }
        }

        public byte[] GetAsBytes()
        {
            using var memStream = new MemoryStream();
            Arguments.ForEach(e => memStream.Write(ByteParsers.UInt32.EncodeValue(e.UlGroupId, out _)));
            Arguments.ForEach(e => memStream.Write(ByteParsers.Byte.EncodeValue((byte)e.EGroupType, out _)));
            var byteArray = memStream.ToArray();
            return byteArray;
        }

        public static byte[] GetCustomArgumentsAsBytes(List<Argument> arguments)
        {
            using var memStream = new MemoryStream();
            arguments.ForEach(e => memStream.Write(ByteParsers.UInt32.EncodeValue(e.UlGroupId, out _)));
            arguments.ForEach(e => memStream.Write(ByteParsers.Byte.EncodeValue((byte)e.EGroupType, out _)));
            var byteArray = memStream.ToArray();
            return byteArray;
        }
    }
}
