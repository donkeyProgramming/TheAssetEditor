using Shared.ByteParsing;

namespace Shared.GameFormats.Wwise.Wem.V132
{
    public static class VorbisHelpers
    {
        private const int ResidueVectorFieldBitWidth = 24;

        public static int ComputeMapType1QuantValues(int entries, int dimensions)
        {
            var bits = IntegerLog(entries);
            var values = entries >> ((bits - 1) * (dimensions - 1) / dimensions);

            var maxIterations = entries + dimensions + 2;
            while (maxIterations-- > 0)
            {
                ulong current = 1;
                ulong next = 1;
                for (var dimensionIndex = 0; dimensionIndex < dimensions; dimensionIndex++)
                {
                    current *= (uint)values;
                    next *= (uint)(values + 1);
                }

                if (current <= (ulong)entries && next > (ulong)entries)
                    return values;

                values += current > (ulong)entries ? -1 : 1;
            }

            throw new InvalidDataException("ComputeMapType1QuantValues failed to converge.");
        }

        public static int IntegerLog(int value)
        {
            var result = 0;
            while (value > 0)
            {
                result++;
                value >>= 1;
            }
            return result;
        }

        public static void TranscribeFloor1Body(BitChunk input, BitWriter output, int codebookCount = -1)
        {
            var partitionCount = (int)input.ReadBits(5);
            output.WriteBits((uint)partitionCount, 5);

            var classIndexPerPartition = new int[partitionCount];
            var highestClassIndex = 0;
            for (var partitionIndex = 0; partitionIndex < partitionCount; partitionIndex++)
            {
                var classIndex = (int)input.ReadBits(4);
                output.WriteBits((uint)classIndex, 4);
                classIndexPerPartition[partitionIndex] = classIndex;
                highestClassIndex = Math.Max(highestClassIndex, classIndex);
            }

            var dimensionsPerClass = new int[highestClassIndex + 1];
            for (var classIndex = 0; classIndex <= highestClassIndex; classIndex++)
            {
                var classDimensionsField = (int)input.ReadBits(3);
                output.WriteBits((uint)classDimensionsField, 3);
                dimensionsPerClass[classIndex] = classDimensionsField + 1;
                var subclassCount = (int)input.ReadBits(2);
                output.WriteBits((uint)subclassCount, 2);

                if (subclassCount != 0)
                {
                    var masterbookIndex = input.ReadBits(8);
                    if (codebookCount >= 0 && masterbookIndex >= (uint)codebookCount)
                        throw new InvalidDataException("Wwise Vorbis floor master book index is out of range.");
                    output.WriteBits(masterbookIndex, 8);
                }

                for (var subclassIndex = 0; subclassIndex < (1 << subclassCount); subclassIndex++)
                {
                    var subclassBookPlusOne = (int)input.ReadBits(8);
                    output.WriteBits((uint)subclassBookPlusOne, 8);
                    if (codebookCount >= 0 && subclassBookPlusOne - 1 >= 0 && subclassBookPlusOne - 1 >= codebookCount)
                        throw new InvalidDataException("Wwise Vorbis floor subclass book index is out of range.");
                }
            }

            var multiplier = input.ReadBits(2);
            output.WriteBits(multiplier, 2);

            var rangeBits = (int)input.ReadBits(4);
            output.WriteBits((uint)rangeBits, 4);

            for (var partitionIndex = 0; partitionIndex < partitionCount; partitionIndex++)
            {
                var classIndex = classIndexPerPartition[partitionIndex];
                for (var dimensionIndex = 0; dimensionIndex < dimensionsPerClass[classIndex]; dimensionIndex++)
                    output.WriteBits(input.ReadBits(rangeBits), rangeBits);
            }
        }

        public static void TranscribeResidueBody(BitChunk input, BitWriter output, int codebookCount = -1)
        {
            output.WriteBits(input.ReadBits(ResidueVectorFieldBitWidth), ResidueVectorFieldBitWidth);
            output.WriteBits(input.ReadBits(ResidueVectorFieldBitWidth), ResidueVectorFieldBitWidth);
            output.WriteBits(input.ReadBits(ResidueVectorFieldBitWidth), ResidueVectorFieldBitWidth);

            var classificationCountField = input.ReadBits(6);
            output.WriteBits(classificationCountField, 6);
            var classificationCount = (int)classificationCountField + 1;

            var classbookIndex = input.ReadBits(8);
            if (codebookCount >= 0 && classbookIndex >= (uint)codebookCount)
                throw new InvalidDataException("Wwise Vorbis residue classbook index is out of range.");

            output.WriteBits(classbookIndex, 8);

            var cascadeVectors = new int[classificationCount];
            for (var classIndex = 0; classIndex < classificationCount; classIndex++)
            {
                var lowBits = (int)input.ReadBits(3);
                output.WriteBits((uint)lowBits, 3);

                var hasHighBits = input.ReadBits(1);
                output.WriteBits(hasHighBits, 1);

                var highBits = 0;
                if (hasHighBits != 0)
                {
                    highBits = (int)input.ReadBits(5);
                    output.WriteBits((uint)highBits, 5);
                }

                cascadeVectors[classIndex] = (highBits << 3) | lowBits;
            }

            for (var classIndex = 0; classIndex < classificationCount; classIndex++)
            {
                for (var bookBitPosition = 0; bookBitPosition < 8; bookBitPosition++)
                {
                    if (!BitHelper.IsBitSet(cascadeVectors[classIndex], bookBitPosition))
                        continue;

                    var bookIndex = input.ReadBits(8);
                    if (codebookCount >= 0 && bookIndex >= (uint)codebookCount)
                        throw new InvalidDataException("Wwise Vorbis residue book index is out of range.");

                    output.WriteBits(bookIndex, 8);
                }
            }
        }

        public static void TranscribeMappingBody(BitChunk input, BitWriter output, int channelCount, int floorCount = -1, int residueCount = -1)
        {
            var hasMultipleSubmaps = input.ReadBits(1);
            output.WriteBits(hasMultipleSubmaps, 1);

            var submapCount = 1;
            if (hasMultipleSubmaps != 0)
            {
                var submapCountField = (int)input.ReadBits(4);
                output.WriteBits((uint)submapCountField, 4);
                submapCount = submapCountField + 1;
            }

            var hasCouplingSteps = input.ReadBits(1);
            output.WriteBits(hasCouplingSteps, 1);

            if (hasCouplingSteps != 0)
            {
                var couplingStepCountField = (int)input.ReadBits(8);
                output.WriteBits((uint)couplingStepCountField, 8);

                var couplingStepCount = couplingStepCountField + 1;
                var channelIndexBits = IntegerLog(channelCount - 1);
                for (var stepIndex = 0; stepIndex < couplingStepCount; stepIndex++)
                {
                    var magnitudeChannelIndex = (int)input.ReadBits(channelIndexBits);
                    var angleChannelIndex = (int)input.ReadBits(channelIndexBits);
                    if (floorCount >= 0 && (magnitudeChannelIndex >= channelCount || angleChannelIndex >= channelCount || magnitudeChannelIndex == angleChannelIndex))
                        throw new InvalidDataException("Wwise Vorbis channel coupling step references invalid channel indices.");

                    output.WriteBits((uint)magnitudeChannelIndex, channelIndexBits);
                    output.WriteBits((uint)angleChannelIndex, channelIndexBits);
                }
            }

            var reservedField = input.ReadBits(2);
            if (floorCount >= 0 && reservedField != 0)
                throw new InvalidDataException("Wwise Vorbis mapping reserved field must be zero.");

            output.WriteBits(reservedField, 2);

            if (submapCount > 1)
            {
                for (var channelIndex = 0; channelIndex < channelCount; channelIndex++)
                {
                    var channelSubmapIndex = input.ReadBits(4);
                    if (floorCount >= 0 && channelSubmapIndex >= (uint)submapCount)
                        throw new InvalidDataException("Wwise Vorbis channel submap index is out of range.");
                        
                    output.WriteBits(channelSubmapIndex, 4);
                }
            }

            for (var submapIndex = 0; submapIndex < submapCount; submapIndex++)
            {
                var unusedSubmapField = input.ReadBits(8);
                output.WriteBits(unusedSubmapField, 8);
                var floorIndex = input.ReadBits(8);
                if (floorCount >= 0 && floorIndex >= (uint)floorCount)
                    throw new InvalidDataException("Wwise Vorbis submap floor index is out of range.");

                output.WriteBits(floorIndex, 8);
                var residueIndex = input.ReadBits(8);
                if (residueCount >= 0 && residueIndex >= (uint)residueCount)
                    throw new InvalidDataException("Wwise Vorbis submap residue index is out of range.");
                    
                output.WriteBits(residueIndex, 8);
            }
        }
    }
}
