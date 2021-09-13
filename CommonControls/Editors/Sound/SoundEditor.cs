using CommonControls.Services;
using FileTypes.Sound;
using FileTypes.Sound.WWise;
using FileTypes.Sound.WWise.Hirc;
using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;
using System.Diagnostics;
using Newtonsoft.Json.Serialization;
using System.Reflection;
using Newtonsoft.Json;
using System.Collections;
using Common;
using Serilog;
using System.Timers;

namespace CommonControls.Editors.Sound
{
    public class SoundEditor
    {
        ILogger _logger = Logging.Create<SoundEditor>();

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
            var masterDat = new SoundDatFile();
            var datPackFiles = _pfs.FindAllWithExtention(".dat");
            foreach (var datPackFile in datPackFiles)
            { 
                var datFile = DatParser.Parse(datPackFile); ;
                masterDat.Merge(datFile);
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
            var failedFiles = new List<string>();
            var files = _pfs.FindAllWithExtention(".bnk");



            /*
             * 
             * Final test, add a new sound in meta tabel Karl franze running : "Look at me....Wiiiii" 
             * Vocalisation_dlc14_medusa_idle_hiss
             * 
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


            var nameHelper = new NameLookupHelper(masterDat.CreateFileNameList());


            //var actions = masterDb.Hircs.Where(x => x.Type == HircType.Action).Cast<CAkAction>().Where(x => x.ActionType == ActionType.Play);
            //var sounds = masterDb.Hircs.Where(x => x.Type == HircType.Sound).Cast<CAkSound>();
            //var switches = masterDb.Hircs.Where(x => x.Type == HircType.SwitchContainer).Cast<CAkSwitchCntr>();
            //var containers = masterDb.Hircs.Where(x => x.Type == HircType.SequenceContainer).Cast<CAkRanSeqCnt>();

            //var numWemFiles = _pfs.FindAllWithExtention(".wem");
            //var distinctSounds = sounds.DistinctBy(x => x.BankSourceData.akMediaInformation.SourceId).OrderByDescending(x => x.BankSourceData.akMediaInformation.SourceId);
            // 95788523


            var valueMap = new Dictionary<string, VisualEvent>();
          
            var currentFile = 0;
            var timer = new Stopwatch();
            timer.Start();

            foreach (var file in files)
            {
                if (file.Name.Contains("media") || file.Name.Contains("init.bnk"))
                {
                    skippedFiles.Add(file.Name);
                    continue;
                }

                if (file.Name.Contains("battle_individual_artillery__core") == false)
                    continue;

                var soundDb = Bnkparser.Parse(file);
                var db = new ExtenededSoundDataBase(soundDb, masterDat);

                var events = soundDb.Hircs.Where(x => x.Type == HircType.Event).Cast<CAkEvent>().ToList();
                var itemsProcessed = 0;

                _logger.Here().Information($"{currentFile}/{files.Count} {file.Name} NumEvents:{events.Count}");

                VisualEventOutputNode rootOutput = new VisualEventOutputNode($"{file.Name} NumEvents: { events.Count}");

                foreach (var currentEvent in events)
                {
                    var name = nameHelper.GetName(currentEvent.Id); ;
                    var visualEvent = new VisualEvent(currentEvent, db, rootOutput);

                    //valueMap.Add(name, visualEvent);

                    if (itemsProcessed % 100 == 0 && itemsProcessed != 0)
                        _logger.Here().Information($"\t{itemsProcessed}/{events.Count} processsed [{timer.Elapsed.TotalSeconds}s]");
                    itemsProcessed++;
                }
                var str = rootOutput.GetDisplayStr();

                 if(events.Count != 0)
                    _logger.Here().Information($"\t{itemsProcessed}/{events.Count} processsed [{timer.Elapsed.TotalSeconds}s]");

                

                currentFile++;
            }




            //var eventNameIds = events.Select(x => $"{x.Id}\t\t{x.DisplayName}");
            //var eventNameIdsDisplayStr = string.Join(", \n", eventNameIds);
            //
            //
            //var connectionStrings = valueMap.Select(x => $"{x.Key} => {x.Value.FirstOrDefault()}").ToList();
            //var displayStr = string.Join(", \n", connectionStrings);


            // how does CAkActorMixer fit into this?
        }

        public class ExtenededSoundDataBase
        {
            public NameLookupHelper NameHelper { get; private set; }
            public SoundDataBase MasterDb { get; private set; }

            public List<CAkEvent> Events { get; private set; }
            public List<CAkAction> Actions { get; private set; }
            public List<CAkSound> Sounds { get; private set; }
            public List<CAkSwitchCntr> Switches { get; private set; }
            public List<CAkRanSeqCnt> RandomContainers { get; private set; }

            
            public ExtenededSoundDataBase(SoundDataBase masterDb, SoundDatFile masterDat)
            {
                NameHelper = new NameLookupHelper(masterDat.CreateFileNameList());
                MasterDb = masterDb;

                Events = masterDb.Hircs.Where(x => x.Type == HircType.Event).Cast<CAkEvent>().ToList();
                Actions = masterDb.Hircs.Where(x => x.Type == HircType.Action).Cast<CAkAction>().Where(x => x.ActionType == ActionType.Play).ToList();
                Sounds = masterDb.Hircs.Where(x => x.Type == HircType.Sound).Cast<CAkSound>().ToList(); 
                Switches = masterDb.Hircs.Where(x => x.Type == HircType.SwitchContainer).Cast<CAkSwitchCntr>().ToList(); 
                RandomContainers = masterDb.Hircs.Where(x => x.Type == HircType.SequenceContainer).Cast<CAkRanSeqCnt>().ToList(); 
            }

            public List<HricItem> GetHircObject(uint id, string bnkFile)
            {
                return MasterDb.Hircs.Where(x => x.Id == id && x.OwnerFile == bnkFile).ToList();
            }

            public List<HricItem> GetHircObjects(List<uint> ids, string bnkFile)
            {
                return MasterDb.Hircs.Where(x => ids.Contains(x.Id) && x.OwnerFile == bnkFile).ToList();
            }


        }

        public class VisualEventOutputNode
        {
            public string Data { get; set; } = "";
            public List<VisualEventOutputNode> Children { get; set; } = new List<VisualEventOutputNode>();

            public VisualEventOutputNode(string data)
            {
                Data = data;
            }

            public VisualEventOutputNode AddChild(string data)
            {
                var child = new VisualEventOutputNode(data);
                Children.Add(child);
                return child;
            }

            public string GetDisplayStr()
            {
                var JsonSettings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    ContractResolver = ShouldSerializeContractResolver.Instance,
                };

                return JsonConvert.SerializeObject(this, JsonSettings);
            }

        }

        public class ShouldSerializeContractResolver : DefaultContractResolver
        {
            public static readonly ShouldSerializeContractResolver Instance = new ShouldSerializeContractResolver();

            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                JsonProperty property = base.CreateProperty(member, memberSerialization);

                if (property.PropertyType != typeof(string))
                {
                    if (property.PropertyType.GetInterface(nameof(IEnumerable)) != null)
                        property.ShouldSerialize = instance => (instance?.GetType().GetProperty(property.PropertyName).GetValue(instance) as IEnumerable<object>)?.Count() > 0;
                }
                return property;
            }
        }

        public class VisualEvent
        {
            CAkEvent _startEvent;
            ExtenededSoundDataBase _db;
            string _name;
             


            public VisualEventOutputNode Output { get; set; }
            public bool ProcesedCorrectly { get; set; } = true;
            public VisualEvent(CAkEvent startEvent, ExtenededSoundDataBase db, VisualEventOutputNode rootOutput)
            {
                _startEvent = startEvent;
                _db = db;

                _name = _db.NameHelper.GetName(_startEvent.Id);
                Output = rootOutput.AddChild($"{_name}[{_startEvent.Id}]");

                Process();
            }


            void Process()
            {
                if (_name == "Battle_Environment_Element_Earth_Shipwreck_Creaks_Start")
                { 
                
                }

                // Find all actions
                var actionIdsForEvent = _startEvent.Actions.Select(x => x.ActionId).ToList();

                var children = _db.GetHircObjects(actionIdsForEvent, _startEvent.OwnerFile);
                foreach (var child in children)
                    ProcessGenericChild(child, Output);
            }


            void ProcessGenericChild(HricItem item, VisualEventOutputNode currentNode)
            {
                if (item is CAkAction action)
                    ProcessChild(action, currentNode);
                else if (item is CAkSound sound)
                    ProcessChild(sound, currentNode);
                else if (item is CAkSwitchCntr switchContainer)
                    ProcessChild(switchContainer, currentNode);
                else if (item is CAkRanSeqCnt randomContainer)
                    ProcessChild(randomContainer, currentNode);
                else if (item is CAkLayerCntr layeredControl)
                    ProcessChild(layeredControl, currentNode);
                else
                    ProcessUnknownChild(item, currentNode);
            }

            void ProcessChild(CAkAction item, VisualEventOutputNode currentNode)
            {
                var node = currentNode.AddChild($"CAkAction [{item.Id}] ActionType:[{item.ActionType}]");

                var actionRefs = _db.GetHircObject(item.SoundId, _startEvent.OwnerFile);
                foreach (var actionRef in actionRefs)
                    ProcessGenericChild(actionRef, node);
            }

            void ProcessChild(CAkSound item, VisualEventOutputNode currentNode)
            {
                currentNode.AddChild($"CAkSound [{item.Id}] => {item.BankSourceData.akMediaInformation.SourceId}.wem");
            }

            void ProcessChild(CAkSwitchCntr item, VisualEventOutputNode currentNode)
            {
                var node = currentNode.AddChild($"CAkSwitchCntr [{item.Id}] enumGroup:[{_db.NameHelper.GetName(item.ulGroupID)}] default:[{_db.NameHelper.GetName(item.ulDefaultSwitch)}]" );
                foreach (var switchCase in item.SwitchList)
               {
                    var switchCaseNode = node.AddChild($"SwitchValue [{_db.NameHelper.GetName(switchCase.SwitchId)}]");
                    foreach (var child in switchCase.NodeIdList)
                    {
                        var childRef = _db.GetHircObject(child, _startEvent.OwnerFile);
                        Debug.Assert(childRef.Count() <= 1);

                        if (childRef.FirstOrDefault() != null)
                            ProcessGenericChild(childRef.FirstOrDefault(), switchCaseNode);
                    }
               }
            }

            void ProcessChild(CAkLayerCntr item, VisualEventOutputNode currentNode)
            {
                var node = currentNode.AddChild($"CAkLayerCntr [{item.Id}]");
                foreach (var layer in item.LayerList)
                {
                    var switchCaseNode = node.AddChild($"LayerChildItem [{_db.NameHelper.GetName(layer.rtpcID)}]");
                    foreach (var child in layer.CAssociatedChildDataList)
                    {
                        var childRef = _db.GetHircObject(child.ulAssociatedChildID, _startEvent.OwnerFile);
                        Debug.Assert(childRef.Count() <= 1);

                        if (childRef.FirstOrDefault() != null)
                            ProcessGenericChild(childRef.FirstOrDefault(), switchCaseNode);
                    }
                }
            }

            void ProcessChild(CAkRanSeqCnt item, VisualEventOutputNode currentNode)
            {
                var node = currentNode.AddChild($"CAkRanSeqCnt [{item.Id}]");

                foreach (var playListItem in item.AkPlaylist)
                {
                    var playListRefs = _db.GetHircObject(playListItem.PlayId, _startEvent.OwnerFile);
                    Debug.Assert(playListRefs.Count() <= 1);

                    if(playListRefs.FirstOrDefault() != null)
                        ProcessGenericChild(playListRefs.FirstOrDefault(), node);
                }

            }

            void ProcessUnknownChild(HricItem item, VisualEventOutputNode currentNode)
            {
                currentNode.AddChild($"Unknown HricItem [{item.Id}] [{item.Type}]");
                ProcesedCorrectly = false;
            }


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
