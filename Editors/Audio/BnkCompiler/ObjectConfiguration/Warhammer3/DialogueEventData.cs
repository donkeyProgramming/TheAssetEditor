using System;
using System.Collections.Generic;
using Audio.Storage;
using Shared.Core.PackFiles;
using Shared.GameFormats.Dat;

namespace Audio.BnkCompiler.ObjectConfiguration.Warhammer3
{
    public class DialogueEventData
    {
        public static Dictionary<string, List<string>> ExtractedDialogueEvents = new Dictionary<string, List<string>>();

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

        public static void StoreExtractedDialogueEvents(IAudioRepository audioRepository, PackFileService packFileService)
        {
            ExtractedDialogueEvents = new Dictionary<string, List<string>>();

            var eventDataDatFile = packFileService.FindFile(@"audio\wwise\event_data__core.dat");
            var parsedDatFile = DatFileParser.Parse(eventDataDatFile, false);
            var extractedDialogueEvents = parsedDatFile.DialogueEvents;

            foreach (var dialogueEvent in extractedDialogueEvents)
            {
                if (!ExtractedDialogueEvents.ContainsKey(dialogueEvent.EventName))
                    ExtractedDialogueEvents[dialogueEvent.EventName] = new List<string>();

                foreach (var stateGroupId in dialogueEvent.Values)
                {
                    var stateGroup = audioRepository.GetNameFromHash(stateGroupId);
                    ExtractedDialogueEvents[dialogueEvent.EventName].Add(stateGroup);
                }
            }
        }
    }
}
