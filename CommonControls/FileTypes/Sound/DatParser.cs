using CommonControls.FileTypes.PackFiles.Models;
using Filetypes.ByteParsing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CommonControls.FileTypes.Sound
{
    public class DatParser
    {

        public static SoundDatFile ParseBackup(PackFile file)
        {
            var chunk = file.DataSource.ReadDataAsChunk();

            SoundDatFile output = new SoundDatFile();

            var sectionZeroCount = chunk.ReadInt32();
            for (int i = 0; i < sectionZeroCount; i++)
                output.Event0.Add(new SoundDatFile.EventType0() { EventName = ReadStr32(chunk), Value = chunk.ReadSingle() });

            var sectionOneCount = chunk.ReadInt32();
            for (int i = 0; i < sectionOneCount; i++)
            {
                var eventEnum = new SoundDatFile.EventEnums() { EnumName = ReadStr32(chunk) };
                var attrValCount = chunk.ReadUInt32();
                for (int j = 0; j < attrValCount; j++)
                    eventEnum.EnumValues.Add(ReadStr32(chunk));

                output.EventCampaginEnums.Add(eventEnum);
            }

            var sectionTwoCount = chunk.ReadInt32();
            for (int i = 0; i < sectionTwoCount; i++)
            {
                var eventEnum = new SoundDatFile.EventEnums() { EnumName = ReadStr32(chunk) };
                var attrValCount = chunk.ReadUInt32();
                for (int j = 0; j < attrValCount; j++)
                    eventEnum.EnumValues.Add(ReadStr32(chunk));

                output.EventBattleEnums.Add(eventEnum);
            }

            var sectionThreeCount = chunk.ReadInt32();
            for (int i = 0; i < sectionThreeCount; i++)
            {
                var eventEnum = new SoundDatFile.EventType1() { EventName = ReadStr32(chunk) };
                var attrValCount = chunk.ReadUInt32();
                for (int j = 0; j < attrValCount; j++)
                    eventEnum.Values.Add(chunk.ReadUInt32());

                output.Event1.Add(eventEnum);
            }

            var sectionFourCount = chunk.ReadInt32();
            for (int i = 0; i < sectionFourCount; i++)
                output.Event2.Add(new SoundDatFile.EventType2() { EventName = ReadStr32(chunk), MinValue = chunk.ReadSingle(), MaxValue = chunk.ReadSingle() });

            var sectionFiveCount = chunk.ReadInt32();
            for (int i = 0; i < sectionFiveCount; i++)
                output.Event3.Add(new SoundDatFile.EventType3() { EventName = ReadStr32(chunk) });

            return output;
        }

        public static SoundDatFile Parse(PackFile file, bool isAtilla)
        {
            var chunk = file.DataSource.ReadDataAsChunk();

            SoundDatFile output = new SoundDatFile();

            var sectionZeroCount = chunk.ReadUInt32();
            for (int i = 0; i < sectionZeroCount; i++)
                output.Event0.Add(new SoundDatFile.EventType0() { EventName = ReadStr32(chunk), Value = chunk.ReadSingle() });

            var sectionOneCount = chunk.ReadInt32();
            for (int i = 0; i < sectionOneCount; i++)
            {
                var eventEnum = new SoundDatFile.EventEnums() { EnumName = ReadStr32(chunk) };
                var attrValCount = chunk.ReadUInt32();
                for (int j = 0; j < attrValCount; j++)
                    eventEnum.EnumValues.Add(ReadStr32(chunk));

                output.EventCampaginEnums.Add(eventEnum);
            }

            var sectionTwoCount = chunk.ReadInt32();
            for (int i = 0; i < sectionTwoCount; i++)
            {
                var eventEnum = new SoundDatFile.EventEnums() { EnumName = ReadStr32(chunk) };
                var attrValCount = chunk.ReadUInt32();
                for (int j = 0; j < attrValCount; j++)
                    eventEnum.EnumValues.Add(ReadStr32(chunk));

                output.EventBattleEnums.Add(eventEnum);
            }

            var sectionThreeCount = chunk.ReadInt32();
            for (int i = 0; i < sectionThreeCount; i++)
            {
                var eventEnum = new SoundDatFile.EventType1() { EventName = ReadStr32(chunk) };
                var attrValCount = chunk.ReadUInt32();
                for (int j = 0; j < attrValCount; j++)
                    eventEnum.Values.Add(chunk.ReadUInt32());

                output.Event1.Add(eventEnum);
            }

            if (isAtilla)
            {
                var sectionFourCount = chunk.ReadInt32();
                for (int i = 0; i < sectionFourCount; i++)
                    output.Event2.Add(new SoundDatFile.EventType2() { EventName = ReadStr32(chunk) });
            }
            else
            {
                var sectionFourCount = chunk.ReadInt32();
                for (int i = 0; i < sectionFourCount; i++)
                    output.Event2.Add(new SoundDatFile.EventType2() { EventName = ReadStr32(chunk), MinValue = chunk.ReadSingle(), MaxValue = chunk.ReadSingle() });

                var sectionFiveCount = chunk.ReadInt32();
                for (int i = 0; i < sectionFiveCount; i++)
                    output.Event3.Add(new SoundDatFile.EventType3() { EventName = ReadStr32(chunk) });
            }

            return output;
        }

        public static SoundDatFile ParseWh3(PackFile file, bool isAtilla)
        {
            var chunk = file.DataSource.ReadDataAsChunk();
            var output = new SoundDatFile();

            var unk0 = chunk.ReadUInt32();
            var sectionZeroCount = chunk.ReadUInt32();
            for (int i = 0; i < sectionZeroCount; i++)
                output.Event0.Add(new SoundDatFile.EventType0() { EventName = ReadStr32(chunk), Value = chunk.ReadSingle() });

            var sectionOneCount = chunk.ReadInt32();
            for (int i = 0; i < sectionOneCount; i++)
            {
                output.Event0.Add(new SoundDatFile.EventType0() { EventName = ReadStr32(chunk), Value = chunk.ReadSingle() });

                //var eventEnum = new SoundDatFile.EventEnums() { EnumName = ReadStr32(chunk) };
                //var attrValCount = chunk.ReadUInt32();
                //for (int j = 0; j < attrValCount; j++)
                //    eventEnum.EnumValues.Add(ReadStr32(chunk));
                //
                //output.EventCampaginEnums.Add(eventEnum);
            }

            var sectionTwoCount = chunk.ReadInt32();
            for (int i = 0; i < sectionTwoCount; i++)
            {
                var eventEnum = new SoundDatFile.EventEnums() { EnumName = ReadStr32(chunk) };
                var attrValCount = chunk.ReadUInt32();
                for (int j = 0; j < attrValCount; j++)
                    eventEnum.EnumValues.Add(ReadStr32(chunk));

                output.EventBattleEnums.Add(eventEnum);
            }

            var sectionThreeCount = chunk.ReadInt32();
            for (int i = 0; i < sectionThreeCount; i++)
            {
                var eventEnum = new SoundDatFile.EventType1() { EventName = ReadStr32(chunk) };
                var attrValCount = chunk.ReadUInt32();
                for (int j = 0; j < attrValCount; j++)
                    eventEnum.Values.Add(chunk.ReadUInt32());

                output.Event1.Add(eventEnum);
            }

            if (isAtilla)
            {
                var sectionFourCount = chunk.ReadInt32();
                for (int i = 0; i < sectionFourCount; i++)
                    output.Event2.Add(new SoundDatFile.EventType2() { EventName = ReadStr32(chunk) });
            }
            else
            {
                var sectionFourCount = chunk.ReadInt32();
                for (int i = 0; i < sectionFourCount; i++)
                    output.Event2.Add(new SoundDatFile.EventType2() { EventName = ReadStr32(chunk), MinValue = chunk.ReadSingle(), MaxValue = chunk.ReadSingle() });

                var sectionFiveCount = chunk.ReadInt32();
                for (int i = 0; i < sectionFiveCount; i++)
                    output.Event3.Add(new SoundDatFile.EventType3() { EventName = ReadStr32(chunk) });
            }

            return output;
        }

        public static string ReadStr32(ByteChunk chunk)
        {
            var strLength = chunk.ReadInt32();
            var bytes = chunk.ReadBytes(strLength);
            return Encoding.UTF8.GetString(bytes);
        }


    }

    public class SoundDatFile
    {
        [DebuggerDisplay("EventType0 {EventName}")]
        public class EventType0
        {
            public string EventName { get; set; }
            public float Value { get; set; }
        }

        [DebuggerDisplay("EventType1 {EventName}")]
        public class EventType1
        {
            public string EventName { get; set; }
            public List<uint> Values { get; set; } = new List<uint>();
        }

        [DebuggerDisplay("EventType2 {EventName}")]
        public class EventType2
        {
            public string EventName { get; set; }
            public float MinValue { get; set; }
            public float MaxValue { get; set; }
        }

        [DebuggerDisplay("EventType3 {EventName}")]
        public class EventType3
        {
            public string EventName { get; set; }
        }

        [DebuggerDisplay("EventTypeEnums {EnumName}")]
        public class EventEnums
        {
            public string EnumName { get; set; }
            public List<string> EnumValues { get; set; } = new List<string>();
        }

        public List<EventType0> Event0 { get; set; } = new List<EventType0>();
        public List<EventEnums> EventCampaginEnums { get; set; } = new List<EventEnums>();
        public List<EventEnums> EventBattleEnums { get; set; } = new List<EventEnums>();
        public List<EventType1> Event1 { get; set; } = new List<EventType1>();
        public List<EventType2> Event2 { get; set; } = new List<EventType2>();
        public List<EventType3> Event3 { get; set; } = new List<EventType3>();

        public void DumpToFile(string filePath)
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine($"Section 1 -{Event0.Count}");
            foreach (var item in Event0)
                builder.AppendLine($"{item.EventName}, {item.Value}");

            builder.AppendLine($"Section 2 -{EventCampaginEnums.Count}");
            foreach (var item in EventCampaginEnums)
                builder.AppendLine($"{item.EnumName}, [{string.Join(",", item.EnumValues)}]");

            builder.AppendLine($"Section 3 -{EventBattleEnums.Count}");
            foreach (var item in EventBattleEnums)
                builder.AppendLine($"{item.EnumName}, [{string.Join(",", item.EnumValues)}]");

            builder.AppendLine($"Section 4 -{Event1.Count}");
            foreach (var item in Event1)
                builder.AppendLine($"{item.EventName}, [{string.Join(",", item.Values)}]");

            builder.AppendLine($"Section 5 -{Event2.Count}");
            foreach (var item in Event2)
                builder.AppendLine($"{item.EventName}, {item.MinValue}, {item.MaxValue}");

            builder.AppendLine($"Section 6 -{Event3.Count}");
            foreach (var item in Event3)
                builder.AppendLine($"{item.EventName}");

            using (var outputFile = File.Open(filePath, FileMode.OpenOrCreate))
            {
                using var streamWriter = new StreamWriter(outputFile);
                streamWriter.Write(builder.ToString());
            }
        }

        public void Merge(SoundDatFile other)
        {
            Event0.AddRange(other.Event0);
            EventCampaginEnums.AddRange(other.EventCampaginEnums);
            EventBattleEnums.AddRange(other.EventBattleEnums);
            Event1.AddRange(other.Event1);
            Event2.AddRange(other.Event2);
            Event3.AddRange(other.Event3);
        }

        public string[] CreateFileNameList()
        {
            var output = new List<string>();
            output.AddRange(Event0.Select(x => x.EventName));

            output.AddRange(EventCampaginEnums.Select(x => x.EnumName));
            output.AddRange(EventCampaginEnums.SelectMany(x => x.EnumValues));

            output.AddRange(EventBattleEnums.Select(x => x.EnumName));
            output.AddRange(EventBattleEnums.SelectMany(x => x.EnumValues));

            output.AddRange(Event1.Select(x => x.EventName));
            output.AddRange(Event2.Select(x => x.EventName));
            output.AddRange(Event3.Select(x => x.EventName));

            return output.ToArray();
        }

        public void SaveTextVersion(string savePath, Editors.Sound.NameLookupHelper nameHelper)
        {
            StringBuilder output = new StringBuilder();
            output.AppendLine($"Events (count:{Event0.Count}):");
            foreach (var item in Event0)
                output.AppendLine($"\t{item.EventName}[{item.Value}]");

            output.AppendLine();
            output.AppendLine($"Unkown (count:{Event1.Count}):");
            foreach (var item in Event1)
                output.AppendLine($"\t{item.EventName} [{string.Join(", ", item.Values.Select(x=>nameHelper.GetName(x)))}]");

            output.AppendLine();
            output.AppendLine($"Settings (count:{Event2.Count}) [minValue,maxValue]:");
            foreach (var item in Event2)
                output.AppendLine($"\t{item.EventName}[{item.MinValue}, {item.MinValue}]");

            output.AppendLine();
            output.AppendLine($"Enums group 0 (count:{EventBattleEnums.Count}):");
            foreach (var item in EventBattleEnums)
                output.AppendLine($"\t{item.EnumName} [{string.Join(", ", item.EnumValues)}]");

            output.AppendLine();
            output.AppendLine($"Enums group 1 (count:{EventCampaginEnums.Count}):");
            foreach (var item in EventCampaginEnums)
                output.AppendLine($"\t{item.EnumName} [{string.Join(", ", item.EnumValues)}]");

            File.WriteAllText(savePath, output.ToString());
            // Events 1 value
            // Voice Events 3values
            // Settings
            // Values?
            // bnk names
            // Battle enums
            // Campagin enims
        }
    }
}
