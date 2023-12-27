using Audio.Storage;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Audio.FileFormats.Dat
{
    public class SoundDatFile
    {
        [DebuggerDisplay("EventWithValue {EventName} {Value}")]
        public class EventWithValue
        {
            public string EventName { get; set; }
            public float Value { get; set; }
        }

        [DebuggerDisplay("EventWithValues {EventName} [{Values.Count}]")]
        public class EventWithValues
        {
            public string EventName { get; set; }
            public List<uint> Values { get; set; } = new List<uint>();
        }

        [DebuggerDisplay("SettingValue {EventName} [{MinValue}-{MaxValue}]")]
        public class SettingValue
        {
            public string EventName { get; set; }
            public float MinValue { get; set; }
            public float MaxValue { get; set; }
        }


        [DebuggerDisplay("EventTypeEnums {EnumName} [{EnumValues.Count}]")]
        public class EventEnums
        {
            public string EnumName { get; set; }
            public List<string> EnumValues { get; set; } = new List<string>();
        }

        public List<EventWithValue> Event0 { get; set; } = new List<EventWithValue>();
        public List<EventEnums> EnumGroup0 { get; set; } = new List<EventEnums>();
        public List<EventEnums> EnumGroup1 { get; set; } = new List<EventEnums>();
        public List<EventWithValues> VoiceEvents { get; set; } = new List<EventWithValues>();
        public List<SettingValue> SettingValues { get; set; } = new List<SettingValue>();
        public List<string> Unknown { get; set; } = new List<string>();

        public void DumpToFile(string filePath)
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine($"Section 1 -{Event0.Count}");
            foreach (var item in Event0)
                builder.AppendLine($"{item.EventName}, {item.Value}");

            builder.AppendLine($"Section 2 -{EnumGroup0.Count}");
            foreach (var item in EnumGroup0)
                builder.AppendLine($"{item.EnumName}, [{string.Join(",", item.EnumValues)}]");

            builder.AppendLine($"Section 3 -{EnumGroup1.Count}");
            foreach (var item in EnumGroup1)
                builder.AppendLine($"{item.EnumName}, [{string.Join(",", item.EnumValues)}]");

            builder.AppendLine($"Section 4 -{VoiceEvents.Count}");
            foreach (var item in VoiceEvents)
                builder.AppendLine($"{item.EventName}, [{string.Join(",", item.Values)}]");

            builder.AppendLine($"Section 5 -{SettingValues.Count}");
            foreach (var item in SettingValues)
                builder.AppendLine($"{item.EventName}, {item.MinValue}, {item.MaxValue}");

            builder.AppendLine($"Section 6 -{Unknown.Count}");
            foreach (var item in Unknown)
                builder.AppendLine($"{item}");

            using (var outputFile = File.Open(filePath, FileMode.OpenOrCreate))
            {
                using var streamWriter = new StreamWriter(outputFile);
                streamWriter.Write(builder.ToString());
            }
        }

        public void Merge(SoundDatFile other)
        {
            Event0.AddRange(other.Event0);
            EnumGroup0.AddRange(other.EnumGroup0);
            EnumGroup1.AddRange(other.EnumGroup1);
            VoiceEvents.AddRange(other.VoiceEvents);
            SettingValues.AddRange(other.SettingValues);
            Unknown.AddRange(other.Unknown);
        }

        public string[] CreateFileNameList()
        {
            var output = new List<string>();
            output.AddRange(Event0.Select(x => x.EventName));

            output.AddRange(EnumGroup0.Select(x => x.EnumName));
            output.AddRange(EnumGroup0.SelectMany(x => x.EnumValues));

            output.AddRange(EnumGroup1.Select(x => x.EnumName));
            output.AddRange(EnumGroup1.SelectMany(x => x.EnumValues));

            output.AddRange(VoiceEvents.Select(x => x.EventName));
            output.AddRange(SettingValues.Select(x => x.EventName));
            output.AddRange(Unknown.Select(x => x));

            return output.ToArray();
        }

        public void SaveTextVersion(string savePath, IAudioRepository audioRepository)
        {
            StringBuilder output = new StringBuilder();
            output.AppendLine($"Events (count:{Event0.Count}):");
            foreach (var item in Event0)
                output.AppendLine($"\t{item.EventName}[{item.Value}]");

            output.AppendLine();
            output.AppendLine($"Unknown (count:{VoiceEvents.Count}):");
            foreach (var item in VoiceEvents)
                output.AppendLine($"\t{item.EventName} [{string.Join(", ", item.Values.Select(x => audioRepository.GetNameFromHash(x)))}]");

            output.AppendLine();
            output.AppendLine($"Settings (count:{SettingValues.Count}) [minValue,maxValue]:");
            foreach (var item in SettingValues)
                output.AppendLine($"\t{item.EventName}[{item.MinValue}, {item.MinValue}]");

            output.AppendLine();
            output.AppendLine($"Enums group 0 (count:{EnumGroup1.Count}):");
            foreach (var item in EnumGroup1)
                output.AppendLine($"\t{item.EnumName} [{string.Join(", ", item.EnumValues)}]");

            output.AppendLine();
            output.AppendLine($"Enums group 1 (count:{EnumGroup0.Count}):");
            foreach (var item in EnumGroup0)
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
