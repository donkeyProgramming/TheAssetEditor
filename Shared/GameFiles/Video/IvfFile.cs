using System.Text;
using Shared.ByteParsing;

namespace Shared.GameFormats.Video
{
    public class IvfFile(CAVp8File caVp8File)
    {
        private const string Signature = "DKIF";
        private const ushort HeaderLength = 32;

        public ushort Version { get; set; } = caVp8File.Version;
        public string CodecFourCC { get; set; } = caVp8File.CodecFourCC;
        public ushort Width { get; set; } = caVp8File.Width;
        public ushort Height { get; set; } = caVp8File.Height;
        public uint NumberOfFrames { get; set; } = caVp8File.NumberOfFrames;
        public float Framerate { get; set; } = caVp8File.Framerate;
        public List<FrameTableRecord> FrameTable { get; set; } = caVp8File.FrameTable;
        public byte[] FrameData { get; set; } = caVp8File.FrameData;

        public byte[] WriteData()
        {
            using var memStream = new MemoryStream();
            using var writer = new BinaryWriter(memStream);

            writer.Write(Encoding.ASCII.GetBytes(Signature));
            writer.Write(Version);
            writer.Write(HeaderLength);
            writer.Write(Encoding.ASCII.GetBytes(CodecFourCC));
            writer.Write(Width);
            writer.Write(Height);

            var (framerateNumerator, framerateDenominator) = ConvertFramerateToRational(Framerate);
            writer.Write(framerateNumerator);
            writer.Write(framerateDenominator);

            writer.Write(NumberOfFrames);
            writer.Write((uint)0);

            var offset = 0;
            for (var frameIndex = 0; frameIndex < FrameTable.Count; frameIndex++)
            {
                var frame = FrameTable[frameIndex];

                var frameEndMinusOne = offset + (int)frame.Size - 1;
                if (frameEndMinusOne < 0)
                    throw new Exception($"IVF frame {frameIndex} has a size of zero at offset zero, which causes integer underflow when computing the last byte index.");

                if (offset < FrameData.Length && frameEndMinusOne < FrameData.Length)
                {
                    var frameData = FrameData[offset..(offset + (int)frame.Size)];
                    writer.Write((uint)frameData.Length);
                    writer.Write((ulong)frameIndex);
                    writer.Write(frameData);
                    offset += (int)frame.Size;
                }
            }

            return memStream.ToArray();
        }

        private static (uint numerator, uint denominator) ConvertFramerateToRational(float framerate)
        {
            if (framerate <= 0f || float.IsNaN(framerate) || float.IsInfinity(framerate))
                throw new Exception($"Cannot convert a framerate of {framerate} to a rational number.");

            var bits = BitConverter.SingleToUInt32Bits(framerate);
            var biasedExponent = (int)BitHelper.ExtractBits(bits, 23, 8);
            var mantissaBits = BitHelper.ExtractBits(bits, 0, 23);

            ulong significand = (1UL << 23) | mantissaBits;
            var exponentShift = biasedExponent - 150;

            ulong numerator;
            ulong denominator;

            if (exponentShift >= 0)
            {
                numerator = significand << exponentShift;
                denominator = 1;
            }
            else
            {
                numerator = significand;
                denominator = 1UL << (-exponentShift);
            }

            var divisor = GreatestCommonDivisor(numerator, denominator);
            return ((uint)(numerator / divisor), (uint)(denominator / divisor));
        }

        private static ulong GreatestCommonDivisor(ulong a, ulong b)
        {
            while (b != 0)
            {
                var remainder = a % b;
                a = b;
                b = remainder;
            }
            return a;
        }
    }
}
