using Shared.Core.ByteParsing;
using Shared.GameFormats.Wwise.Enums;

namespace Shared.GameFormats.Wwise.Hirc.V136.Shared
{
    public class AkPropBundle_V136
    {
        public byte Props { get; set; }
        public List<PropBundleInstance_V136> PropsList { get; set; } = [];

        public void CreateSpecificData(ByteChunk chunk)
        {
            Props = chunk.ReadByte();

            // Read all the Ids first
            for (byte i = 0; i < Props; i++)
                PropsList.Add(new PropBundleInstance_V136() { Id = (AkPropId_V136)chunk.ReadByte() });

            // Then write all the values
            for (byte i = 0; i < Props; i++)
                PropsList[i].Value = chunk.ReadUInt32();
        }

        public byte[] GetAsByteArray()
        {
            using var memStream = new MemoryStream();
            memStream.Write(ByteParsers.Byte.EncodeValue((byte)PropsList.Count, out _));

            // Write all the Ids first
            foreach (var akProp in PropsList)
                memStream.Write(ByteParsers.Byte.EncodeValue((byte)akProp.Id, out _));

            // Then write all the values
            foreach (var akProp in PropsList)
                memStream.Write(ByteParsers.UInt32.EncodeValue(akProp.Value, out _));

            return memStream.ToArray();
        }

        public uint GetSize()
        {
            var propsSize = ByteHelper.GetPropertyTypeSize(Props);

            uint propsListSize = 0;
            foreach (var akProp in PropsList)
                propsListSize += akProp.GetSize();

            return propsSize + propsListSize;
        }

        public class PropBundleInstance_V136
        {
            public AkPropId_V136 Id { get; set; }
            public uint Value { get; set; }

            public uint GetSize()
            {
                var idSize = ByteHelper.GetPropertyTypeSize(Id);
                var valueSize = ByteHelper.GetPropertyTypeSize(Value);
                return idSize + valueSize;
            }
        }
    }
}
