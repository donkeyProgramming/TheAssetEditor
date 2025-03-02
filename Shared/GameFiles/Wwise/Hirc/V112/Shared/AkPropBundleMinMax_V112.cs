using Shared.Core.ByteParsing;
using static Shared.GameFormats.Wwise.Enums.Enums_V112;

namespace Shared.GameFormats.Wwise.Hirc.V112.Shared
{
    public class AkPropBundleMinMax_V112
    {
        public byte Props { get; set; }
        public List<AkPropBundleInstance_V112> PropsList { get; set; } = [];

        public void ReadData(ByteChunk chunk)
        {
            Props = chunk.ReadByte();

            // First read the all the types.
            for (byte i = 0; i < Props; i++)
                PropsList.Add(new AkPropBundleInstance_V112() { Type = (AkPropId_V112)chunk.ReadByte() });

            // Then read the all min and max values.
            for (byte i = 0; i < Props; i++)
            {
                PropsList[i].Min = chunk.ReadSingle();
                PropsList[i].Max = chunk.ReadSingle();
            }
        }

        public byte[] ReadData()
        {
            using var memStream = new MemoryStream();
            memStream.Write(ByteParsers.Byte.EncodeValue((byte)PropsList.Count, out _));
            foreach (var value in PropsList)
                memStream.Write(ByteParsers.Byte.EncodeValue((byte)value.Type, out _));

            // Read all the Ids first 
            foreach (var value in PropsList)
            {
                memStream.Write(ByteParsers.Single.EncodeValue((byte)value.Min, out _));
                memStream.Write(ByteParsers.Single.EncodeValue((byte)value.Max, out _));
            }

            // Then read the all min and max values.
            var byteArray = memStream.ToArray();
            if (byteArray.Length != GetSize())
                throw new Exception("Invalid size");
            return byteArray;
        }

        public uint GetSize()
        {
            var propsSize = ByteHelper.GetPropertyTypeSize(Props);

            uint propsListSize = 0;
            foreach (var prop in PropsList)
                propsListSize += prop.GetSize();

            return propsSize + propsListSize;
        }

        public class AkPropBundleInstance_V112
        {
            public AkPropId_V112 Type { get; set; }
            public float Min { get; set; }
            public float Max { get; set; }

            public uint GetSize()
            {
                var typeSize = ByteHelper.GetPropertyTypeSize(Type);
                var minSize = ByteHelper.GetPropertyTypeSize(Min);
                var maxSize = ByteHelper.GetPropertyTypeSize(Max);
                return typeSize + minSize + maxSize;
            }
        }
    }
}
