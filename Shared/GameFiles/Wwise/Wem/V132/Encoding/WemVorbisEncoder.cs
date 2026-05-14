using Shared.ByteParsing;
using Shared.GameFormats.Audio.Codecs;
using Shared.GameFormats.Audio.Ogg;
using Shared.GameFormats.Audio.Wav;

namespace Shared.GameFormats.Wwise.Wem.V132.Encoding
{
    public class WemVorbisEncoder(WwiseCodebookLibrary codebookLibrary)
    {
        private readonly WwiseCodebookLibrary _codebookLibrary = codebookLibrary;

        public WemEncodingSettings EncodingSettings { get; set; } = new WemEncodingSettings();

        private const int EncodingWriteBufferSize = 512;
        private const float DefaultSilenceDbFloor = -96.0f;

        public WemFile EncodeFromWav(byte[] wavData, WemEncodingSettings? encodingSettings = null)
        {
            ArgumentNullException.ThrowIfNull(wavData);
            var wavFile = WavFile.CreateFromBytes(wavData);
            var perChannelSamples = ConvertPcmToPerChannelFloat(wavFile.Audio, wavFile.FmtChunk.FormatTag);
            return EncodeFloatSamplesToWem(perChannelSamples, checked(wavFile.Audio.Channels), checked((int)wavFile.Audio.SampleRate));
        }

        private WemFile EncodeFloatSamplesToWem(float[][] perChannelSamples, int channels, int sampleRate)
        {
            var vorbisIdPacketBlockSizeByteOffset = 28;
            uint wemChannelMaskMetadataBits = 0x100;
            var wemChannelMaskShift = 12;
            uint waveMonoChannelMask = 4;
            uint waveStereoChannelMask = 3;
            uint waveSurroundChannelMask = 63;
            uint waveFullSurroundChannelMask = 255;

            var info = OggVorbisEncoder.VorbisInfo.InitVariableBitRate(channels, sampleRate, EncodingSettings.Quality);
            var comments = new OggVorbisEncoder.Comments();
            var identificationPacket = OggVorbisEncoder.HeaderPacketBuilder.BuildInfoPacket(info);
            var booksPacket = OggVorbisEncoder.HeaderPacketBuilder.BuildBooksPacket(info);
            OggVorbisEncoder.HeaderPacketBuilder.BuildCommentsPacket(comments);

            var processingState = OggVorbisEncoder.ProcessingState.Create(info);
            var sampleCount = perChannelSamples[0].Length;
            for (var readOffset = 0; readOffset < sampleCount; readOffset += EncodingWriteBufferSize)
            {
                var length = Math.Min(EncodingWriteBufferSize, sampleCount - readOffset);
                processingState.WriteData(perChannelSamples, length, readOffset);
            }
            processingState.WriteEndOfStream();

            var oggAudioPackets = new List<OggAudioPacket>();
            while (processingState.PacketOut(out var oggPacket))
                oggAudioPackets.Add(new OggAudioPacket { PacketData = oggPacket.PacketData, GranulePosition = oggPacket.GranulePosition });

            var wemSetupData = CompressSetup(booksPacket.PacketData, channels);
            var previousPacketIsLargeBlock = false;
            var wemAudioPackets = new List<WemAudioPacket>();
            for (var i = 0; i < oggAudioPackets.Count; i++)
            {
                var wemPacketData = CompressAudioPacket(oggAudioPackets[i].PacketData, wemSetupData.ModeConfiguration, ref previousPacketIsLargeBlock);
                wemAudioPackets.Add(new WemAudioPacket { Data = wemPacketData, GranulePosition = oggAudioPackets[i].GranulePosition });
            }

            var blockSizeByte = identificationPacket.PacketData[vorbisIdPacketBlockSizeByteOffset];
            var blockSize0Exponent = BitHelper.ExtractBits(blockSizeByte, 4, 4);
            var blockSize1Exponent = BitHelper.ExtractBits(blockSizeByte, 0, 4);
            var largeBlockSize = 1 << blockSize0Exponent;
            var smallBlockSize = 1 << blockSize1Exponent;

            long lastGranulePosition;
            if (wemAudioPackets.Count > 0)
                lastGranulePosition = wemAudioPackets[wemAudioPackets.Count - 1].GranulePosition;
            else
                lastGranulePosition = sampleCount;
                
            var lastGranuleExtra = (int)(lastGranulePosition - sampleCount);
            if (lastGranuleExtra < 0)
                lastGranuleExtra = 0;

            var seekTable = new List<WemSeekTableRecord>();
            if (EncodingSettings.UseSeekTable)
                seekTable = BuildSeekTable(wemAudioPackets, largeBlockSize, smallBlockSize, sampleCount, lastGranuleExtra, wemSetupData);

            var codebookHash = WwiseHash.Compute(wemSetupData.SetupPacketBytes);
            var seekTableSize = (uint)(seekTable.Count * WemSeekTableRecord.Size);
            var setupPacketSize = sizeof(ushort) + wemSetupData.SetupPacketBytes.Length;
            var audioDataSize = wemAudioPackets.Sum(p => WemAudioPacket.LengthPrefixSize + p.Data.Length);
            var totalDataSize = seekTableSize + setupPacketSize + audioDataSize;
            var averageBytesPerSecond = (uint)(sampleCount > 0 ? totalDataSize * sampleRate / sampleCount : 0);

            uint channelMask;
            if (channels == 1)
                channelMask = waveMonoChannelMask;
            else if (channels == 2)
                channelMask = waveStereoChannelMask;
            else if (channels == 6)
                channelMask = waveSurroundChannelMask;
            else if (channels == 8)
                channelMask = waveFullSurroundChannelMask;
            else
                channelMask = (uint)((1 << channels) - 1u);

            var fmtChunkChannelMask = (uint)channels | wemChannelMaskMetadataBits | (channelMask << wemChannelMaskShift);

            var fmtChunk = new FmtChunk
            {
                Channels = checked((ushort)channels),
                SampleRate = checked((uint)sampleRate),
                AverageBytesPerSecond = averageBytesPerSecond,
                ChannelMask = fmtChunkChannelMask,
                SampleCount = sampleCount,
                SetupPacketSize = (uint)setupPacketSize,
                AudioDataOffset = (uint)(setupPacketSize + audioDataSize),
                LastGranuleExtra = (ushort)lastGranuleExtra,
                SeekTableSize = seekTableSize,
                FirstAudioPacketOffset = seekTableSize + (uint)setupPacketSize,
                MaxPacketSize = (ushort)(wemAudioPackets.Count > 0 ? wemAudioPackets.Max(packet => packet.Data.Length) : 0),
                LastGranuleExtra2 = (ushort)lastGranuleExtra,
                CodebookHash = codebookHash,
                SmallBlockSizeExponent = (byte)blockSize1Exponent,
                LargeBlockSizeExponent = (byte)blockSize0Exponent,
            };
            
            AkdChunk? akdChunk = null;
            if (EncodingSettings.GenerateAkdChunk)
                akdChunk = BuildAkdChunk(perChannelSamples, channels, sampleRate);

            JunkChunk? junkChunk = null;
            if (akdChunk != null)
            {
                var fmtDataSize = (int)FmtChunk.ChunkSize;
                var akdDataSize = akdChunk.GetSize();
                junkChunk = BuildJunkChunkForAkd(fmtDataSize, akdDataSize);
            }

            return new WemFile
            {
                FmtChunk = fmtChunk,
                DataChunk = new DataChunk
                {
                    SeekTable = seekTable,
                    SetupPacket = wemSetupData.SetupPacketBytes,
                    AudioPackets = wemAudioPackets,
                },
                JunkChunk = junkChunk,
                AkdChunk = akdChunk,
            };
        }

        private AkdChunk BuildAkdChunk(float[][] perChannelSamples, int channels, int sampleRate)
        {
            // Measure integrated programme loudness using BS.1770 K-weighting with the standard absolute gate (-70 LUFS) 
            // and relative gate (-10 LU), then convert the LUFS delta to the linear gain stored in the AKD chunk.
            var measuredLufs = MeasureBS1770Loudness(perChannelSamples, channels, sampleRate);
            var gainDb = EncodingSettings.TargetLoudnessDb - measuredLufs;
            var loudnessNormalisationGain = MathF.Pow(10.0f, gainDb / 20.0f);

            var channelCount = perChannelSamples.Length;
            var downmixNormalisationGain = 1.0f;
            if (channelCount > 2)
                downmixNormalisationGain = MathF.Sqrt(2.0f / channelCount);

            var akdChunk = new AkdChunk
            {
                LoudnessNormalisationGain = loudnessNormalisationGain,
                DownmixNormalisationGain = downmixNormalisationGain,
            };

            if (!EncodingSettings.IncludeHdrEnvelopeData)
                return akdChunk;

            var monoMix = BuildMonoMix(perChannelSamples);
            if (monoMix.Length == 0)
                return akdChunk;

            var windowSize = Math.Max(64, EncodingSettings.HdrEnvelopeWindowSizeSamples);
            var pointCount = (monoMix.Length + windowSize - 1) / windowSize;
            var envelopeData = new byte[pointCount * AkdChunk.EnvelopePointSize];
            var peakRmsDb = DefaultSilenceDbFloor;

            for (var pointIndex = 0; pointIndex < pointCount; pointIndex++)
            {
                var sampleIndex = pointIndex * windowSize;
                var samplesInWindow = Math.Min(windowSize, monoMix.Length - sampleIndex);
                var windowRms = ComputeRms(monoMix, sampleIndex, samplesInWindow);
                var windowDb = LinearToDb(windowRms);
                peakRmsDb = Math.Max(peakRmsDb, windowDb);

                var quantisedDb = QuantiseEnvelopeDb(windowDb);
                var envelopeByteOffset = pointIndex * (int)AkdChunk.EnvelopePointSize;

                var sampleIndexBytes = BitConverter.GetBytes((uint)sampleIndex);
                envelopeData[envelopeByteOffset] = sampleIndexBytes[0];
                envelopeData[envelopeByteOffset + 1] = sampleIndexBytes[1];
                envelopeData[envelopeByteOffset + 2] = sampleIndexBytes[2];
                envelopeData[envelopeByteOffset + 3] = sampleIndexBytes[3];

                var quantisedBytes = BitConverter.GetBytes(quantisedDb);
                envelopeData[envelopeByteOffset + 4] = quantisedBytes[0];
                envelopeData[envelopeByteOffset + 5] = quantisedBytes[1];
            }

            akdChunk.HdrEnvelopePointCount = checked((uint)pointCount);
            akdChunk.HdrPeakRms = peakRmsDb;
            akdChunk.EnvelopeData = envelopeData;

            return akdChunk;
        }

        private static JunkChunk BuildJunkChunkForAkd(int fmtDataSize, int akdDataSize)
        {
            var targetDataChunkPayloadOffset = 128;
            var dataOffsetAlignment = 16;
            var chunkHeaderSize = (int)RiffChunkHeader.HeaderSize;
            var fmtPadding = fmtDataSize % RiffChunkHeader.ChunkPaddingAlignment == 0 ? 0 : 1;
            var akdPadding = akdDataSize % RiffChunkHeader.ChunkPaddingAlignment == 0 ? 0 : 1;

            var bytesBeforeJunk = WemFileHeader.Size + chunkHeaderSize + fmtDataSize + fmtPadding + chunkHeaderSize;
            var bytesAfterJunk = chunkHeaderSize + akdDataSize + akdPadding + chunkHeaderSize;
            var minimumTargetOffset = Math.Max(targetDataChunkPayloadOffset, bytesBeforeJunk + bytesAfterJunk);
            var alignedTargetOffset = ((minimumTargetOffset + dataOffsetAlignment - 1) / dataOffsetAlignment) * dataOffsetAlignment;

            return new JunkChunk { Padding = JunkChunk.CalculatePadding(alignedTargetOffset, bytesBeforeJunk, bytesAfterJunk) };
        }

        private static float MeasureBS1770Loudness(float[][] perChannelSamples, int channels, int sampleRate)
        {
            var coefficients = ComputeKWeightingCoefficients(sampleRate);

            // Channel weights per BS.1770: L/R/C = 1.0, LFE = 0.0, Ls/Rs = 1.41.
            // For mono=1 and stereo=2 all channels are weight 1.0.
            // For 5.1 (6ch): L R C LFE Ls Rs → weights 1,1,1,0,1.41,1.41 → sum contribution.
            var channelWeights = GetBS1770ChannelWeights(channels);

            var filteredMeanSquare = new double[channels];
            for (var c = 0; c < channels; c++)
            {
                var filtered = ApplyBiquadCascade(perChannelSamples[c], coefficients.PreFilter, coefficients.RlbFilter);
                var sumSquares = 0.0;
                foreach (var s in filtered) sumSquares += s * s;
                filteredMeanSquare[c] = sumSquares / filtered.Length;
            }

            // Gating block size: 400 ms with 75% overlap (100 ms step).
            var blockSamples = (int)(sampleRate * 0.400);
            var stepSamples  = (int)(sampleRate * 0.100);
            if (blockSamples <= 0)
                blockSamples = 1;
            if (stepSamples <= 0)
                stepSamples = 1;

            var sampleCount = perChannelSamples[0].Length;
            var blocks = new List<double>();

            var filteredChannels = new double[channels][];
            for (var c = 0; c < channels; c++)
            {
                var filteredChannel = ApplyBiquadCascade(perChannelSamples[c], coefficients.PreFilter, coefficients.RlbFilter);
                filteredChannels[c] = Array.ConvertAll(filteredChannel, x => (double)x);
            }

            for (var start = 0; start + blockSamples <= sampleCount; start += stepSamples)
            {
                var blockLoudness = 0.0;
                for (var c = 0; c < channels; c++)
                {
                    var sumSquares = 0.0;
                    for (var i = start; i < start + blockSamples; i++)
                        sumSquares += filteredChannels[c][i] * filteredChannels[c][i];
                    blockLoudness += channelWeights[c] * (sumSquares / blockSamples);
                }
                blocks.Add(blockLoudness);
            }

            if (blocks.Count == 0)
                return DefaultSilenceDbFloor;

            // Absolute BS.1770 gate: keep only blocks at or above -70 LUFS.
            // In linear mean-square power this threshold is approximately 1e-7.
            var absoluteGateLinear = 1e-7;
            var absGated = blocks.Where(b => b >= absoluteGateLinear).ToList();
            if (absGated.Count == 0)
                return DefaultSilenceDbFloor;

            // Relative BS.1770 gate: recompute the threshold from the absolute-gated
            // programme power and keep blocks within 10 LU of that level.
            var ungatedMean = absGated.Average();
            var relativeGateLinear = ungatedMean * Math.Pow(10.0, -1.0); // −10 LU = /10 in linear power
            var relGated = absGated.Where(b => b >= relativeGateLinear).ToList();
            if (relGated.Count == 0)
                return DefaultSilenceDbFloor;

            var integrated = relGated.Average();
            return (float)(-0.691 + 10.0 * Math.Log10(integrated));
        }

        private static KWeightingCoefficients ComputeKWeightingCoefficients(int sampleRate)
        {
            // Derive BS.1770 K-weighting filters from the analogue prototype with a
            // bilinear transform so coefficients scale correctly with sample rate.
            var sampleRateHz = (double)sampleRate;

            // High-shelf pre-filter (+4 dB around 1.68 kHz)
            var preFilterShelfGain = 1.58489319246111;
            var preFilterBandGain = 1.25849557432540;
            var preFilterAngularFrequency = 2.0 * Math.PI * 1681.974450955533;
            var preFilterQuality = 0.7071752369554196;

            var preFilterPreWarp = Math.Tan(preFilterAngularFrequency / (2.0 * sampleRateHz));
            var preFilterPreWarpSquared = preFilterPreWarp * preFilterPreWarp;
            var preFilterDenominator = 1.0 + preFilterPreWarp / preFilterQuality + preFilterPreWarpSquared;

            var preFilter = new BiquadCoefficients(
                (preFilterShelfGain + preFilterBandGain * preFilterPreWarp / preFilterQuality + preFilterPreWarpSquared) / preFilterDenominator,
                2.0 * (preFilterPreWarpSquared - preFilterShelfGain) / preFilterDenominator,
                (preFilterShelfGain - preFilterBandGain * preFilterPreWarp / preFilterQuality + preFilterPreWarpSquared) / preFilterDenominator,
                2.0 * (preFilterPreWarpSquared - 1.0) / preFilterDenominator,
                (1.0 - preFilterPreWarp / preFilterQuality + preFilterPreWarpSquared) / preFilterDenominator);

            // RLB high-pass at 38.1 Hz (second-order)
            var rlbAngularFrequency = 2.0 * Math.PI * 38.13547087602444;
            var rlbQuality = 0.5003270373238773;

            var rlbPreWarp = Math.Tan(rlbAngularFrequency / (2.0 * sampleRateHz));
            var rlbPreWarpSquared = rlbPreWarp * rlbPreWarp;
            var rlbDenominator = 1.0 + rlbPreWarp / rlbQuality + rlbPreWarpSquared;

            var rlbFilter = new BiquadCoefficients(
                1.0 / rlbDenominator,
                -2.0 / rlbDenominator,
                1.0 / rlbDenominator,
                2.0 * (rlbPreWarpSquared - 1.0) / rlbDenominator,
                (1.0 - rlbPreWarp / rlbQuality + rlbPreWarpSquared) / rlbDenominator);

            return new KWeightingCoefficients(preFilter, rlbFilter);
        }

        private static float[] ApplyBiquadCascade(float[] input, BiquadCoefficients firstStage, BiquadCoefficients secondStage)
        {
            var sampleCount = input.Length;
            var firstStageOutput = new double[sampleCount];
            double firstStageDelay1 = 0;
            double firstStageDelay2 = 0;
            for (var i = 0; i < sampleCount; i++)
            {
                var inputSample = input[i];
                var firstStageSample = firstStage.B0 * inputSample + firstStageDelay1;
                firstStageDelay1 = firstStage.B1 * inputSample - firstStage.A1 * firstStageSample + firstStageDelay2;
                firstStageDelay2 = firstStage.B2 * inputSample - firstStage.A2 * firstStageSample;
                firstStageOutput[i] = firstStageSample;
            }

            var output = new float[sampleCount];
            double secondStageDelay1 = 0;
            double secondStageDelay2 = 0;
            for (var i = 0; i < sampleCount; i++)
            {
                var firstStageSample = firstStageOutput[i];
                var secondStageSample = secondStage.B0 * firstStageSample + secondStageDelay1;
                secondStageDelay1 = secondStage.B1 * firstStageSample - secondStage.A1 * secondStageSample + secondStageDelay2;
                secondStageDelay2 = secondStage.B2 * firstStageSample - secondStage.A2 * secondStageSample;
                output[i] = (float)secondStageSample;
            }
            return output;
        }

        private static double[] GetBS1770ChannelWeights(int channels)
        {
            // Mono
            if (channels == 1)
                return [1.0];

            // Stereo (L, R)
            if (channels == 2)
                return [1.0, 1.0];

            // 5.1 Surround (L, R, C, LFE, Ls, Rs)
            if (channels == 6)
                return [1.0, 1.0, 1.0, 0.0, 1.41, 1.41];

            // 7.1 Surround (L, R, C, LFE, Ls, Rs, Lrs, Rrs)
            if (channels == 8)
                return [1.0, 1.0, 1.0, 0.0, 1.41, 1.41, 1.41, 1.41];

            return Enumerable.Repeat(1.0, channels).ToArray();
        }

        private static float[] BuildMonoMix(float[][] perChannelSamples)
        {
            if (perChannelSamples.Length == 0)
                return [];

            var sampleCount = perChannelSamples[0].Length;
            var channelCount = perChannelSamples.Length;
            var mono = new float[sampleCount];

            for (var sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
            {
                var sum = 0.0f;
                for (var channelIndex = 0; channelIndex < channelCount; channelIndex++)
                    sum += perChannelSamples[channelIndex][sampleIndex];
                mono[sampleIndex] = sum / channelCount;
            }

            return mono;
        }

        private static float ComputeRms(float[] samples, int startIndex, int count)
        {
            if (count <= 0)
                return 0.0f;

            var sumSquares = 0.0;
            for (var i = 0; i < count; i++)
            {
                var sample = samples[startIndex + i];
                sumSquares += sample * sample;
            }

            return (float)Math.Sqrt(sumSquares / count);
        }

        private static float LinearToDb(float linear)
        {
            if (linear <= 1e-12f)
                return DefaultSilenceDbFloor;
            return 20.0f * MathF.Log10(linear);
        }

        private static ushort QuantiseEnvelopeDb(float db)
        {
            var clampedDb = Math.Clamp(db, DefaultSilenceDbFloor, 0.0f);
            var attenuationDb = -clampedDb;
            var normalised = attenuationDb / -DefaultSilenceDbFloor;
            var quantised = (int)MathF.Round(normalised * ushort.MaxValue);
            return (ushort)Math.Clamp(quantised, 0, ushort.MaxValue);
        }

        private WemVorbisSetupData CompressSetup(byte[] standardSetupBytes, int channels)
        {
            var vorbisAsciiTagByteCount = 6;
            var wwiseCodebookIdBitCount = 10;
            var vorbisTimeDomainTransformTypeBitWidth = 16;
            var input = new BitChunk(standardSetupBytes);
            var output = new BitWriter(Math.Max(standardSetupBytes.Length / 4, 64));

            input.ReadBits(8);
            for (var headerIndex = 0; headerIndex < vorbisAsciiTagByteCount; headerIndex++)
                input.ReadBits(BitHelper.BitsPerByte);

            var codebookCountField = input.ReadBits(8);
            output.WriteBits(codebookCountField, 8);
            var codebookCount = (int)codebookCountField + 1;
            for (var codebookIndex = 0; codebookIndex < codebookCount; codebookIndex++)
            {
                var startBit = input.BitPosition;
                SkipStandardVorbisCodebook(input);
                var endBit = input.BitPosition;
                var bitCount = endBit - startBit;

                var codebookBits = input.ReadBitRangeAsBytes(startBit, bitCount);
                var libraryId = _codebookLibrary.FindLibraryId(codebookBits, bitCount);
                output.WriteBits((uint)libraryId, wwiseCodebookIdBitCount);
            }

            var timeCountField = input.ReadBits(6);
            var timeCount = (int)timeCountField + 1;
            for (var timeIndex = 0; timeIndex < timeCount; timeIndex++)
                input.ReadBits(vorbisTimeDomainTransformTypeBitWidth);

            WwiseVorbisBitstreamTranscriber.TranscribeFloorConfigurationsVorbisToWwise(input, output);
            WwiseVorbisBitstreamTranscriber.TranscribeResidueConfigurationsVorbisToWwise(input, output);
            WwiseVorbisBitstreamTranscriber.TranscribeMappingConfigurationsVorbisToWwise(input, output, channels);

            var modeCountField = input.ReadBits(6);
            output.WriteBits(modeCountField, 6);
            var modeCount = (int)modeCountField + 1;
            var modeBlockSizes = new bool[modeCount];
            for (var modeIndex = 0; modeIndex < modeCount; modeIndex++)
            {
                var blockSizeBit = input.ReadBits(1);
                output.WriteBits(blockSizeBit, 1);
                modeBlockSizes[modeIndex] = blockSizeBit != 0;

                var windowType = input.ReadBits(16);
                var transformType = input.ReadBits(16);
                if (windowType != 0 || transformType != 0)
                    throw new InvalidDataException("Wwise Vorbis only supports window/transform type 0; encoder emitted non-zero value.");

                output.WriteBits(input.ReadBits(8), 8);
            }

            return new WemVorbisSetupData
            {
                SetupPacketBytes = output.ToArray(),
                ModeConfiguration = new VorbisModeConfiguration
                {
                    ModeBits = WwiseVorbisBitstreamTranscriber.IntegerLog(modeCount - 1),
                    ModeBlockSizes = modeBlockSizes,
                },
            };
        }

        private static byte[] CompressAudioPacket(byte[] standardPacket, VorbisModeConfiguration modeInfo, ref bool previousPacketIsLargeBlock)
        {
            var audioPacketHeaderBitsConsumed = 1;
            var terminalBitsForLongBlock = 2;
            if (standardPacket.Length == 0)
                return [];

            if (modeInfo.ModeBits == 0)
            {
                previousPacketIsLargeBlock = modeInfo.ModeBlockSizes.Length > 0 && modeInfo.ModeBlockSizes[0];
                return standardPacket;
            }

            var input = new BitChunk(standardPacket);
            var output = new BitWriter(standardPacket.Length);
            var totalInputBits = standardPacket.Length * BitHelper.BitsPerByte;
            var bitsConsumed = audioPacketHeaderBitsConsumed;

            input.ReadBits(1);

            uint modeNumber = 0;
            if (modeInfo.ModeBits > 0)
            {
                modeNumber = input.ReadBits(modeInfo.ModeBits);
                bitsConsumed += modeInfo.ModeBits;
            }

            output.WriteBits(modeNumber, modeInfo.ModeBits);
            var currentPacketIsLargeBlock = modeNumber < modeInfo.ModeBlockSizes.Length && modeInfo.ModeBlockSizes[modeNumber];
            if (currentPacketIsLargeBlock)
            {
                input.ReadBits(1);
                input.ReadBits(1);
                bitsConsumed += terminalBitsForLongBlock;
            }

            var remainingBits = totalInputBits - bitsConsumed;
            while (remainingBits >= BitHelper.BitsPerByte)
            {
                output.WriteBits(input.ReadBits(BitHelper.BitsPerByte), BitHelper.BitsPerByte);
                remainingBits -= BitHelper.BitsPerByte;
            }

            if (remainingBits > 0)
                output.WriteBits(input.ReadBits(remainingBits), remainingBits);

            output.AlignToByte();
            previousPacketIsLargeBlock = currentPacketIsLargeBlock;
            return output.ToArray();
        }

        private static List<WemSeekTableRecord> BuildSeekTable(List<WemAudioPacket> audioPackets, int largeBlockSize, int smallBlockSize, int totalSamples, int lastGranuleExtra, WemVorbisSetupData setupData)
        {
            var coupleGranuleDivisor = 4;
            var seekEntries = new List<WemSeekTableRecord>();
            if (audioPackets.Count == 0)
                return seekEntries;

            var modeInfo = setupData.ModeConfiguration;
            var modeMask = modeInfo.ModeBits > 0 ? ((1 << modeInfo.ModeBits) - 1) : 0;
            var seekInterval = largeBlockSize / 2;
            var targetTotalGranule = totalSamples + lastGranuleExtra;
            var granuleDeltas = new int[audioPackets.Count];
            var previousBlockSize = 0;

            for (var packetIndex = 0; packetIndex < audioPackets.Count; packetIndex++)
            {
                var packetData = audioPackets[packetIndex].Data;
                var modeNumber = packetData.Length > 0 && modeInfo.ModeBits > 0 ? packetData[0] & modeMask : 0;
                var currentBlockSize = modeNumber < modeInfo.ModeBlockSizes.Length && modeInfo.ModeBlockSizes[modeNumber] ? largeBlockSize : smallBlockSize;
                granuleDeltas[packetIndex] = packetIndex == 0 ? 0 : (previousBlockSize + currentBlockSize) / coupleGranuleDivisor;
                previousBlockSize = currentBlockSize;
            }

            var pendingGranule = 0;
            var pendingBytes = sizeof(ushort) + setupData.SetupPacketBytes.Length;
            var emittedGranuleTotal = 0;

            for (var packetIndex = 0; packetIndex < audioPackets.Count; packetIndex++)
            {
                pendingGranule += granuleDeltas[packetIndex];
                pendingBytes += WemAudioPacket.LengthPrefixSize + audioPackets[packetIndex].Data.Length;

                var isLastPacket = packetIndex == audioPackets.Count - 1;
                if (pendingGranule >= seekInterval || isLastPacket)
                {
                    emittedGranuleTotal += pendingGranule;
                    if (isLastPacket && emittedGranuleTotal != targetTotalGranule)
                        pendingGranule -= emittedGranuleTotal - targetTotalGranule;

                    seekEntries.Add(new WemSeekTableRecord { GranuleDelta = (ushort)pendingGranule, ByteCount = (ushort)pendingBytes });
                    pendingGranule = 0;
                    pendingBytes = 0;
                }
            }

            return seekEntries;
        }

        private static void SkipStandardVorbisCodebook(BitChunk input)
        {
            var sync = input.ReadBits(VorbisCodebook.SyncPatternBitWidth);
            if (sync != VorbisCodebook.SyncPattern)
                throw new InvalidDataException($"Standard Vorbis codebook sync pattern was 0x{sync:X6} (expected 0x{VorbisCodebook.SyncPattern:X6}).");

            var dimensions = (int)input.ReadBits(VorbisCodebook.DimensionsBitWidth);
            var entries = (int)input.ReadBits(VorbisCodebook.EntriesBitWidth);
            var ordered = input.ReadBits(1);
            if (ordered != 0)
            {
                input.ReadBits(5);
                var currentEntry = 0;
                while (currentEntry < entries)
                {
                    var bits = WwiseVorbisBitstreamTranscriber.IntegerLog(entries - currentEntry);
                    var number = (int)input.ReadBits(bits);
                    currentEntry += number;
                }

                if (currentEntry > entries)
                    throw new InvalidDataException("Standard Vorbis codebook entry count overflowed.");
            }
            else
            {
                var sparse = input.ReadBits(1);
                for (var entryIndex = 0; entryIndex < entries; entryIndex++)
                {
                    var present = true;
                    if (sparse != 0)
                        present = input.ReadBits(1) != 0;
                    if (present)
                        input.ReadBits(5);
                }
            }

            var lookupType = input.ReadBits(4);
            if (lookupType == 0)
                return;

            if (lookupType != 1)
                throw new InvalidDataException($"Standard Vorbis codebook uses unsupported lookup type {lookupType}.");

            input.ReadBits(VorbisCodebook.VqFloatFieldBitWidth);
            input.ReadBits(VorbisCodebook.VqFloatFieldBitWidth);
            var valueLength = (int)input.ReadBits(VorbisCodebook.ValueLengthBitWidth);
            input.ReadBits(1);

            var quantValueCount = WwiseVorbisBitstreamTranscriber.ComputeMapType1QuantValues(entries, dimensions);
            for (var i = 0; i < quantValueCount; i++)
                input.ReadBits(valueLength + 1);
        }

        private static float[][] ConvertPcmToPerChannelFloat(PcmAudio audio, ushort formatTag)
        {
            const ushort IeeeFloatFormatTag = 3;
            const int Pcm8BitDepth = 8;
            const int Pcm16BitDepth = 16;
            const int Pcm24BitDepth = 24;
            const int Pcm32BitDepth = 32;
            const float Pcm16BitNormalizationDivisor = 32768.0f;
            const float Pcm24BitNormalizationDivisor = 8388608.0f;
            const float Pcm32BitNormalizationDivisor = 2147483648.0f;
            const int Pcm8BitMidpoint = 128;
            const float Pcm8BitNormalizationDivisor = 128.0f;

            var channels = audio.Channels;
            var sampleCount = audio.SampleCount;
            var bytesPerSample = audio.BitsPerSample / BitHelper.BitsPerByte;

            var perChannelSamples = new float[channels][];
            for (var channelIndex = 0; channelIndex < channels; channelIndex++)
                perChannelSamples[channelIndex] = new float[sampleCount];

            for (var sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
            {
                for (var channelIndex = 0; channelIndex < channels; channelIndex++)
                {
                    var byteOffset = (sampleIndex * channels + channelIndex) * bytesPerSample;
                    float rawSample;

                    if (audio.BitsPerSample == Pcm16BitDepth)
                        rawSample = (short)(audio.Data[byteOffset] | (audio.Data[byteOffset + 1] << BitHelper.BitsPerByte)) / Pcm16BitNormalizationDivisor;
                    else if (audio.BitsPerSample == Pcm8BitDepth)
                        rawSample = (audio.Data[byteOffset] - Pcm8BitMidpoint) / Pcm8BitNormalizationDivisor;
                    else if (audio.BitsPerSample == Pcm24BitDepth)
                    {
                        var sample24 = audio.Data[byteOffset] | (audio.Data[byteOffset + 1] << BitHelper.BitsPerByte) | (audio.Data[byteOffset + 2] << 16);
                        if ((sample24 & 0x800000) != 0)
                            sample24 |= unchecked((int)0xFF000000);

                        rawSample = sample24 / Pcm24BitNormalizationDivisor;
                    }
                    else if (audio.BitsPerSample == Pcm32BitDepth)
                    {
                        if (formatTag == IeeeFloatFormatTag)
                            rawSample = BitConverter.ToSingle(audio.Data, byteOffset);
                        else
                            rawSample = BitConverter.ToInt32(audio.Data, byteOffset) / Pcm32BitNormalizationDivisor;
                    }
                    else
                        throw new InvalidDataException($"Unsupported WAV bits per sample: {audio.BitsPerSample}. Supported formats are 8-bit, 16-bit, 24-bit, and 32-bit PCM, plus 32-bit IEEE float.");

                    perChannelSamples[channelIndex][sampleIndex] = rawSample;
                }
            }

            return perChannelSamples;
        }
    }
}
