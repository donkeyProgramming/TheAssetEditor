using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.Audio.Codecs.Vorbis;
using Shared.GameFormats.Video;

namespace Editors.Audio.Shared.Utilities
{
    public static class CAVp8Exporter
    {
        public static byte[] ExportToWebM(PackFile caVp8PackFile, PackFile wemPackFile)
        {
            var vorbisAudio = VorbisAudio.CreateFromWemBytes(wemPackFile.DataSource.ReadData());
            var caVp8File = new CAVp8File(caVp8PackFile.DataSource.ReadData());
            var webMFile = new WebMFile
            {
                Width = caVp8File.Width,
                Height = caVp8File.Height,
                Framerate = caVp8File.Framerate,
                FrameTable = caVp8File.FrameTable,
                FrameData = caVp8File.FrameData,
                VorbisCodecPrivate = vorbisAudio.VorbisCodecPrivateData,
                VorbisAudioPackets = vorbisAudio.Packets,
                VorbisSampleRate = checked((int)vorbisAudio.SampleRate),
                VorbisChannels = vorbisAudio.Channels,
            };
            return webMFile.WriteData();
        }

        public static byte[] ExportToIvf(PackFile caVp8PackFile)
        {
            var caVp8File = new CAVp8File(caVp8PackFile.DataSource.ReadData());
            var ivfFile = new IvfFile(caVp8File);
            return ivfFile.WriteData();
        }
    }
}
