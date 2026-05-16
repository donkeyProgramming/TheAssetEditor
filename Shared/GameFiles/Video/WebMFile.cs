using System.Text;
using Shared.GameFormats.Audio.Codecs.Vorbis;

namespace Shared.GameFormats.Video
{
    public class WebMFile
    {
        private const uint IdTracks = 0x1654AE6B;
        private const uint IdTrackEntry = 0xAE;
        private const uint IdTrackNumber = 0xD7;
        private const uint IdTrackUid = 0x73C5;
        private const uint IdTrackType = 0x83;
        private const uint IdFlagLacing = 0x9C;
        private const uint IdCodecId = 0x86;
        private const uint TrackNumberVideo = 1;
        private const uint TrackNumberAudio = 2;
        private const byte VintMarkerOneByte   = 0x80; 
        private const byte VintMarkerEightByte = 0x01;

        public ushort Width { get; set; }
        public ushort Height { get; set; }
        public float Framerate { get; set; }
        public List<FrameTableRecord> FrameTable { get; set; } = [];
        public byte[] FrameData { get; set; } = [];
        public byte[]? VorbisCodecPrivate { get; set; }
        public List<VorbisAudioPacket> VorbisAudioPackets { get; set; } = [];
        public int VorbisSampleRate { get; set; }
        public int VorbisChannels { get; set; }

        public byte[] WriteData()
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
    
            writer.Write(BuildEbmlHeader());
            var idSegment = 0x18538067U;
            WriteId(writer, idSegment);
            // We use unknown size because total length is not known upfront
            WriteUnknownSize(writer);
            writer.Write(BuildInfo());
            writer.Write(BuildTracks());
            WriteClusters(writer);
            return stream.ToArray();
        }

        private static byte[] BuildEbmlHeader()
        {
            var idEbml = 0x1A45DFA3U;
            var idEbmlVersion = 0x4286U;
            var idEbmlReadVersion = 0x42F7U;
            var idEbmlMaxIdLength = 0x42F2U;
            var idEbmlMaxSizeLength = 0x42F3U;
            var idDocType = 0x4282U;
            var idDocTypeVersion = 0x4287U;
            var idDocTypeReadVersion = 0x4285U;

            var ebmlVersion = UintElement(idEbmlVersion, 1);
            var ebmlReadVersion = UintElement(idEbmlReadVersion, 1);
            var ebmlMaxIdLength = UintElement(idEbmlMaxIdLength, 4);
            var ebmlMaxSizeLength = UintElement(idEbmlMaxSizeLength, 8);
            var docType = StringElement(idDocType, "webm");
            var docTypeVersion = UintElement(idDocTypeVersion, 4);
            var docTypeReadVersion = UintElement(idDocTypeReadVersion, 2);

            var content = Concat(
                ebmlVersion,
                ebmlReadVersion,
                ebmlMaxIdLength,
                ebmlMaxSizeLength,
                docType,
                docTypeVersion,
                docTypeReadVersion
            );

            return Element(idEbml, content);
        }

        private byte[] BuildInfo()
        {
            var idInfo = 0x1549A966U;
            var idTimestampScale = 0x2AD7B1U;
            var idMuxingApp = 0x4D80U;
            var idWritingApp = 0x5741U;
            var idDuration = 0x4489U;
            var durationMs = FrameTable.Count / (double)Framerate * 1000.0;
            var timestampScaleNanoseconds = 1_000_000UL;

            var timestampScale = UintElement(idTimestampScale, timestampScaleNanoseconds);
            var muxingApp = StringElement(idMuxingApp, "AssetEditor");
            var writingApp = StringElement(idWritingApp, "AssetEditor");
            var duration = FloatElement(idDuration, durationMs);

            var content = Concat(
                timestampScale,
                muxingApp,
                writingApp,
                duration
            );

            return Element(idInfo, content);
        }

        private byte[] BuildTracks()
        {
            var videoEntry = BuildVideoTrackEntry();

            var hasVorbisAudio = VorbisCodecPrivate != null && VorbisAudioPackets.Count > 0;
            if (!hasVorbisAudio)
                return Element(IdTracks, videoEntry);

            var audioEntry = BuildAudioTrackEntry();
            return Element(IdTracks, Concat(videoEntry, audioEntry));
        }

        private byte[] BuildVideoTrackEntry()
        {
            var idVideo = 0xE0U;
            var idPixelWidth = 0xB0U;
            var idPixelHeight = 0xBAU;

            var trackNumber = UintElement(IdTrackNumber, TrackNumberVideo);
            var trackUid = UintElement(IdTrackUid, TrackNumberVideo);
            var trackType = UintElement(IdTrackType, 1);
            var flagLacing = UintElement(IdFlagLacing, 0);
            var codecId = StringElement(IdCodecId, "V_VP8");

            var pixelWidth = UintElement(idPixelWidth, Width);
            var pixelHeight = UintElement(idPixelHeight, Height);
            var videoSettings = Element(idVideo, Concat(pixelWidth, pixelHeight));

            var content = Concat(
                trackNumber,
                trackUid,
                trackType,
                flagLacing,
                codecId,
                videoSettings
            );

            return Element(IdTrackEntry, content);
        }

        private byte[] BuildAudioTrackEntry()
        {
            var idCodecPrivate = 0x63A2U;
            var idAudio = 0xE1U;
            var idSamplingFrequency = 0xB5U;
            var idChannels = 0x9FU;

            var trackNumber = UintElement(IdTrackNumber, TrackNumberAudio);
            var trackUid = UintElement(IdTrackUid, TrackNumberAudio);
            var trackType = UintElement(IdTrackType, 2);
            var flagLacing = UintElement(IdFlagLacing, 0);
            var codecId = StringElement(IdCodecId, "A_VORBIS");
            var codecPrivate = Element(idCodecPrivate, VorbisCodecPrivate!);

            var samplingFrequency = FloatElement(idSamplingFrequency, VorbisSampleRate);
            var channels = UintElement(idChannels, (ulong)VorbisChannels);
            var audioSettings = Element(idAudio, Concat(samplingFrequency, channels));

            var content = Concat(
                trackNumber,
                trackUid,
                trackType,
                flagLacing,
                codecId,
                codecPrivate,
                audioSettings
            );

            return Element(IdTrackEntry, content);
        }

        private void WriteClusters(BinaryWriter writer)
        {
            if (FrameTable.Count == 0)
                return;

            var idCluster = 0x1F43B675U;
            var idTimestamp = 0xE7U;
            var maxClusterDurationMs = 30000;
            var msPerVideoFrame = 1000.0 / Framerate;
            var videoDataOffset = 0;
            var videoIndex = 0;
            var audioIndex = 0;

            while (videoIndex < FrameTable.Count)
            {
                using var clusterContent = new MemoryStream();
                using var clusterWriter = new BinaryWriter(clusterContent);

                var clusterTimestampMs = (long)(videoIndex * msPerVideoFrame);
                clusterWriter.Write(UintElement(idTimestamp, (ulong)clusterTimestampMs));

                var blocks = new List<ClusterBlock>();
                var clusterVideoStart = videoIndex;

                while (videoIndex < FrameTable.Count)
                {
                    var frameTimestampMs = (long)(videoIndex * msPerVideoFrame);
                    var relativeMs = frameTimestampMs - clusterTimestampMs;

                    var isKeyFrame = FrameTable[videoIndex].IsKeyFrame;
                    if (videoIndex > clusterVideoStart && isKeyFrame && relativeMs > maxClusterDurationMs)
                        break;

                    var frame = FrameTable[videoIndex];
                    var frameData = FrameData[videoDataOffset..(videoDataOffset + (int)frame.Size)];
                    videoDataOffset += (int)frame.Size;

                    blocks.Add(new ClusterBlock(frameTimestampMs, BuildSimpleBlock(frameData, (short)relativeMs, isKeyFrame, trackNumber: (int)TrackNumberVideo)));
                    videoIndex++;
                }

                var clusterEndMs = long.MaxValue;
                if (videoIndex < FrameTable.Count)
                    clusterEndMs = (long)(videoIndex * msPerVideoFrame);

                while (audioIndex < VorbisAudioPackets.Count && VorbisAudioPackets[audioIndex].TimestampMilliseconds < clusterEndMs)
                {
                    var audioPacket = VorbisAudioPackets[audioIndex];
                    var audioTimestampMs = audioPacket.TimestampMilliseconds;
                    var relativeMs = (short)Math.Clamp(audioTimestampMs - clusterTimestampMs, short.MinValue, short.MaxValue);
                    blocks.Add(new ClusterBlock(audioTimestampMs, BuildSimpleBlock(audioPacket.Data, relativeMs, isKeyFrame: false, trackNumber: (int)TrackNumberAudio)));
                    audioIndex++;
                }

                foreach (var block in blocks.OrderBy(block => block.TimestampMs))
                    clusterWriter.Write(block.Data);

                writer.Write(Element(idCluster, clusterContent.ToArray()));
            }
        }

        private static byte[] BuildSimpleBlock(byte[] frameData, short relativeTimestampMs, bool isKeyFrame, int trackNumber)
        {
            var idSimpleBlock = 0xA3U;
            byte simpleBlockKeyframeFlag = 0x80;
            byte simpleBlockNoFlags = 0x00;

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            
            writer.Write((byte)(VintMarkerOneByte | trackNumber));
            writer.Write((byte)((relativeTimestampMs >> 8) & 0xFF));
            writer.Write((byte)(relativeTimestampMs & 0xFF));

            if (isKeyFrame)
                writer.Write(simpleBlockKeyframeFlag);
            else
                writer.Write(simpleBlockNoFlags);

            writer.Write(frameData);

            return Element(idSimpleBlock, stream.ToArray());
        }

        private static byte[] Element(uint id, byte[] content)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            WriteId(writer, id);
            WriteVint(writer, (ulong)content.Length);
            writer.Write(content);
            return stream.ToArray();
        }

        private static byte[] UintElement(uint id, ulong value)
        {
            var numBytes = 1;
            var shifted = value >> 8;
            while (shifted > 0) { shifted >>= 8; numBytes++; }

            var content = new byte[numBytes];
            var remaining = value;
            for (var i = numBytes - 1; i >= 0; i--)
            {
                content[i] = (byte)(remaining & 0xFF);
                remaining >>= 8;
            }
            return Element(id, content);
        }

        private static byte[] StringElement(uint id, string value) => Element(id, Encoding.ASCII.GetBytes(value));

        private static byte[] FloatElement(uint id, double value)
        {
            var bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return Element(id, bytes);
        }

        private static byte[] Concat(params byte[][] arrays)
        {
            var result = new byte[arrays.Sum(a => a.Length)];
            var offset = 0;
            foreach (var array in arrays)
            {
                Buffer.BlockCopy(array, 0, result, offset, array.Length);
                offset += array.Length;
            }
            return result;
        }

        private static void WriteId(BinaryWriter writer, uint id)
        {
            var idMaxOneByte = 0xFFU;
            var idMaxTwoByte = 0xFFFFU;
            var idMaxThreeByte = 0xFFFFFFU;

            if (id <= idMaxOneByte)
                writer.Write((byte)id);
            else if (id <= idMaxTwoByte)
            {
                writer.Write((byte)(id >> 8));
                writer.Write((byte)(id & 0xFF));
            }
            else if (id <= idMaxThreeByte)
            {
                writer.Write((byte)(id >> 16));
                writer.Write((byte)((id >> 8) & 0xFF));
                writer.Write((byte)(id & 0xFF));
            }
            else
            {
                writer.Write((byte)(id >> 24));
                writer.Write((byte)((id >> 16) & 0xFF));
                writer.Write((byte)((id >> 8) & 0xFF));
                writer.Write((byte)(id & 0xFF));
            }
        }

        private static void WriteVint(BinaryWriter writer, ulong value)
        {
            var vintMaxOneByte = 0x7EUL;
            var vintMaxTwoByte = 0x3FFEUL;
            var vintMaxThreeByte = 0x1FFFFEUL;
            var vintMaxFourByte = 0x0FFFFFFEUL;
            var vintMarkerTwoByte = 0x40UL;
            var vintMarkerThreeByte = 0x20UL;
            var vintMarkerFourByte = 0x10UL;

            if (value <= vintMaxOneByte)
                writer.Write((byte)(VintMarkerOneByte | value));
            else if (value <= vintMaxTwoByte)
            {
                writer.Write((byte)(vintMarkerTwoByte | (value >> 8)));
                writer.Write((byte)(value & 0xFF));
            }
            else if (value <= vintMaxThreeByte)
            {
                writer.Write((byte)(vintMarkerThreeByte | (value >> 16)));
                writer.Write((byte)((value >> 8) & 0xFF));
                writer.Write((byte)(value & 0xFF));
            }
            else if (value <= vintMaxFourByte)
            {
                writer.Write((byte)(vintMarkerFourByte | (value >> 24)));
                writer.Write((byte)((value >> 16) & 0xFF));
                writer.Write((byte)((value >> 8) & 0xFF));
                writer.Write((byte)(value & 0xFF));
            }
            else
            {
                writer.Write(VintMarkerEightByte);
                for (var i = 6; i >= 0; i--)
                    writer.Write((byte)((value >> (i * 8)) & 0xFF));
            }
        }

        private static void WriteUnknownSize(BinaryWriter writer)
        {
            writer.Write(VintMarkerEightByte);
            for (var i = 0; i < 7; i++)
                writer.Write((byte)0xFF);
        }
    }
}
