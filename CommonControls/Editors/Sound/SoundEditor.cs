using CommonControls.Services;
using FileTypes.Sound;
using FileTypes.Sound.WWise;
using FileTypes.Sound.WWise.Hirc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CommonControls.Editors.Sound
{
    public class SoundEditor
    {
        private readonly PackFileService _pfs;

        public SoundEditor(PackFileService pfs)
        {
            _pfs = pfs;
        }

        public void ParseAll()
        {


            // Battle_Group_Foley_Collision_Metal_Stop
            // 1902570353
            //var stringToHash = "Battle_Group_Foley_Collision_Metal_Stop";
            //var lower = stringToHash.ToLower();
            //var bytes = System.Text.Encoding.UTF8.GetBytes(lower);
            //
            //uint hashValue = 2166136261;
            //for (int byteIndex = 0; byteIndex < bytes.Length; byteIndex++)
            //{
            //    var nameByte = bytes[byteIndex];
            //    hashValue = hashValue * 16777619; //#FNV prime
            //    hashValue = hashValue ^ nameByte; //#FNV xor
            //    hashValue = hashValue & 0xFFFFFFFF; //#python clamp
            //}
            //
            //
            //// Create a super dat
            var masterFile = new SoundDatFile();
            var datPackFiles = _pfs.FindAllWithExtention(".dat");
            foreach (var datPackFile in datPackFiles)
            { 
                var datFile = DatParser.Parse(datPackFile); ;
                masterFile.Merge(datFile);
            }
            //masterFile.DumpToFile(@"C:\temp\SoundTesting\masterDatDump.txt");
            //
            ////
            //var bnkFile = packfileService.FindFile(@"audio/wwise/battle_advice__core.bnk");
            //var bnkFile = _pfs.FindFile(@"audio/wwise/battle_individual_melee__warhammer2.bnk");
            //var test = Bnkparser.Parse(bnkFile);

            var masterDb = new SoundDataBase();
            var parsedFiles = new List<string>();
            var skippedFiles = new List<string>();
            var files = _pfs.FindAllWithExtention(".bnk");
            foreach (var file in files)
            {
                if (file.Name.Contains("media"))
                {
                    skippedFiles.Add(file.Name);
                    continue;
                }

                var soundDb = Bnkparser.Parse(file);
                masterDb.Hircs.AddRange(soundDb.Hircs);
                parsedFiles.Add(file.Name);
            }

             


            /*
                event > action > sound > .wem
                event > action > random-sequence > sound(s) > .wem
                event > action > switch > switch/segment/sound > ...
                event > action > music segment > music track(s) > .wem(s).
                event > action > music random-sequence > music segment(s) > ...
                event > action > music switch > switch(es)/segment(s)/random-sequence(s) > ...


                Event => action     =>  sound
                                    =>  CAkActionSetAkProp
                                    =>  Switch  => sound
                                                => Rand

                                    =>  Rand    => Sound
             */


            var nameHelper = new NameLookupHelper(masterFile.CreateFileNameList());
            
            var events = masterDb.Hircs.Where(x => x.Type == HircType.Event).Cast<CAkEvent>().ToList();
            var actions = masterDb.Hircs.Where(x => x.Type == HircType.Action).Cast<CAkAction>().Where(x => x.ActionType == ActionType.Play);
            var sounds = masterDb.Hircs.Where(x => x.Type == HircType.Sound).Cast<CAkSound>();
            var switches = masterDb.Hircs.Where(x => x.Type == HircType.SwitchContainer).Cast<CAkSwitchCntr>();
            var containers = masterDb.Hircs.Where(x => x.Type == HircType.SequenceContainer).Cast<CAkRanSeqCnt>();


            // 95788523

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
                var soundFileIdsForActions = sounds
                    .Where(x => soundIdsForActions.Contains(x.Id))
                    .Select(x=>x.BankSourceData.akMediaInformation.SourceId);

                var switchesForActions = switches
                    .Where(x => soundIdsForActions.Contains(x.Id));

                var containersForAction = containers
                    .Where(x => soundIdsForActions.Contains(x.Id));

                valueMap[currentEvent.DisplayName] = soundFileIdsForActions.ToList();
            }
            
            var connectionStrings = valueMap.Select(x => $"{x.Key} => {x.Value.FirstOrDefault()}").ToList();
            var displayStr = string.Join(", \n", connectionStrings);

        }

        public class VisualView
        { 
            
        }

        public class SoundEvent
        { 
            
        }

        public class SoundAction
        { 
        }

        public class Sound
        { 
        }

        public class SoundSwitch
        {
        }

        public class SoundContainer
        { 
        
        }
    }
}
