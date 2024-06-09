using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Shared.Core.Misc;
using Audio.Storage;
using Shared.Core.PackFiles;
using Shared.Core.Events;
using Microsoft.VisualBasic.Devices;
using Shared.Core.PackFiles.Models;
using System.Text;
using Shared.GameFormats.Dat;

namespace Audio.BnkCompiler.ObjectConfiguration.Warhammer3
{
    public class DialogueEventData
    {
        private readonly PackFileService _packFileService;
        private readonly IAudioRepository _audioRepository;

        public DialogueEventData(PackFileService packFileService, IAudioRepository audioRepository)
        {
            _packFileService = packFileService;
            _audioRepository = audioRepository;
        }

        public static Dictionary<string, List<string>> ExtractedDialogueEvents = new Dictionary<string, List<string>>();
        public static List<string> ExtractedStateGroups = new List<string>();
        public static Dictionary<string, List<string>> ExtractedStates = new Dictionary<string, List<string>>();

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

        public void StoreExtractedDialogueEvents()
        {
            ExtractedDialogueEvents = new Dictionary<string, List<string>>();
            ExtractedStateGroups = new List<string>();
            var eventDataDatFile = _packFileService.FindFile(@"audio\wwise\event_data__core.dat");
            //var parsedDatFile = WWiseNameLoader.LoadDatFile(eventDataDatFile);
            //var datSection4 = SoundDatFile.VoiceEvents;
            // create a dictionary of dialogue events and their states

            ExtractedStateGroups.Sort();
        }
    }
}
