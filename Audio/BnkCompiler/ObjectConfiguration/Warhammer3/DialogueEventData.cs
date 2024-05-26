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

        public DialogueEventData(IAudioRepository audioRepository)
        {
            _audioRepository = audioRepository;
        }

        public static Dictionary<string, List<string>> ExtractedDialogueEvents = new Dictionary<string, List<string>>();

        // Dynamically extract dialogue events from the dat files (dialogue events are contained within Section 4).
        public void ExtractDialogueEventsFromDat()
        {
            var datDumpMasterPath = $"{DirectoryHelper.Temp}\\DatDumps\\dat_dump_master.txt";
            var lines = File.ReadAllLines(datDumpMasterPath);
            var datSection4 = ExtractSection(lines, "Section 4", "Section 5");

            foreach (var line in datSection4)
            {
                var parts = ParseLine(line);
                var dialogueEvent = parts.Item1;
                var stateGroups = parts.Item2;

                foreach (var value in stateGroups)
                {
                    if (value != "")
                    {
                        var hashedId = uint.Parse(value);

                        if (!ExtractedDialogueEvents.ContainsKey(dialogueEvent))
                            ExtractedDialogueEvents[dialogueEvent] = new List<string>();

                        ExtractedDialogueEvents[dialogueEvent].Add(_audioRepository.GetNameFromHash(hashedId));
                    }
                }
            }
        }

        public static List<string> ExtractSection(string[] lines, string startSection, string endSection)
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

        public static Tuple<string, List<string>> ParseLine(string line)
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

        public static string MatchDialogueEventToBnk(string dialogueEvent)
        {
            dialogueEvent = dialogueEvent.ToLower();

            if (dialogueEvent.Contains("battle_vo_conversation"))
                return "battle_vo_conversational";

            else if (dialogueEvent.Contains("battle_vo_order"))
                return "battle_vo_orders";

            else if (dialogueEvent.Contains("campaign_vo_cs"))
                return "campaign_vo_conversational";

            else if (dialogueEvent.Contains("campaign_vo") || dialogueEvent == "gotrek_felix_arrival" || dialogueEvent == "gotrek_felix_departure")
                return "campaign_vo";

            else if (dialogueEvent.Contains("frontend_vo"))
                return "frontend_vo";

            else if (dialogueEvent == "battle_individual_melee_weapon_hit")
                return "battle_individual_melee";

            else
                throw new Exception($"{dialogueEvent} could not be matched to a bnk.");
        }
    }
}
