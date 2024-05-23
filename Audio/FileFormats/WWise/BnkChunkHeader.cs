using SharedCore.ByteParsing;
using System;
using System.IO;
using System.Text;

namespace Audio.FileFormats.WWise
{
    public class BnkChunkHeader
    {
        public static uint HeaderByteSize { get => 8; }
        public string Tag { get; set; }
        public uint ChunkSize { get; set; }

        public static BnkChunkHeader CreateFromBytes(ByteChunk chunck)
        {
            var instance = new BnkChunkHeader();
            instance.Tag = Encoding.UTF8.GetString(chunck.ReadBytes(4));
            instance.ChunkSize = chunck.ReadUInt32();
            return instance;
        }

        public static BnkChunkHeader PeakFromBytes(ByteChunk chunck)
        {
            var peakBytes = chunck.PeakChunk(8);
            return CreateFromBytes(peakBytes);
        }

        public static byte[] GetAsByteArray(BnkChunkHeader header)
        {
            if (header.Tag.Length != 4)
                throw new Exception($"Header not valid {header.Tag}");

            using var memStream = new MemoryStream();
            memStream.Write(ByteParsers.Byte.EncodeValue((byte)header.Tag[0], out _));
            memStream.Write(ByteParsers.Byte.EncodeValue((byte)header.Tag[1], out _));
            memStream.Write(ByteParsers.Byte.EncodeValue((byte)header.Tag[2], out _));
            memStream.Write(ByteParsers.Byte.EncodeValue((byte)header.Tag[3], out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(header.ChunkSize, out _));

            var byteArray = memStream.ToArray();

            // Reload the object to ensure sanity
            var reload = CreateFromBytes(new ByteChunk(byteArray));
            return byteArray;
        }
    }
}

//https://wiki.xentax.com/index.php/Wwise_SoundBank_(*.bnk)#DIDX_section
//https://github.com/bnnm/wwiser/blob/cd5c086ef2c104e7133e361d385a1023408fb92f/wwiser/wmodel.py#L205
//https://github.com/Maddoxkkm/bnk-event-extraction
//https://github.com/vgmstream/vgmstream/blob/37cc12295c92ec6aa874118fb237bd3821970836/src/meta/bkhd.c
//https://github.com/admiralnelson/total-warhammer-RE-audio/blob/master/BnkExtract.py
//https://github.com/eXpl0it3r/bnkextr