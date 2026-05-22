using Shared.GameFormats.Wwise.Wem.V132;
using Shared.GameFormats.Wwise.Wem.V132.Decoding;
using Shared.GameFormats.Wwise.Wem.V132.Encoding;

namespace Shared.GameFormats.Audio.Codecs.Vorbis
{
    public class VorbisAudio
    {
        public byte Channels { get; set; }
        public byte[] VorbisCodecPrivateData { get; set; } = [];
        public VorbisIdentificationPacket IdentificationHeader { get; set; } = new();
        public VorbisCommentPacket CommentHeader { get; set; } = new();
        public VorbisSetupPacket SetupHeader { get; set; } = new();
        public List<VorbisAudioPacket> Packets { get; set; } = [];
        public int SampleCount { get; set; }
        public uint SampleRate { get; set; }

        public static VorbisAudio CreateFromWemBytes(byte[] wemBytes)
        {
            var wemFile = WemFile.CreateFromWemBytes(wemBytes);
            var codebookLibrary = new WwiseCodebookLibrary();
            var decoder = new WemVorbisDecoder(codebookLibrary);
            return decoder.Decode(wemFile);
        }
    }
}
