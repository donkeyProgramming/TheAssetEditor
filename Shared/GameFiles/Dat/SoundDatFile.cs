using System.Diagnostics;
using System.Text;

namespace Shared.GameFormats.Dat
{
    public class SoundDatFile
    {
        [DebuggerDisplay("EventWithValue {EventName} {Value}")]
        public class DatEventWithStateGroup
        {
            public string EventName { get; set; }
            public float Value { get; set; }
        }

        [DebuggerDisplay("EventWithValues {EventName} [{Values.Count}]")]
        public class DatDialogueEventsWithStateGroups
        {
            public string EventName { get; set; }
            public List<uint> StateGroups { get; set; } = new List<uint>();
        }

        [DebuggerDisplay("SettingValue {EventName} [{MinValue}-{MaxValue}]")]
        public class DatSettingValues
        {
            public string EventName { get; set; }
            public float MinValue { get; set; }
            public float MaxValue { get; set; }
        }


        [DebuggerDisplay("EventTypeEnums {EnumName} [{EnumValues.Count}]")]
        public class DatStateGroupsWithStates
        {
            public string StateGroupName { get; set; }
            public List<string> States { get; set; } = new List<string>();
        }

        public List<DatEventWithStateGroup> EventWithStateGroup { get; set; } = new List<DatEventWithStateGroup>();
        public List<DatStateGroupsWithStates> StateGroupsWithStates0 { get; set; } = new List<DatStateGroupsWithStates>();
        public List<DatStateGroupsWithStates> StateGroupsWithStates1 { get; set; } = new List<DatStateGroupsWithStates>();
        public List<DatDialogueEventsWithStateGroups> DialogueEventsWithStateGroups { get; set; } = new List<DatDialogueEventsWithStateGroups>();
        public List<DatSettingValues> SettingValues { get; set; } = new List<DatSettingValues>();
        public List<string> Unknown { get; set; } = new List<string>();

        public void DumpToFile(string filePath)
        {
            var builder = new StringBuilder();

            builder.AppendLine($"Section 1 -{EventWithStateGroup.Count}");
            foreach (var item in EventWithStateGroup)
                builder.AppendLine($"{item.EventName}, {item.Value}");

            builder.AppendLine($"Section 2 -{StateGroupsWithStates0.Count}");
            foreach (var item in StateGroupsWithStates0)
                builder.AppendLine($"{item.StateGroupName}, [{string.Join(",", item.States)}]");

            builder.AppendLine($"Section 3 -{StateGroupsWithStates1.Count}");
            foreach (var item in StateGroupsWithStates1)
                builder.AppendLine($"{item.StateGroupName}, [{string.Join(",", item.States)}]");

            builder.AppendLine($"Section 4 -{DialogueEventsWithStateGroups.Count}");
            foreach (var item in DialogueEventsWithStateGroups)
                builder.AppendLine($"{item.EventName}, [{string.Join(",", item.StateGroups)}]");

            builder.AppendLine($"Section 5 -{SettingValues.Count}");
            foreach (var item in SettingValues)
                builder.AppendLine($"{item.EventName}, {item.MinValue}, {item.MaxValue}");

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
            output.AddRange(EventWithStateGroup.Select(x => x.EventName));

            output.AddRange(StateGroupsWithStates0.Select(x => x.StateGroupName));
            output.AddRange(StateGroupsWithStates0.SelectMany(x => x.States));

            output.AddRange(StateGroupsWithStates1.Select(x => x.StateGroupName));
            output.AddRange(StateGroupsWithStates1.SelectMany(x => x.States));

            output.AddRange(DialogueEventsWithStateGroups.Select(x => x.EventName));
            output.AddRange(SettingValues.Select(x => x.EventName));
            output.AddRange(Unknown.Select(x => x));

            return output.ToArray();
        }
    }
}
