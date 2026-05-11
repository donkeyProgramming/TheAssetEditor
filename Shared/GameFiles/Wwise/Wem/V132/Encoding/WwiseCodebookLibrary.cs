using System.Buffers.Binary;
using Shared.ByteParsing;
using Shared.EmbeddedResources;

namespace Shared.GameFormats.Wwise.Wem.V132.Encoding
{
    public class WwiseCodebookLibrary
    {
        private readonly byte[] _packedCodebooks;
        private readonly int[] _codebookOffsets;
        private readonly Dictionary<string, int> _libraryIdByCodebookBits;

        public WwiseCodebookLibrary()
        {
            _packedCodebooks = ResourceLoader.LoadBytes("Resources.Wwise.packed_codebooks_aoTuV_603.bin");
            _codebookOffsets = ParseCodebookOffsets(_packedCodebooks);
            _libraryIdByCodebookBits = BuildLibraryIdLookup();
        }

        public int LibraryCount => Math.Max(0, _codebookOffsets.Length - 1);

        public VorbisCodebook GetCodebook(int codebookId)
        {
            var packed = GetPackedEntry(codebookId);
            var writer = new BitWriter(Math.Max(packed.Length * 4, 64));
            TranscribeFromPackedEntry(writer, packed);
            return new VorbisCodebook(writer.ToArray(), writer.BitPosition);
        }

        public int FindLibraryId(byte[] standardCodebookBits, int bitCount)
        {
            var fingerprint = BuildFingerprint(standardCodebookBits, bitCount);
            if (_libraryIdByCodebookBits.TryGetValue(fingerprint, out var matchedId))
                return matchedId;

            throw new InvalidDataException($"Vorbis codebook with bitCount {bitCount} was not found");
        }

        private ReadOnlySpan<byte> GetPackedEntry(int codebookId)
        {
            if (codebookId < 0 || codebookId + 1 >= _codebookOffsets.Length)
                throw new InvalidDataException($"Wwise Vorbis codebook {codebookId} was not found in the codebook library.");

            var startOffset = _codebookOffsets[codebookId];
            var endOffset = _codebookOffsets[codebookId + 1];
            if (startOffset < 0 || endOffset < startOffset || endOffset > _packedCodebooks.Length)
                throw new InvalidDataException($"Wwise Vorbis codebook {codebookId} has an invalid packed offset range.");

            return _packedCodebooks.AsSpan(startOffset, endOffset - startOffset);
        }

        private Dictionary<string, int> BuildLibraryIdLookup()
        {
            var lookup = new Dictionary<string, int>(LibraryCount);
            for (var codebookId = 0; codebookId < LibraryCount; codebookId++)
            {
                var codebook = GetCodebook(codebookId);
                lookup[BuildFingerprint(codebook.Data, codebook.BitCount)] = codebookId;
            }
            return lookup;
        }

        private static int[] ParseCodebookOffsets(byte[] data)
        {
            var tableOffset = (int)BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(data.Length - 4, 4));
            var count = (data.Length - tableOffset) / 4;
            var offsets = new int[count];

            for (var index = 0; index < count; index++)
                offsets[index] = (int)BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(tableOffset + index * 4, 4));

            return offsets;
        }

        private static string BuildFingerprint(byte[] bits, int bitCount) => bitCount.ToString() + ':' + Convert.ToHexString(bits);

        private static void TranscribeFromPackedEntry(BitWriter output, ReadOnlySpan<byte> codebookBytes)
        {
            var input = new BitChunk(codebookBytes.ToArray());
            output.WriteBits(VorbisCodebook.SyncPattern, VorbisCodebook.SyncPatternBitWidth);

            var dimensions = (int)input.ReadBits(4);
            output.WriteBits((uint)dimensions, VorbisCodebook.DimensionsBitWidth);

            var entries = (int)input.ReadBits(14);
            output.WriteBits((uint)entries, VorbisCodebook.EntriesBitWidth);

            var ordered = input.ReadBits(1);
            output.WriteBits(ordered, 1);
            if (ordered != 0)
            {
                var initialCodewordLength = input.ReadBits(5);
                output.WriteBits(initialCodewordLength, 5);

                var currentEntry = 0;
                while (currentEntry < entries)
                {
                    var bits = VorbisHelpers.IntegerLog(entries - currentEntry);
                    var number = (int)input.ReadBits(bits);
                    output.WriteBits((uint)number, bits);
                    currentEntry += number;
                }

                if (currentEntry > entries)
                    throw new InvalidDataException("Wwise Vorbis codebook entry count exceeded the declared size.");
            }
            else
            {
                var codewordLengthLength = (int)input.ReadBits(3);
                var sparse = input.ReadBits(1);
                output.WriteBits(sparse, 1);
                if (codewordLengthLength is < 1 or > 5)
                    throw new InvalidDataException("Wwise Vorbis codebook codeword length field width is invalid.");

                for (var entryIndex = 0; entryIndex < entries; entryIndex++)
                {
                    var present = true;
                    if (sparse != 0)
                    {
                        present = input.ReadBits(1) != 0;
                        output.WriteBits(present ? 1u : 0u, 1);
                    }

                    if (!present)
                        continue;

                    output.WriteBits(input.ReadBits(codewordLengthLength), 5);
                }
            }

            var lookupType = input.ReadBits(1);
            output.WriteBits(lookupType, 4);

            if (lookupType == 0)
                return;

            if (lookupType != 1)
                throw new InvalidDataException("Wwise Vorbis codebook uses an unsupported lookup type.");

            output.WriteBits(input.ReadBits(VorbisCodebook.VqFloatFieldBitWidth), VorbisCodebook.VqFloatFieldBitWidth);
            output.WriteBits(input.ReadBits(VorbisCodebook.VqFloatFieldBitWidth), VorbisCodebook.VqFloatFieldBitWidth);

            var valueLength = input.ReadBits(VorbisCodebook.ValueLengthBitWidth);
            output.WriteBits(valueLength, VorbisCodebook.ValueLengthBitWidth);
            output.WriteBits(input.ReadBits(1), 1);

            var quantisedValueCount = VorbisHelpers.ComputeMapType1QuantValues(entries, dimensions);
            for (var quantisedIndex = 0; quantisedIndex < quantisedValueCount; quantisedIndex++)
                output.WriteBits(input.ReadBits((int)valueLength + 1), (int)valueLength + 1);
        }
    }
}
