using Shared.ByteParsing;
using Shared.GameFormats.Audio.Codecs;
using Shared.GameFormats.Wwise.Wem.V132;
using Shared.GameFormats.Wwise.Wem.V132.Encoding;

namespace Shared.GameFormats.Wwise.Wem.V132.Decoding
{
    public class WemVorbisDecoder(WwiseCodebookLibrary codebookLibrary)
    {
        private readonly WwiseCodebookLibrary _codebookLibrary = codebookLibrary;

        private const int XiphCodecPrivatePrefixLength = 16;
        private const int XiphLacingContinuationValue = 255;
        private const int VorbisModeTypeBitWidth = 16;

        public VorbisAudio Decode(WemFile wemFile)
        {
            var decodeResult = BuildDecodeResult(wemFile);
            return BuildVorbis(wemFile, decodeResult);
        }

        private VorbisDecodeResult BuildDecodeResult(WemFile wemFile)
        {
            var decodeResult = new VorbisDecodeResult
            {
                CommentPacket = new VorbisCommentHeader().WriteData(),
                IdentificationPacket = new VorbisIdentificationHeader(wemFile).WriteData(),
                LargeBlockSize = 1 << wemFile.FmtChunk.LargeBlockSizeExponent,
                SmallBlockSize = 1 << wemFile.FmtChunk.SmallBlockSizeExponent,
                UsesWwisePacketHeaderVariant = wemFile.FmtChunk.SmallBlockSizeExponent != wemFile.FmtChunk.LargeBlockSizeExponent,
            };
            ExpandSetupPacket(wemFile.DataChunk.SetupPacket, wemFile.FmtChunk.Channels, decodeResult);
            return decodeResult;
        }

        private static VorbisAudio BuildVorbis(WemFile wemFile, VorbisDecodeResult decodeResult)
        {
            var wemPackets = wemFile.DataChunk.AudioPackets;
            var rebuiltPackets = new List<byte[]>(wemPackets.Count);
            var perPacketBlockSizes = new List<bool>(wemPackets.Count);
            var previousPacketIsLargeBlock = false;

            for (var i = 0; i < wemPackets.Count; i++)
            {
                var payload = wemPackets[i].Data;
                if (payload.Length == 0)
                    throw new InvalidDataException("Wwise Vorbis packet size cannot be zero.");

                byte? nextPacketFirstByte = null;
                if (i + 1 < wemPackets.Count && wemPackets[i + 1].Data.Length > 0)
                    nextPacketFirstByte = wemPackets[i + 1].Data[0];

                var currentPacketIsLargeBlock = PacketUsesLargeBlock(payload[0], decodeResult);
                var rebuiltPacket = RebuildAudioPacket(payload, nextPacketFirstByte, decodeResult, previousPacketIsLargeBlock, currentPacketIsLargeBlock);

                rebuiltPackets.Add(rebuiltPacket);
                perPacketBlockSizes.Add(currentPacketIsLargeBlock);
                previousPacketIsLargeBlock = currentPacketIsLargeBlock;
            }

            var packetTimestamps = ComputePacketTimestampsMilliseconds(perPacketBlockSizes, decodeResult, wemFile.FmtChunk.SampleCount, wemFile.FmtChunk.SampleRate);
            var vorbisPackets = new List<VorbisAudioPacket>();
            for (var i = 0; i < rebuiltPackets.Count; i++)
                vorbisPackets.Add(new VorbisAudioPacket { Data = rebuiltPackets[i], TimestampMilliseconds = packetTimestamps[i] });

            return new VorbisAudio
            {
                Channels = checked((byte)wemFile.FmtChunk.Channels),
                VorbisCodecPrivateData = BuildXiphLacedCodecPrivate(decodeResult.IdentificationPacket, decodeResult.CommentPacket, decodeResult.SetupPacket),
                Packets = vorbisPackets,
                SampleCount = wemFile.FmtChunk.SampleCount,
                SampleRate = wemFile.FmtChunk.SampleRate,
            };
        }

        private void ExpandSetupPacket(byte[] compactSetupBytes, int channels, VorbisDecodeResult decodeResult)
        {
            var largeBufferCapacity = 4096;
            uint setupPacketType = 0x05;
            var vorbisHeaderTag = "vorbis";
            uint vorbisFramingBit = 1;

            var input = new BitChunk(compactSetupBytes);
            var output = new BitWriter(largeBufferCapacity);

            output.WriteByte((byte)setupPacketType);
            output.WriteAscii(vorbisHeaderTag);

            var codebookCount = TranscribeCodebooks(input, output);

            WriteTimeDomainTransformPlaceholders(output);

            var floorCount = WwiseVorbisBitstreamTranscriber.TranscribeFloorConfigurationsWwiseToVorbis(input, output, codebookCount);
            var residueCount = WwiseVorbisBitstreamTranscriber.TranscribeResidueConfigurationsWwiseToVorbis(input, output, codebookCount);
            var mappingCount = WwiseVorbisBitstreamTranscriber.TranscribeMappingConfigurationsWwiseToVorbis(input, output, channels, floorCount, residueCount);

            decodeResult.ModeConfig = TranscribeModeConfigurations(input, output, mappingCount);

            output.WriteBits(vorbisFramingBit, 1);

            decodeResult.SetupPacket = output.ToArray();
        }

        private static byte[] RebuildAudioPacket(byte[] payload, byte? nextFirstByte, VorbisDecodeResult decodeResult, bool previousPacketIsLargeBlock, bool currentPacketIsLargeBlock)
        {
            uint audioPacketTypeValue = 0;
            if (!decodeResult.UsesWwisePacketHeaderVariant)
                return payload;

            var input = new BitChunk(payload);
            var output = new BitWriter(Math.Max(payload.Length + 2, XiphCodecPrivatePrefixLength));

            output.WriteBits(audioPacketTypeValue, 1);

            var modeNumber = decodeResult.ModeConfig.ModeBits > 0 ? (int)input.ReadBits(decodeResult.ModeConfig.ModeBits) : 0;
            output.WriteBits((uint)modeNumber, decodeResult.ModeConfig.ModeBits);

            var remainderBitCount = BitHelper.BitsPerByte - decodeResult.ModeConfig.ModeBits;
            uint remainderBits = 0;
            if (remainderBitCount > 0)
                remainderBits = input.ReadBits(remainderBitCount);

            if (currentPacketIsLargeBlock)
            {
                uint previousBlockBits = 0;
                if (previousPacketIsLargeBlock)
                    previousBlockBits = 1;
                output.WriteBits(previousBlockBits, 1);
                
                uint nextBlockBits = 0;
                var nextPacketIsLargeBlock = nextFirstByte.HasValue && PacketUsesLargeBlock(nextFirstByte.Value, decodeResult);
                if (nextPacketIsLargeBlock)
                    nextBlockBits = 1;
                output.WriteBits(nextBlockBits, 1);
            }

            if (remainderBitCount > 0)
                output.WriteBits(remainderBits, remainderBitCount);

            for (var byteIndex = 1; byteIndex < payload.Length; byteIndex++)
                output.WriteByte(payload[byteIndex]);

            output.AlignToByte();
            return output.ToArray();
        }

        private static bool PacketUsesLargeBlock(byte firstByte, VorbisDecodeResult decodeResult)
        {
            if (decodeResult.ModeConfig.ModeBits == 0)
                return decodeResult.ModeConfig.ModeBlockSizes[0];
            else
            {
                var modeMask = (1 << decodeResult.ModeConfig.ModeBits) - 1;
                var modeNumber = firstByte & modeMask;
                return decodeResult.ModeConfig.ModeBlockSizes[modeNumber];
            }
        }

        private static long[] ComputePacketTimestampsMilliseconds(List<bool> blockSizesPerPacket, VorbisDecodeResult decodeResult, int sampleCount, uint sampleRate)
        {
            var granuleOverlapDivisor = 4;
            var timestampMillisecondsScale = 1000;
            var timestamps = new long[blockSizesPerPacket.Count];
            if (blockSizesPerPacket.Count == 0)
                return timestamps;

            long accumulatedSamples = 0;
            int? previousBlockSize = null;
            for (var index = 0; index < blockSizesPerPacket.Count; index++)
            {
                timestamps[index] = accumulatedSamples;
                var currentBlockSize = blockSizesPerPacket[index] ? decodeResult.LargeBlockSize : decodeResult.SmallBlockSize;
                if (previousBlockSize.HasValue)
                    accumulatedSamples += (previousBlockSize.Value + currentBlockSize) / granuleOverlapDivisor;
                previousBlockSize = currentBlockSize;
            }

            var scale = 1.0;
            if (accumulatedSamples > 0 && sampleCount > 0)
                scale = (double)sampleCount / accumulatedSamples;

            for (var index = 0; index < timestamps.Length; index++)
                timestamps[index] = (long)Math.Round(timestamps[index] * scale * timestampMillisecondsScale / sampleRate);

            return timestamps;
        }

        private static byte[] BuildXiphLacedCodecPrivate(byte[] identificationPacket, byte[] commentPacket, byte[] setupPacket)
        {
            byte xiphLacedHeaderPacketCountMinusOne = 0x02;
            var bytes = new List<byte>(identificationPacket.Length + commentPacket.Length + setupPacket.Length + XiphCodecPrivatePrefixLength) { xiphLacedHeaderPacketCountMinusOne };
            WriteXiphLacedSize(bytes, identificationPacket.Length);
            WriteXiphLacedSize(bytes, commentPacket.Length);
            bytes.AddRange(identificationPacket);
            bytes.AddRange(commentPacket);
            bytes.AddRange(setupPacket);
            return bytes.ToArray();
        }

        private static void WriteXiphLacedSize(List<byte> output, int size)
        {
            while (size >= XiphLacingContinuationValue)
            {
                output.Add(XiphLacingContinuationValue);
                size -= XiphLacingContinuationValue;
            }
            output.Add((byte)size);
        }

        private int TranscribeCodebooks(BitChunk input, BitWriter output)
        {
            var codebookCountField = input.ReadBits(8);
            output.WriteBits(codebookCountField, 8);

            var codebookCount = (int)codebookCountField + 1;
            for (var codebookIndex = 0; codebookIndex < codebookCount; codebookIndex++)
            {
                var codebookId = (int)input.ReadBits(10);
                var codebook = _codebookLibrary.GetCodebook(codebookId);
                
                var codebookInput = new BitChunk(codebook.Data);
                for (var bitIndex = 0; bitIndex < codebook.BitCount; bitIndex += 32)
                {
                    var bitsToWrite = Math.Min(32, codebook.BitCount - bitIndex);
                    var value = codebookInput.ReadBits(bitsToWrite);
                    output.WriteBits(value, bitsToWrite);
                }
            }

            return codebookCount;
        }

        private static void WriteTimeDomainTransformPlaceholders(BitWriter output)
        {
            uint vorbisTimeDomainTransformCountField = 0;
            var vorbisTimeDomainTransformCountBitWidth = 6;
            uint vorbisTimeDomainTransformType = 0;

            output.WriteBits(vorbisTimeDomainTransformCountField, vorbisTimeDomainTransformCountBitWidth);
            output.WriteBits(vorbisTimeDomainTransformType, WwiseVorbisBitstreamTranscriber.VorbisMappingTypeBitWidth);
        }

        private static VorbisModeConfiguration TranscribeModeConfigurations(BitChunk input, BitWriter output, int mappingCount)
        {
            uint vorbisModeWindowType = 0;
            uint vorbisModeTransformType = 0;
            var modeCountField = input.ReadBits(6);
            output.WriteBits(modeCountField, 6);

            var modeCount = (int)modeCountField + 1;
            var modeBlockSizes = new bool[modeCount];
            for (var modeIndex = 0; modeIndex < modeCount; modeIndex++)
            {
                var blockSizeBit = input.ReadBits(1);
                output.WriteBits(blockSizeBit, 1);
                modeBlockSizes[modeIndex] = blockSizeBit != 0;

                output.WriteBits(vorbisModeWindowType, VorbisModeTypeBitWidth);
                output.WriteBits(vorbisModeTransformType, VorbisModeTypeBitWidth);

                var mappingIndex = input.ReadBits(8);
                if (mappingIndex >= mappingCount)
                    throw new InvalidDataException("Wwise Vorbis mode mapping index is out of range.");

                output.WriteBits(mappingIndex, 8);
            }

            return new VorbisModeConfiguration
            {
                ModeBits = WwiseVorbisBitstreamTranscriber.IntegerLog(modeCount - 1),
                ModeBlockSizes = modeBlockSizes,
            };
        }
    }
}
