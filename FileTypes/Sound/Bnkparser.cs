using Filetypes.ByteParsing;
using FileTypes.PackFiles.Models;
using FileTypes.Sound.WWise;
using FileTypes.Sound.WWise.Bkhd;
using FileTypes.Sound.WWise.Hirc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FileTypes.Sound
{

    public class Bnkparser
    {
        public static void Parse(PackFile file, string[] names)
        {
            var chunk = file.DataSource.ReadDataAsChunk();

            var soundDb = new SoundDataBase();
            var parsers = new Dictionary<string, IParser>();
            parsers["BKHD"] = new BkhdParser();
            parsers["HIRC"] = new HircParser();

            while (chunk.BytesLeft != 0)
            {
                var cc4 = Encoding.UTF8.GetString(chunk.ReadBytes(4));
                parsers[cc4].Parse(chunk, soundDb);
            }

            var nameHelper = new NameLookupHelper(names);

            var events = soundDb.Hircs.Where(x => x.Type == HircType.Event).Cast<CAkEvent>().ToList();
            var actions = soundDb.Hircs.Where(x => x.Type == HircType.Action).Cast<CAkAction>().Where(x => x.ActionType == ActionType.Play);
            var sounds = soundDb.Hircs.Where(x => x.Type == HircType.Sound).Cast<CAkSound>();
            var switches = soundDb.Hircs.Where(x => x.Type == HircType.SwitchContainer).Cast<CAkSwitchCntr>();
            
            var valueMap = new Dictionary<string, List<uint>>();
            foreach (var currentEvent in events)
            {
                currentEvent.DisplayName = nameHelper.GetName(currentEvent.Id);
                if (valueMap.ContainsKey(currentEvent.DisplayName))
                    throw new Exception();

                // Find all actions connected to the event
                var actionIdsForEvent = currentEvent.Actions.Select(x => x.ActionId);
                var actionInstancesForEvent = actions.Where(x => actionIdsForEvent.Contains(x.Id));

                // Find all soudns connected to actions
                var soundIdsForActions = actionInstancesForEvent.Select(x => x.SoundId);
                var soundFileIdsForActions = sounds.Where(x => soundIdsForActions.Contains(x.Id)).Select(x=>x.BankSourceData.akMediaInformation.SourceId);

                valueMap[currentEvent.DisplayName] = soundFileIdsForActions.ToList();
            }

            var connectionStrings = valueMap.Select(x => $"{x.Key} => {x.Value.First()}").ToList();
            var displayStr = string.Join(", \n", connectionStrings);

        }
    }

    public class NameLookupHelper
    {
        Dictionary<uint, string> _hashValueMap = new Dictionary<uint, string>();

        public NameLookupHelper(string[] names)
        {
            foreach (var name in names)
            {
                var hashVal = Hash(name);
                _hashValueMap[hashVal] = name;
            }
        }

        uint Hash(string value)
        {
            var lower = value.ToLower();
            var bytes = Encoding.UTF8.GetBytes(lower);
            
            uint hashValue = 2166136261;
            for (int byteIndex = 0; byteIndex < bytes.Length; byteIndex++)
            {
                var nameByte = bytes[byteIndex];
                hashValue = hashValue * 16777619; 
                hashValue = hashValue ^ nameByte;
                hashValue = hashValue & 0xFFFFFFFF;
            }

            return hashValue;
        }

        public string GetName(uint value)
        {
            if (_hashValueMap.ContainsKey(value))
                return _hashValueMap[value];
            return value.ToString();
        }
    }
}

//https://wiki.xentax.com/index.php/Wwise_SoundBank_(*.bnk)#DIDX_section
//https://github.com/bnnm/wwiser/blob/cd5c086ef2c104e7133e361d385a1023408fb92f/wwiser/wmodel.py#L205
//https://github.com/Maddoxkkm/bnk-event-extraction
//https://github.com/vgmstream/vgmstream/blob/37cc12295c92ec6aa874118fb237bd3821970836/src/meta/bkhd.c
// https://github.com/admiralnelson/total-warhammer-RE-audio/blob/master/BnkExtract.py
// https://github.com/eXpl0it3r/bnkextr