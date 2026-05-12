using System;
using System.Linq;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Editors.Audio.Shared.Storage;
using Editors.Audio.Shared.Wwise.HircExploration;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.Audio.Codecs;
using Shared.GameFormats.Audio.Wav;
using Shared.GameFormats.Video;
using Shared.GameFormats.Wwise;
using Shared.GameFormats.Wwise.Hirc;
using Shared.GameFormats.Wwise.Wem.V132;
using Shared.GameFormats.Wwise.Wem.V132.Decoding;
using Shared.GameFormats.Wwise.Wem.V132.Encoding;

namespace Editors.Audio.Shared.Utilities
{
    public static class CAVp8Exporter
    {
        public static byte[] ExportToWebM(PackFile packFile, IPackFileService packFileService, IAudioRepository audioRepository)
        {
            var caVp8File = new CAVp8File(packFile.DataSource.ReadData());
            var vorbisAudio = GetVorbisAudio(packFile, packFileService, audioRepository);

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

        public static byte[] ExportToWav(PackFile packFile, IPackFileService packFileService, IAudioRepository audioRepository)
        {
            var vorbisAudio = GetVorbisAudio(packFile, packFileService, audioRepository);
            var pcmAudio = vorbisAudio.ToPcm();
            var wavFile = new WavFile { Audio = pcmAudio };
            return wavFile.WriteData();
        }

        public static byte[] ExportToWem(PackFile packFile, IPackFileService packFileService, IAudioRepository audioRepository)
        {
            var wavBytes = ExportToWav(packFile, packFileService, audioRepository);
            var wemFile = WemFile.CreateFromWavBytes(wavBytes);
            return wemFile.WriteData();
        }

        public static byte[] ExportToIvf(PackFile packFile)
        {
            var caVp8File = new CAVp8File(packFile.DataSource.ReadData());
            var ivfFile = new IvfFile(caVp8File);
            return ivfFile.WriteData();
        }

        private static VorbisAudio GetVorbisAudio(PackFile packFile, IPackFileService packFileService, IAudioRepository audioRepository)
        {
            var wemBytes = GetWem(packFile, packFileService, audioRepository);
            var wemFile = WemFile.CreateFromBytes(wemBytes);
            var codebookLibrary = new WwiseCodebookLibrary();
            var decoder = new WemVorbisDecoder(codebookLibrary);
            return decoder.Decode(wemFile);
        }

        private static byte[] GetWem(PackFile packFile, IPackFileService packFileService, IAudioRepository audioRepository)
        {
            audioRepository.Load(Wh3LanguageInformation.GetAllLanguages());

            var movieFilePath = packFileService.GetFullPath(packFile);
            var actionEventName = Wh3ActionEventInformation.GetMovieActionEventName(movieFilePath);
            var actionEventId = WwiseHash.Compute(actionEventName);

            var actionEventHircs = audioRepository.GetHircs(actionEventId);
            if (actionEventHircs.Count == 0)
                throw new Exception($"Cannot find Action Event: {actionEventName}.");

            var hircTreeChildrenParser = new HircTreeChildrenParser(audioRepository);
            var nodes = hircTreeChildrenParser.BuildHierarchyAsFlatList(actionEventHircs.First());
            
            var soundNode = nodes.FirstOrDefault(node => node.Hirc is ICAkSound);
            if (soundNode == null)
                throw new Exception($"Cannot find a Sound node for Action Event: {actionEventName}.");

            var sourceId = ((ICAkSound)soundNode.Hirc).GetSourceId();
            var wemFile = audioRepository.FindWem(sourceId.ToString());
            if (wemFile == null)
                throw new Exception($"Cannot find {sourceId}.wem");

            return wemFile.DataSource.ReadData();
        }
    }
}
