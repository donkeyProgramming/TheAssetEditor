using Shared.Core.ByteParsing;
using Shared.GameFormats.Wwise.Enums;

namespace Shared.GameFormats.Wwise.Hirc.V136.Shared
{
    public class AkPropBundleMinMax_V136
    {
        public byte Props { get; set; }
        public List<AkPropBundleInstance_V136> PropsList { get; set; } = [];

        public void ReadData(ByteChunk chunk)
        {
            Props = chunk.ReadByte();

            // First read the all the types.
            for (byte i = 0; i < Props; i++)
                PropsList.Add(new AkPropBundleInstance_V136() { Type = (AkPropId_V136)chunk.ReadByte() });

            // Then read the all min and max values.
            for (byte i = 0; i < Props; i++)
            {
                PropsList[i].Min = chunk.ReadSingle();
                PropsList[i].Max = chunk.ReadSingle();
            }
        }

        public byte[] WriteData()
        {
            using var memStream = new MemoryStream();
            memStream.Write(ByteParsers.Byte.EncodeValue((byte)PropsList.Count, out _));

            // Read all the Ids first 
            foreach (var value in PropsList)
                memStream.Write(ByteParsers.Byte.EncodeValue((byte)value.Type, out _));

            // Then read the all min and max values.
            foreach (var value in PropsList)
            {
                memStream.Write(ByteParsers.Single.EncodeValue((byte)value.Min, out _));
                memStream.Write(ByteParsers.Single.EncodeValue((byte)value.Max, out _));
            }

            return memStream.ToArray();
        }

        public uint GetSize()
        {
            var propsSize = ByteHelper.GetPropertyTypeSize(Props);

            uint propsListSize = 0;
            foreach (var prop in PropsList)
                propsListSize += prop.GetSize();

            return propsSize + propsListSize;
        }

        public AkPropBundleMinMax_V136 Clone()
        {
            return new AkPropBundleMinMax_V136
            {
                Props = Props,
                PropsList = PropsList.Select(prop => prop.Clone()).ToList()
            };
        }

        public class AkPropBundleInstance_V136
        {
            public AkPropId_V136 Type { get; set; }
            public float Min { get; set; }
            public float Max { get; set; }

            public uint GetSize()
            {
                var typeSize = ByteHelper.GetPropertyTypeSize(Type);
                var minSize = ByteHelper.GetPropertyTypeSize(Min);
                var maxSize = ByteHelper.GetPropertyTypeSize(Max);
                return typeSize + minSize + maxSize;
            }

            public AkPropBundleInstance_V136 Clone()
            {
                return new AkPropBundleInstance_V136
                {
                    Type = Type,
                    Min = Min,
                    Max = Max
                };
            }
        }
    }
}
