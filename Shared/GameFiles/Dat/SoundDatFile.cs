using System.Diagnostics;
using System.Text;

namespace Shared.GameFormats.Dat
{
    public class SoundDatFile
    {
        [DebuggerDisplay("EventWithValue {EventName} {Value}")]
        public class DatEventWithStateGroup
        {
            public string Event { get; set; }
            public float Value { get; set; }
        }

        [DebuggerDisplay("EventWithValues {EventName} [{Values.Count}]")]
        public class DatDialogueEventsWithStateGroups
        {
            public string Event { get; set; }
            public List<uint> StateGroups { get; set; } = new List<uint>();
        }

        [DebuggerDisplay("SettingValue {EventName} [{MinValue}-{MaxValue}]")]
        public class DatSettingValues
        {
            public string Event { get; set; }
            public float MinValue { get; set; }
            public float MaxValue { get; set; }
        }


        [DebuggerDisplay("EventTypeEnums {EnumName} [{EnumValues.Count}]")]
        public class DatStateGroupsWithStates
        {
            public string StateGroup { get; set; }
            public List<string> States { get; set; } = [];
        }

        public List<DatEventWithStateGroup> EventWithStateGroup { get; set; } = [];
        public List<DatStateGroupsWithStates> StateGroupsWithStates0 { get; set; } = [];
        public List<DatStateGroupsWithStates> StateGroupsWithStates1 { get; set; } = [];
        public List<DatDialogueEventsWithStateGroups> DialogueEventsWithStateGroups { get; set; } = [];
        public List<DatSettingValues> SettingValues { get; set; } = [];
        public List<string> Unknown { get; set; } = [];

        public void DumpToFile(string filePath)
        {
            var builder = new StringBuilder();

            builder.AppendLine($"Section 1 -{EventWithStateGroup.Count}");
            foreach (var item in EventWithStateGroup)
                builder.AppendLine($"{item.Event}, {item.Value}");

            builder.AppendLine($"Section 2 -{StateGroupsWithStates0.Count}");
            foreach (var item in StateGroupsWithStates0)
                builder.AppendLine($"{item.StateGroup}, [{string.Join(",", item.States)}]");

            builder.AppendLine($"Section 3 -{StateGroupsWithStates1.Count}");
            foreach (var item in StateGroupsWithStates1)
                builder.AppendLine($"{item.StateGroup}, [{string.Join(",", item.States)}]");

            builder.AppendLine($"Section 4 -{DialogueEventsWithStateGroups.Count}");
            foreach (var item in DialogueEventsWithStateGroups)
                builder.AppendLine($"{item.Event}, [{string.Join(",", item.StateGroups)}]");

            builder.AppendLine($"Section 5 -{SettingValues.Count}");
            foreach (var item in SettingValues)
                builder.AppendLine($"{item.Event}, {item.MinValue}, {item.MaxValue}");

            builder.AppendLine($"Section 6 -{Unknown.Count}");
            foreach (var item in Unknown)
                builder.AppendLine($"{item}");

            using var outputFile = File.Open(filePath, FileMode.OpenOrCreate);
            using var streamWriter = new StreamWriter(outputFile);
            streamWriter.Write(builder.ToString());
        }

        public void Merge(SoundDatFile other)
        {
            EventWithStateGroup.AddRange(other.EventWithStateGroup);
            StateGroupsWithStates0.AddRange(other.StateGroupsWithStates0);
            StateGroupsWithStates1.AddRange(other.StateGroupsWithStates1);
            DialogueEventsWithStateGroups.AddRange(other.DialogueEventsWithStateGroups);
            SettingValues.AddRange(other.SettingValues);
            Unknown.AddRange(other.Unknown);
        }

        public string[] CreateFileNameList()
        {
            var output = new List<string>();
            output.AddRange(EventWithStateGroup.Select(x => x.Event));

            output.AddRange(StateGroupsWithStates0.Select(x => x.StateGroup));
            output.AddRange(StateGroupsWithStates0.SelectMany(x => x.States));

            output.AddRange(StateGroupsWithStates1.Select(x => x.StateGroup));
            output.AddRange(StateGroupsWithStates1.SelectMany(x => x.States));

            output.AddRange(DialogueEventsWithStateGroups.Select(x => x.Event));
            output.AddRange(SettingValues.Select(x => x.Event));
            output.AddRange(Unknown.Select(x => x));

            return output.ToArray();
        }
    }
}
