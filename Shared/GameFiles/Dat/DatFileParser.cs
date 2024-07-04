using System.Text;
using Shared.Core.ByteParsing;
using Shared.Core.PackFiles.Models;

namespace Shared.GameFormats.Dat
{
    public class DatFileParser
    {
        public static SoundDatFile Parse(PackFile file, bool isAtilla)
        {
            var chunk = file.DataSource.ReadDataAsChunk();

            var output = new SoundDatFile();

            var sectionZeroCount = chunk.ReadUInt32();
            for (var i = 0; i < sectionZeroCount; i++)
                output.Event0.Add(new SoundDatFile.EventWithValue() { EventName = ReadStr32(chunk), Value = chunk.ReadSingle() });

            var sectionOneCount = chunk.ReadInt32();
            for (var i = 0; i < sectionOneCount; i++)
            {
                var eventEnum = new SoundDatFile.EventEnums() { EnumName = ReadStr32(chunk) };
                var attrValCount = chunk.ReadUInt32();
                for (var j = 0; j < attrValCount; j++)
                    eventEnum.EnumValues.Add(ReadStr32(chunk));

                output.EnumGroup0.Add(eventEnum);
            }

            var sectionTwoCount = chunk.ReadInt32();
            for (var i = 0; i < sectionTwoCount; i++)
            {
                var eventEnum = new SoundDatFile.EventEnums() { EnumName = ReadStr32(chunk) };
                var attrValCount = chunk.ReadUInt32();
                for (var j = 0; j < attrValCount; j++)
                    eventEnum.EnumValues.Add(ReadStr32(chunk));

                output.EnumGroup1.Add(eventEnum);
            }

            var sectionThreeCount = chunk.ReadInt32();
            for (var i = 0; i < sectionThreeCount; i++)
            {
                var eventEnum = new SoundDatFile.EventWithValues() { EventName = ReadStr32(chunk) };
                var attrValCount = chunk.ReadUInt32();
                for (var j = 0; j < attrValCount; j++)
                    eventEnum.Values.Add(chunk.ReadUInt32());

                output.VoiceEvents.Add(eventEnum);
            }

            if (isAtilla)
            {
                var sectionFourCount = chunk.ReadInt32();
                for (var i = 0; i < sectionFourCount; i++)
                    output.SettingValues.Add(new SoundDatFile.SettingValue() { EventName = ReadStr32(chunk) });
            }
            else
            {
                var sectionFourCount = chunk.ReadInt32();
                for (var i = 0; i < sectionFourCount; i++)
                    output.SettingValues.Add(new SoundDatFile.SettingValue() { EventName = ReadStr32(chunk), MinValue = chunk.ReadSingle(), MaxValue = chunk.ReadSingle() });

                var sectionFiveCount = chunk.ReadInt32();
                for (var i = 0; i < sectionFiveCount; i++)
                    output.Unknown.Add(ReadStr32(chunk));
            }

            return output;
        }

        public static byte[] GetAsByteArray(SoundDatFile file)
        {
            using var memStream = new MemoryStream();

            memStream.Write(ByteParsers.UInt32.EncodeValue((uint)file.Event0.Count(), out _));
            foreach (var value in file.Event0)
            {
                memStream.Write(WriteStr32(value.EventName));
                memStream.Write(ByteParsers.Single.EncodeValue(value.Value, out _));
            }

            memStream.Write(ByteParsers.UInt32.EncodeValue((uint)file.EnumGroup0.Count(), out _));
            foreach (var enumType in file.EnumGroup0)
            {
                memStream.Write(WriteStr32(enumType.EnumName));
                memStream.Write(ByteParsers.UInt32.EncodeValue((uint)enumType.EnumValues.Count(), out _));

                foreach (var enumValue in enumType.EnumValues)
                    memStream.Write(WriteStr32(enumType.EnumName));
            }

            memStream.Write(ByteParsers.UInt32.EncodeValue((uint)file.EnumGroup1.Count(), out _));
            foreach (var enumType in file.EnumGroup1)
            {
                memStream.Write(WriteStr32(enumType.EnumName));
                memStream.Write(ByteParsers.UInt32.EncodeValue((uint)enumType.EnumValues.Count(), out _));

                foreach (var enumValue in enumType.EnumValues)
                    memStream.Write(WriteStr32(enumType.EnumName));
            }

            memStream.Write(ByteParsers.UInt32.EncodeValue((uint)file.VoiceEvents.Count(), out _));
            foreach (var voiceEvent in file.VoiceEvents)
            {
                memStream.Write(WriteStr32(voiceEvent.EventName));
                memStream.Write(ByteParsers.UInt32.EncodeValue((uint)voiceEvent.Values.Count(), out _));

                foreach (var value in voiceEvent.Values)
                    memStream.Write(ByteParsers.UInt32.EncodeValue(value, out _));
            }


            memStream.Write(ByteParsers.UInt32.EncodeValue((uint)file.SettingValues.Count(), out _));
            foreach (var value in file.SettingValues)
            {
                memStream.Write(WriteStr32(value.EventName));
                memStream.Write(ByteParsers.Single.EncodeValue(value.MinValue, out _));
                memStream.Write(ByteParsers.Single.EncodeValue(value.MaxValue, out _));
            }

            memStream.Write(ByteParsers.UInt32.EncodeValue((uint)file.Unknown.Count(), out _));
            foreach (var value in file.Unknown)
                memStream.Write(WriteStr32(value));

            return memStream.ToArray();
        }

        public static string ReadStr32(ByteChunk chunk)
        {
            var strLength = chunk.ReadInt32();
            var bytes = chunk.ReadBytes(strLength);
            return Encoding.UTF8.GetString(bytes);
        }

        static byte[] WriteStr32(string str)
        {
            var buffer = ByteParsers.Int32.EncodeValue(str.Length, out _).ToList();
            var strBytes = Encoding.UTF8.GetBytes(str);
            buffer.AddRange(strBytes);
            return buffer.ToArray();
        }
    }
}
