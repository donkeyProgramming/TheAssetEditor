using Shared.ByteParsing;

namespace Shared.GameFormats.Wwise.Wem.V132
{
    public static class WwiseVorbisBitstreamTranscriber
    {
        public const int VorbisFloorType1 = 1;
        public const int VorbisFloorTypeBitWidth = 16;
        public const int VorbisMappingType0 = 0;
        public const int VorbisMappingTypeBitWidth = 16;
        public const int VorbisResidueTypeBitWidth = 16;
        public const int WwiseResidueTypeBitWidth = 2;
        public const int SupportedResidueTypeMaximum = 2;

        private const int ConfigurationCountFieldBitWidth = 6;

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

        public static int TranscribeFloorConfigurationsVorbisToWwise(BitChunk input, BitWriter output)
        {
            var floorCountField = input.ReadBits(ConfigurationCountFieldBitWidth);
            output.WriteBits(floorCountField, ConfigurationCountFieldBitWidth);
            var floorCount = (int)floorCountField + 1;
            for (var floorIndex = 0; floorIndex < floorCount; floorIndex++)
                TranscribeFloorConfigurationVorbisToWwise(input, output);
            return floorCount;
        }

        public static void TranscribeFloorConfigurationVorbisToWwise(BitChunk input, BitWriter output)
        {
            var floorType = input.ReadBits(VorbisFloorTypeBitWidth);
            if (floorType != VorbisFloorType1)
                throw new InvalidDataException($"Wwise Vorbis only supports floor type 1; encoder emitted {floorType}.");

            TranscribeFloor1Body(input, output);
        }

        public static int TranscribeResidueConfigurationsVorbisToWwise(BitChunk input, BitWriter output)
        {
            var residueCountField = input.ReadBits(ConfigurationCountFieldBitWidth);
            output.WriteBits(residueCountField, ConfigurationCountFieldBitWidth);
            var residueCount = (int)residueCountField + 1;
            for (var residueIndex = 0; residueIndex < residueCount; residueIndex++)
                TranscribeResidueConfigurationVorbisToWwise(input, output);
            return residueCount;
        }

        public static void TranscribeResidueConfigurationVorbisToWwise(BitChunk input, BitWriter output)
        {
            var residueType = input.ReadBits(VorbisResidueTypeBitWidth);
            if (residueType > SupportedResidueTypeMaximum)
                throw new InvalidDataException($"Wwise Vorbis residue type must be 0..2 (got {residueType}).");

            output.WriteBits(residueType, WwiseResidueTypeBitWidth);
            TranscribeResidueBody(input, output);
        }

        public static int TranscribeMappingConfigurationsVorbisToWwise(BitChunk input, BitWriter output, int channelCount)
        {
            var mappingCountField = input.ReadBits(ConfigurationCountFieldBitWidth);
            output.WriteBits(mappingCountField, ConfigurationCountFieldBitWidth);
            var mappingCount = (int)mappingCountField + 1;
            for (var mappingIndex = 0; mappingIndex < mappingCount; mappingIndex++)
                TranscribeMappingConfigurationVorbisToWwise(input, output, channelCount);
            return mappingCount;
        }

        public static void TranscribeMappingConfigurationVorbisToWwise(BitChunk input, BitWriter output, int channelCount)
        {
            var mappingType = input.ReadBits(VorbisMappingTypeBitWidth);
            if (mappingType != VorbisMappingType0)
                throw new InvalidDataException($"Wwise Vorbis only supports mapping type 0 (got {mappingType}).");

            TranscribeMappingBody(input, output, channelCount);
        }

        public static int TranscribeFloorConfigurationsWwiseToVorbis(BitChunk input, BitWriter output, int codebookCount)
        {
            var floorCountField = input.ReadBits(ConfigurationCountFieldBitWidth);
            output.WriteBits(floorCountField, ConfigurationCountFieldBitWidth);
            var floorCount = (int)floorCountField + 1;
            for (var floorIndex = 0; floorIndex < floorCount; floorIndex++)
                TranscribeFloorConfigurationWwiseToVorbis(input, output, codebookCount);
            return floorCount;
        }

        public static void TranscribeFloorConfigurationWwiseToVorbis(BitChunk input, BitWriter output, int codebookCount)
        {
            output.WriteBits((uint)VorbisFloorType1, VorbisFloorTypeBitWidth);
            TranscribeFloor1Body(input, output, codebookCount);
        }

        public static int TranscribeResidueConfigurationsWwiseToVorbis(BitChunk input, BitWriter output, int codebookCount)
        {
            var residueCountField = input.ReadBits(ConfigurationCountFieldBitWidth);
            output.WriteBits(residueCountField, ConfigurationCountFieldBitWidth);

            var residueCount = (int)residueCountField + 1;
            for (var residueIndex = 0; residueIndex < residueCount; residueIndex++)
                TranscribeResidueConfigurationWwiseToVorbis(input, output, codebookCount);

            return residueCount;
        }

        public static void TranscribeResidueConfigurationWwiseToVorbis(BitChunk input, BitWriter output, int codebookCount)
        {
            var residueType = input.ReadBits(WwiseResidueTypeBitWidth);
            if (residueType > SupportedResidueTypeMaximum)
                throw new InvalidDataException("Wwise Vorbis residue type is invalid.");

            output.WriteBits(residueType, VorbisResidueTypeBitWidth);
            TranscribeResidueBody(input, output, codebookCount);
        }

        public static int TranscribeMappingConfigurationsWwiseToVorbis(BitChunk input, BitWriter output, int channelCount, int floorCount, int residueCount)
        {
            var mappingCountField = input.ReadBits(ConfigurationCountFieldBitWidth);
            output.WriteBits(mappingCountField, ConfigurationCountFieldBitWidth);

            var mappingCount = (int)mappingCountField + 1;
            for (var mappingIndex = 0; mappingIndex < mappingCount; mappingIndex++)
                TranscribeMappingConfigurationWwiseToVorbis(input, output, channelCount, floorCount, residueCount);

            return mappingCount;
        }

        public static void TranscribeMappingConfigurationWwiseToVorbis(BitChunk input, BitWriter output, int channelCount, int floorCount, int residueCount)
        {
            output.WriteBits(VorbisMappingType0, VorbisMappingTypeBitWidth);
            TranscribeMappingBody(input, output, channelCount, floorCount, residueCount);
        }

        public static void TranscribeFloor1Body(BitChunk input, BitWriter output, int codebookCount = -1)
        {
            var floorPartitionCountBitWidth = 5;
            var floorClassIndexBitWidth = 4;
            var floorClassDimensionsBitWidth = 3;
            var floorSubclassCountBitWidth = 2;
            var floorMasterbookIndexBitWidth = 8;
            var floorSubclassBookPlusOneBitWidth = 8;
            var floorMultiplierBitWidth = 2;
            var floorRangeBitsBitWidth = 4;

            var partitionCount = (int)input.ReadBits(floorPartitionCountBitWidth);
            output.WriteBits((uint)partitionCount, floorPartitionCountBitWidth);

            var classIndexPerPartition = new int[partitionCount];
            var highestClassIndex = 0;
            for (var partitionIndex = 0; partitionIndex < partitionCount; partitionIndex++)
            {
                var classIndex = (int)input.ReadBits(floorClassIndexBitWidth);
                output.WriteBits((uint)classIndex, floorClassIndexBitWidth);
                classIndexPerPartition[partitionIndex] = classIndex;
                highestClassIndex = Math.Max(highestClassIndex, classIndex);
            }

            var dimensionsPerClass = new int[highestClassIndex + 1];
            for (var classIndex = 0; classIndex <= highestClassIndex; classIndex++)
            {
                var classDimensionsField = (int)input.ReadBits(floorClassDimensionsBitWidth);
                output.WriteBits((uint)classDimensionsField, floorClassDimensionsBitWidth);
                dimensionsPerClass[classIndex] = classDimensionsField + 1;
                var subclassCount = (int)input.ReadBits(floorSubclassCountBitWidth);
                output.WriteBits((uint)subclassCount, floorSubclassCountBitWidth);

                if (subclassCount != 0)
                {
                    var masterbookIndex = input.ReadBits(floorMasterbookIndexBitWidth);
                    if (codebookCount >= 0 && masterbookIndex >= (uint)codebookCount)
                        throw new InvalidDataException("Wwise Vorbis floor master book index is out of range.");
                    output.WriteBits(masterbookIndex, floorMasterbookIndexBitWidth);
                }

                for (var subclassIndex = 0; subclassIndex < (1 << subclassCount); subclassIndex++)
                {
                    var subclassBookPlusOne = (int)input.ReadBits(floorSubclassBookPlusOneBitWidth);
                    output.WriteBits((uint)subclassBookPlusOne, floorSubclassBookPlusOneBitWidth);
                    if (codebookCount >= 0 && subclassBookPlusOne - 1 >= 0 && subclassBookPlusOne - 1 >= codebookCount)
                        throw new InvalidDataException("Wwise Vorbis floor subclass book index is out of range.");
                }
            }

            var multiplier = input.ReadBits(floorMultiplierBitWidth);
            output.WriteBits(multiplier, floorMultiplierBitWidth);

            var rangeBits = (int)input.ReadBits(floorRangeBitsBitWidth);
            output.WriteBits((uint)rangeBits, floorRangeBitsBitWidth);

            for (var partitionIndex = 0; partitionIndex < partitionCount; partitionIndex++)
            {
                var classIndex = classIndexPerPartition[partitionIndex];
                for (var dimensionIndex = 0; dimensionIndex < dimensionsPerClass[classIndex]; dimensionIndex++)
                    output.WriteBits(input.ReadBits(rangeBits), rangeBits);
            }
        }

        public static void TranscribeResidueBody(BitChunk input, BitWriter output, int codebookCount = -1)
        {
            var residueVectorFieldBitWidth = 24;
            var residueClassificationCountBitWidth = 6;
            var residueClassbookIndexBitWidth = 8;
            var residueCascadeLowBitsWidth = 3;
            var residueCascadeHasHighBitsWidth = 1;
            var residueCascadeHighBitsWidth = 5;
            var residueBookIndexBitWidth = 8;
            var cascadeVectorBitCount = 8;

            output.WriteBits(input.ReadBits(residueVectorFieldBitWidth), residueVectorFieldBitWidth);
            output.WriteBits(input.ReadBits(residueVectorFieldBitWidth), residueVectorFieldBitWidth);
            output.WriteBits(input.ReadBits(residueVectorFieldBitWidth), residueVectorFieldBitWidth);

            var classificationCountField = input.ReadBits(residueClassificationCountBitWidth);
            output.WriteBits(classificationCountField, residueClassificationCountBitWidth);
            var classificationCount = (int)classificationCountField + 1;

            var classbookIndex = input.ReadBits(residueClassbookIndexBitWidth);
            if (codebookCount >= 0 && classbookIndex >= (uint)codebookCount)
                throw new InvalidDataException("Wwise Vorbis residue classbook index is out of range.");

            output.WriteBits(classbookIndex, residueClassbookIndexBitWidth);

            var cascadeVectors = new int[classificationCount];
            for (var classIndex = 0; classIndex < classificationCount; classIndex++)
            {
                var lowBits = (int)input.ReadBits(residueCascadeLowBitsWidth);
                output.WriteBits((uint)lowBits, residueCascadeLowBitsWidth);

                var hasHighBits = input.ReadBits(residueCascadeHasHighBitsWidth);
                output.WriteBits(hasHighBits, residueCascadeHasHighBitsWidth);

                var highBits = 0;
                if (hasHighBits != 0)
                {
                    highBits = (int)input.ReadBits(residueCascadeHighBitsWidth);
                    output.WriteBits((uint)highBits, residueCascadeHighBitsWidth);
                }

                cascadeVectors[classIndex] = (highBits << 3) | lowBits;
            }

            for (var classIndex = 0; classIndex < classificationCount; classIndex++)
            {
                for (var bookBitPosition = 0; bookBitPosition < cascadeVectorBitCount; bookBitPosition++)
                {
                    if (!BitHelper.IsBitSet(cascadeVectors[classIndex], bookBitPosition))
                        continue;

                    var bookIndex = input.ReadBits(residueBookIndexBitWidth);
                    if (codebookCount >= 0 && bookIndex >= (uint)codebookCount)
                        throw new InvalidDataException("Wwise Vorbis residue book index is out of range.");

                    output.WriteBits(bookIndex, residueBookIndexBitWidth);
                }
            }
        }

        public static void TranscribeMappingBody(BitChunk input, BitWriter output, int channelCount, int floorCount = -1, int residueCount = -1)
        {
            var mappingHasMultipleSubmapsBitWidth = 1;
            var mappingSubmapCountFieldBitWidth = 4;
            var mappingHasCouplingStepsBitWidth = 1;
            var mappingCouplingStepCountFieldBitWidth = 8;
            var mappingReservedFieldBitWidth = 2;
            var mappingChannelSubmapIndexBitWidth = 4;
            var mappingSubmapFieldBitWidth = 8;

            var hasMultipleSubmaps = input.ReadBits(mappingHasMultipleSubmapsBitWidth);
            output.WriteBits(hasMultipleSubmaps, mappingHasMultipleSubmapsBitWidth);

            var submapCount = 1;
            if (hasMultipleSubmaps != 0)
            {
                var submapCountField = (int)input.ReadBits(mappingSubmapCountFieldBitWidth);
                output.WriteBits((uint)submapCountField, mappingSubmapCountFieldBitWidth);
                submapCount = submapCountField + 1;
            }

            var hasCouplingSteps = input.ReadBits(mappingHasCouplingStepsBitWidth);
            output.WriteBits(hasCouplingSteps, mappingHasCouplingStepsBitWidth);

            if (hasCouplingSteps != 0)
            {
                var couplingStepCountField = (int)input.ReadBits(mappingCouplingStepCountFieldBitWidth);
                output.WriteBits((uint)couplingStepCountField, mappingCouplingStepCountFieldBitWidth);

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

            var reservedField = input.ReadBits(mappingReservedFieldBitWidth);
            if (floorCount >= 0 && reservedField != 0)
                throw new InvalidDataException("Wwise Vorbis mapping reserved field must be zero.");

            output.WriteBits(reservedField, mappingReservedFieldBitWidth);

            if (submapCount > 1)
            {
                for (var channelIndex = 0; channelIndex < channelCount; channelIndex++)
                {
                    var channelSubmapIndex = input.ReadBits(mappingChannelSubmapIndexBitWidth);
                    if (floorCount >= 0 && channelSubmapIndex >= (uint)submapCount)
                        throw new InvalidDataException("Wwise Vorbis channel submap index is out of range.");
                        
                    output.WriteBits(channelSubmapIndex, mappingChannelSubmapIndexBitWidth);
                }
            }

            for (var submapIndex = 0; submapIndex < submapCount; submapIndex++)
            {
                var unusedSubmapField = input.ReadBits(mappingSubmapFieldBitWidth);
                output.WriteBits(unusedSubmapField, mappingSubmapFieldBitWidth);
                var floorIndex = input.ReadBits(mappingSubmapFieldBitWidth);
                if (floorCount >= 0 && floorIndex >= (uint)floorCount)
                    throw new InvalidDataException("Wwise Vorbis submap floor index is out of range.");

                output.WriteBits(floorIndex, mappingSubmapFieldBitWidth);
                var residueIndex = input.ReadBits(mappingSubmapFieldBitWidth);
                if (residueCount >= 0 && residueIndex >= (uint)residueCount)
                    throw new InvalidDataException("Wwise Vorbis submap residue index is out of range.");
                    
                output.WriteBits(residueIndex, mappingSubmapFieldBitWidth);
            }
        }
    }
}
