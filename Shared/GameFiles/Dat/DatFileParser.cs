using System.Text;
using Shared.Core.ByteParsing;
using Shared.Core.PackFiles.Models;

namespace Shared.GameFormats.Dat
{
    public class DatFileParser
    {
        public static SoundDatFile Parse(PackFile packFile, bool isAtilla)
        {
            var output = new SoundDatFile();
            var chunk = packFile.DataSource.ReadDataAsChunk();

            var sectionZeroCount = chunk.ReadUInt32();
            for (var i = 0; i < sectionZeroCount; i++)
                output.EventWithStateGroup.Add(new SoundDatFile.DatEventWithStateGroup() { Event = ReadStr32(chunk), Value = chunk.ReadSingle() });

            var sectionOneCount = chunk.ReadInt32();
            for (var i = 0; i < sectionOneCount; i++)
            {
                var stateGroup = new SoundDatFile.DatStateGroupsWithStates() { StateGroup = ReadStr32(chunk) };
                var state = chunk.ReadUInt32();
                for (var j = 0; j < state; j++)
                    stateGroup.States.Add(ReadStr32(chunk));

                output.StateGroupsWithStates0.Add(stateGroup);
            }

            var sectionTwoCount = chunk.ReadInt32();
            for (var i = 0; i < sectionTwoCount; i++)
            {
                var stateGroup = new SoundDatFile.DatStateGroupsWithStates() { StateGroup = ReadStr32(chunk) };
                var state = chunk.ReadUInt32();
                for (var j = 0; j < state; j++)
                    stateGroup.States.Add(ReadStr32(chunk));

                output.StateGroupsWithStates1.Add(stateGroup);
            }

            var sectionThreeCount = chunk.ReadInt32();
            for (var i = 0; i < sectionThreeCount; i++)
            {
                var dialogueEvent = new SoundDatFile.DatDialogueEventsWithStateGroups() { Event = ReadStr32(chunk) };
                var stateGroup = chunk.ReadUInt32();

                for (var j = 0; j < stateGroup; j++)
                    dialogueEvent.StateGroups.Add(chunk.ReadUInt32());

                output.DialogueEventsWithStateGroups.Add(dialogueEvent);
            }

            if (isAtilla)
            {
                var sectionFourCount = chunk.ReadInt32();
                for (var i = 0; i < sectionFourCount; i++)
                    output.SettingValues.Add(new SoundDatFile.DatSettingValues() { Event = ReadStr32(chunk) });
            }
            else
            {
                var sectionFourCount = chunk.ReadInt32();
                for (var i = 0; i < sectionFourCount; i++)
                    output.SettingValues.Add(new SoundDatFile.DatSettingValues() { Event = ReadStr32(chunk), MinValue = chunk.ReadSingle(), MaxValue = chunk.ReadSingle() });

                var sectionFiveCount = chunk.ReadInt32();
                for (var i = 0; i < sectionFiveCount; i++)
                    output.Unknown.Add(ReadStr32(chunk));
            }

            return output;
        }

        public static byte[] WriteData(SoundDatFile file)
        {
            using var memStream = new MemoryStream();

            memStream.Write(ByteParsers.UInt32.EncodeValue((uint)file.EventWithStateGroup.Count(), out _));
            foreach (var value in file.EventWithStateGroup)
            {
                memStream.Write(WriteStr32(value.Event));
                memStream.Write(ByteParsers.Single.EncodeValue(value.Value, out _));
            }

            memStream.Write(ByteParsers.UInt32.EncodeValue((uint)file.StateGroupsWithStates0.Count(), out _));
            foreach (var enumType in file.StateGroupsWithStates0)
            {
                memStream.Write(WriteStr32(enumType.StateGroup));
                memStream.Write(ByteParsers.UInt32.EncodeValue((uint)enumType.States.Count(), out _));

                foreach (var enumValue in enumType.States)
                    memStream.Write(WriteStr32(enumValue));
            }

            memStream.Write(ByteParsers.UInt32.EncodeValue((uint)file.StateGroupsWithStates1.Count(), out _));
            foreach (var enumType in file.StateGroupsWithStates1)
            {
                memStream.Write(WriteStr32(enumType.StateGroup));
                memStream.Write(ByteParsers.UInt32.EncodeValue((uint)enumType.States.Count(), out _));

                foreach (var enumValue in enumType.States)
                    memStream.Write(WriteStr32(enumValue));
            }

            memStream.Write(ByteParsers.UInt32.EncodeValue((uint)file.DialogueEventsWithStateGroups.Count(), out _));
            foreach (var voiceEvent in file.DialogueEventsWithStateGroups)
            {
                memStream.Write(WriteStr32(voiceEvent.Event));
                memStream.Write(ByteParsers.UInt32.EncodeValue((uint)voiceEvent.StateGroups.Count(), out _));

                foreach (var value in voiceEvent.StateGroups)
                    memStream.Write(ByteParsers.UInt32.EncodeValue(value, out _));
            }

            memStream.Write(ByteParsers.UInt32.EncodeValue((uint)file.SettingValues.Count(), out _));
            foreach (var value in file.SettingValues)
            {
                memStream.Write(WriteStr32(value.Event));
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
