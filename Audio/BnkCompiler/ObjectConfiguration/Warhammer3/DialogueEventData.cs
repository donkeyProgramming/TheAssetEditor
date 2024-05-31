using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Shared.Core.Misc;
using Audio.Storage;

namespace Audio.BnkCompiler.ObjectConfiguration.Warhammer3
{
    public class DialogueEventData
    {
        private readonly IAudioRepository _audioRepository;

        public DialogueEventData(IAudioRepository audioRepository) => _audioRepository = audioRepository;

        public static Dictionary<string, List<string>> ExtractedDialogueEvents = new Dictionary<string, List<string>>();
        public static List<string> ExtractedStateGroups = new List<string>();
        public static Dictionary<string, List<string>> ExtractedStates = new Dictionary<string, List<string>>();        

        // Dynamically extract dialogue events from the master dat file (dialogue events are contained within Section 4).
        public void ExtractDialogueEventsDataFromDat()
        {
            ExtractedDialogueEvents = new Dictionary<string, List<string>>();
            ExtractedStateGroups = new List<string>();

            var datDumpMasterPath = $"{DirectoryHelper.Temp}\\DatDumps\\dat_dump_master.txt";
            var lines = File.ReadAllLines(datDumpMasterPath);
            var datSection4 = ExtractSection(lines, "Section 4", "Section 5");

            foreach (var line in datSection4)
            {
                var parts = ParseLine(line);
                var extractedDialogueEvent = parts.Item1;
                var extractedStateGroups = parts.Item2;

                foreach (var extractedStateGroup in extractedStateGroups)
                {
                    if (extractedStateGroup != "")
                    {
                        var stateGroup = _audioRepository.GetNameFromHash(uint.Parse(extractedStateGroup));

                        if (!ExtractedDialogueEvents.ContainsKey(extractedDialogueEvent))
                            ExtractedDialogueEvents[extractedDialogueEvent] = new List<string>();

                        ExtractedDialogueEvents[extractedDialogueEvent].Add(stateGroup);

                        if (!ExtractedStateGroups.Contains(stateGroup))
                            ExtractedStateGroups.Add(stateGroup);
                    }
                }
            }

            ExtractedStateGroups.Sort();

            var csvFilePath = $"{DirectoryHelper.Temp}\\DatDumps\\dat_dump_dialogue_events.csv";
            ExportData(ExtractedDialogueEvents, csvFilePath);
        }

        // Dynamically extract states from the master dat file (states are contained within Section 3).
        public static void ExtractStatesDataFromDat()
        {
            ExtractedStates = new Dictionary<string, List<string>>();

            var datDumpMasterPath = $"{DirectoryHelper.Temp}\\DatDumps\\dat_dump_master.txt";
            var lines = File.ReadAllLines(datDumpMasterPath);
            var datSection4 = ExtractSection(lines, "Section 3", "Section 4");

            foreach (var line in datSection4)
            {
                var parts = ParseLine(line);
                var extractedStateGroup = parts.Item1;
                var extractedStates = parts.Item2;

                foreach (var extractedState in extractedStates)
                {
                    if (ExtractedStateGroups.Contains(extractedStateGroup))
                    {
                        var state = extractedState;
                        if (state == "None")
                            state = "Any";

                        if (!ExtractedStates.ContainsKey(extractedStateGroup))
                            ExtractedStates[extractedStateGroup] = new List<string>();

                        ExtractedStates[extractedStateGroup].Add(state);
                    }
                }
            }

            var csvFilePath = $"{DirectoryHelper.Temp}\\DatDumps\\dat_dump_states.csv";
            ExportData(ExtractedStates, csvFilePath);
        }

        private static List<string> ExtractSection(string[] lines, string startSection, string endSection)
        {
            var sectionLines = new List<string>();
            var inSection = false;

            foreach (var line in lines)
            {
                if (line.StartsWith(startSection))
                {
                    inSection = true;
                    continue;
                }

                if (inSection && line.StartsWith(endSection))
                    break;

                if (inSection)
                    sectionLines.Add(line);
            }

            return sectionLines;
        }

        private static Tuple<string, List<string>> ParseLine(string line)
        {
            var commaIndex = line.IndexOf(',');

            if (commaIndex == -1)
                throw new Exception("Invalid line format");

            var key = line.Substring(0, commaIndex).Trim();
            var valuesPart = line.Substring(commaIndex + 1).Trim();

            valuesPart = valuesPart.Trim('[', ']');
            var values = valuesPart.Split(',').Select(s => s.Trim()).ToList();

            return new Tuple<string, List<string>>(key, values);
        }

        private static void ExportData(Dictionary<string, List<string>> data, string csvFilePath)
        {
            using var writer = new StreamWriter(csvFilePath);
            // Write the headers
            var headerLine = string.Join(",", data.Keys);
            writer.WriteLine(headerLine);

            // Get the maximum number of items in any group to ensure all rows are filled
            var maxItems = data.Values.Max(states => states.Count);

            for (var i = 0; i < maxItems; i++)
            {
                var row = new List<string>();

                foreach (var states in data.Values)
                {
                    if (i < states.Count)
                        row.Add(states[i]);
                    else
                        row.Add(string.Empty); // Add empty string if there are no more items in this group
                }

                var csvLine = string.Join(",", row);
                writer.WriteLine(csvLine);
            }
        }

        // Get the bnk that a dialogue event is contained within.
        public static string GetBnkFromDialogueEvent(string dialogueEvent)
        {
            dialogueEvent = dialogueEvent.ToLower();

            if (dialogueEvent.Contains("battle_vo_conversation"))
                return "battle_vo_conversational";

            else if (dialogueEvent.Contains("battle_vo_order"))
                return "battle_vo_orders";

            else if (dialogueEvent.Contains("campaign_vo_cs") || dialogueEvent.Contains("Campaign_CS"))
                return "campaign_vo_conversational";

            else if (dialogueEvent.Contains("campaign_vo") || dialogueEvent == "gotrek_felix_arrival" || dialogueEvent == "gotrek_felix_departure")
                return "campaign_vo";

            else if (dialogueEvent.Contains("frontend_vo"))
                return "frontend_vo";

            else if (dialogueEvent == "Battle_Individual_Melee_Weapon_Hit")
                return "battle_individual_melee";

            else
                throw new Exception($"Error: {dialogueEvent} could not be matched to a bnk.");
        }
    }
}
