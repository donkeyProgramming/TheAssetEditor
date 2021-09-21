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
using System.IO;
using System.Text;
using FileTypes.PackFiles.Models;

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

        List<PackFile> GetAttilaFiles()
        {
            var fileInFolder = Directory.GetFiles(@"C:\temp\SoundTesting\Attila");
            fileInFolder = fileInFolder.Where(x => x.Contains(".bnk", StringComparison.OrdinalIgnoreCase)).ToArray();

            var packfilList = new List<PackFile>();
            foreach (var file in fileInFolder)
            {
                var tempFile = new PackFile(Path.GetFileName(file), new FileSystemSource(file));
                packfilList.Add(tempFile);
            }
            return packfilList;
        }

        List<PackFile> GetPackFileFiles()
        {
            var files = _pfs.FindAllWithExtention(".bnk");
            return files;
        }

        NameLookupHelper GetNameHelper(List<PackFile> files)
        {
            var masterDat = new SoundDatFile();
            var datPackFiles = _pfs.FindAllWithExtention(".dat");
            foreach (var datPackFile in datPackFiles)
            {
                var datFile = DatParser.Parse(datPackFile); ;
                masterDat.Merge(datFile);
            }

            var fileNameDump0 = files.Select(x => x.Name);
            var fileNameDump1 = files.Select(x => Path.GetFileNameWithoutExtension(x.Name));
            var fileNameDump3 = fileNameDump0.Union(fileNameDump1);
            var fileNameDump4 = string.Join(", \n", fileNameDump3);

            foreach (var item in fileNameDump3)
                masterDat.Event3.Add(new SoundDatFile.EventType3() { EventName = item });

            var nameHelper = new NameLookupHelper(masterDat.CreateFileNameList());
            return nameHelper;
        }

        public void ParseAll()
        {
            var masterDb = new SoundDataBase();
            var parsedFiles = new List<string>();
            var skippedFiles = new List<string>();
            var failedFiles = new List<string>();

            var files = GetAttilaFiles();
            var nameHelper = GetNameHelper(files);

            var numWemFiles = _pfs.FindAllWithExtention(".wem");
            List<uint> referensedSounds = new List<uint>();
            List<string> unknownObjectTypes = new List<string>();

            var currentFile = 0;
            var timer = new Stopwatch();
            timer.Start();

            VisualEventOutputNode rootOutput = new VisualEventOutputNode($"Root :");
            var statsNode = rootOutput.AddChild("Stats");

            foreach (var file in files)
            {
                if (file.Name.Contains("media") || file.Name.Contains("init.bnk") || file.Name.Contains("animation_blood_data.bnk"))
                {
                    skippedFiles.Add(file.Name);
                    continue; 
                }


                if (file.Name.Contains("battle_advice.bnk") == false)
                    continue;
                

                //if (file.Name.Contains("battle_individual_artillery__core") == false)
                //    continue;

                //if (file.Name.Contains("battle_vo_conversational__core") == false)
                //   continue;
                //if (file.Name.Contains("battle_vo_orders__core") == false)
                //    continue;



                var soundDb = Bnkparser.Parse(file);
                var db = new ExtenededSoundDataBase(soundDb, nameHelper, referensedSounds, unknownObjectTypes);

                var events = soundDb.Hircs.Where(x => x.Type == HircType.Event).Cast<CAkEvent>().ToList();
                var dialogEvents = soundDb.Hircs.Where(x => x.Type == HircType.Dialogue_Event && x as CAkDialogueEvent != null).Cast<CAkDialogueEvent>().ToList();
                var itemsProcessed = 0;

                _logger.Here().Information($"{currentFile}/{files.Count} {file.Name} NumEvents:{events.Count} NumDialogEvents: {dialogEvents.Count}");

                var fileOutput = rootOutput.AddChild($"File: {file.Name} NumEvents: {events.Count} NumDialogEvents: {dialogEvents.Count}");

                foreach (var currentEvent in events)
                {
                    var name = nameHelper.GetName(currentEvent.Id); ;
                    var visualEvent = new VisualEvent(currentEvent, db, fileOutput, file.Name);

                    if (itemsProcessed % 100 == 0 && itemsProcessed != 0)
                        _logger.Here().Information($"\t{itemsProcessed}/{events.Count} events processsed [{timer.Elapsed.TotalSeconds}s]");
                    itemsProcessed++;
                }

                var dialogItemsProcessed = 0;
                foreach (var currentEvent in dialogEvents)
                {
                    var name = nameHelper.GetName(currentEvent.Id); ;
                    var visualEvent = new VisualEvent(currentEvent, db, fileOutput, file.Name);

                    if (dialogItemsProcessed % 100 == 0 && dialogItemsProcessed != 0)
                        _logger.Here().Information($"\t{dialogItemsProcessed}/{events.Count} dialogEvents processsed [{timer.Elapsed.TotalSeconds}s]");
                    dialogItemsProcessed++;
                }


                if (events.Count != 0)
                    _logger.Here().Information($"\t{itemsProcessed}/{events.Count} events processsed [{timer.Elapsed.TotalSeconds}s]");

                if (dialogEvents.Count != 0)
                    _logger.Here().Information($"\t{dialogItemsProcessed}/{dialogEvents.Count} dialogEvents processsed [{timer.Elapsed.TotalSeconds}s]");

                currentFile++;
            }


            var x = unknownObjectTypes.Distinct().Select(x => x + $"[{unknownObjectTypes.Count(unkObj => unkObj == x)}]");

            statsNode.AddChild($"Num bnk Files = {files.Count}");
            statsNode.AddChild($"Num wem Files = {numWemFiles.Count}");
            statsNode.AddChild($"References wem Files = {referensedSounds.Distinct().Count()}");
            statsNode.AddChild($"Unknown hirc types = {string.Join(",", x)}");

            VisualEventSerializer serializer = new VisualEventSerializer();
            var output = serializer.Start(rootOutput);



            //var str = rootOutput.GetDisplayStr();
            File.WriteAllText(@"C:\temp\SoundTesting\Warhammer2RippedEvents.txt", output);


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

            public List<uint> ReferensedSounds { get; set; }
            public List<string> UnknownObjectTypes { get; set; }


            public ExtenededSoundDataBase(SoundDataBase masterDb, NameLookupHelper nameHelper, List<uint> referensedSounds, List<string> unknownObjectType)
            {
                NameHelper = nameHelper;
                MasterDb = masterDb;

                Events = masterDb.Hircs.Where(x => x.Type == HircType.Event).Cast<CAkEvent>().ToList();
                Actions = masterDb.Hircs.Where(x => x.Type == HircType.Action).Cast<CAkAction>().Where(x => x.ActionType == ActionType.Play).ToList();
                Sounds = masterDb.Hircs.Where(x => x.Type == HircType.Sound).Cast<CAkSound>().ToList(); 
                Switches = masterDb.Hircs.Where(x => x.Type == HircType.SwitchContainer).Cast<CAkSwitchCntr>().ToList(); 
                RandomContainers = masterDb.Hircs.Where(x => x.Type == HircType.SequenceContainer).Cast<CAkRanSeqCnt>().ToList();
                ReferensedSounds = referensedSounds;
                UnknownObjectTypes = unknownObjectType;
            }

            public List<HricItem> GetHircObject(uint id, string bnkFile)
            {
                var res = MasterDb.Hircs.Where(x => x.Id == id && x.OwnerFile == bnkFile).ToList();
                //if (res.Count == 0)
                //    throw new Exception();
                return res;
            }

            public List<HricItem> GetHircObjects(List<uint> ids, string bnkFile)
            {
                var res = MasterDb.Hircs.Where(x => ids.Contains(x.Id) && x.OwnerFile == bnkFile).ToList();
                //if (res.Count == 0)
                //    throw new Exception();
                return res;
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
        }

        public class VisualEventSerializer
        {
            StringBuilder _builder;
            public string Start(VisualEventOutputNode root)
            {
                _builder = new StringBuilder();
                HandleNode(root, 0);
                return GetStr();
            }

            void HandleNode(VisualEventOutputNode node, int indentation)
            {
                var indentStr = string.Concat(Enumerable.Repeat('\t', indentation));
                _builder.AppendLine(indentStr + node.Data);

                foreach (var item in node.Children)
                {
                    HandleNode(item, indentation + 1);
                }
            }

            string GetStr()
            {
                return _builder.ToString();
            }

        }


        public class VisualEvent
        {
            string _ownerFileName;
            ExtenededSoundDataBase _db;
            string _name;
             
            public VisualEventOutputNode Output { get; set; }
            public bool ProcesedCorrectly { get; set; } = true;
            public VisualEvent(CAkEvent startEvent, ExtenededSoundDataBase db, VisualEventOutputNode rootOutput, string ownerFileName)
            {
                _ownerFileName = ownerFileName;
                _db = db;
                _name = _db.NameHelper.GetName(startEvent.Id);

                ProcessChild(startEvent, rootOutput);
            }

            public VisualEvent(CAkDialogueEvent startEvent, ExtenededSoundDataBase db, VisualEventOutputNode rootOutput, string ownerFileName)
            {
                _ownerFileName = ownerFileName;
                _db = db;
                _name = _db.NameHelper.GetName(startEvent.Id);

                ProcessChild(startEvent, rootOutput);
            }


            void ProcessChild(CAkEvent caEvent, VisualEventOutputNode currentNode)
            {
                var name  = _db.NameHelper.GetName(caEvent.Id);
                var eventNode = currentNode.AddChild($"-> Event:{name}[{caEvent.Id}]");
                if (Output == null)
                    Output = eventNode;

                var actionIdsForEvent = caEvent.Actions.Select(x => x.ActionId).ToList();

                var children = _db.GetHircObjects(actionIdsForEvent, caEvent.OwnerFile);
                foreach (var child in children)
                    ProcessGenericChild(child, eventNode);
            }


            void ProcessGenericChild(HricItem item, VisualEventOutputNode currentNode)
            {
                if (item is CAkAction action)
                    ProcessChild(action, currentNode);
                else if (item is CAkEvent caEvent)
                    ProcessChild(caEvent, currentNode);
                else if (item is CAkSound sound)
                    ProcessChild(sound, currentNode);
                else if (item is CAkSwitchCntr switchContainer)
                    ProcessChild(switchContainer, currentNode);
                else if (item is CAkRanSeqCnt randomContainer)
                    ProcessChild(randomContainer, currentNode);
                else if (item is CAkLayerCntr layeredControl)
                    ProcessChild(layeredControl, currentNode);
                else if (item is CAkDialogueEvent dialogEvent)
                    ProcessChild(dialogEvent, currentNode);
                else
                    ProcessUnknownChild(item, currentNode);
            }

            void ProcessChild(CAkAction item, VisualEventOutputNode currentNode)
            {
                var node = currentNode.AddChild($"CAkAction ActionType:[{item.ActionType}] \tId:[{item.Id}]");

                var actionRefs = _db.GetHircObject(item.SoundId, _ownerFileName);
                foreach (var actionRef in actionRefs)
                    ProcessGenericChild(actionRef, node);
            }

            void ProcessChild(CAkSound item, VisualEventOutputNode currentNode)
            {
                currentNode.AddChild($"CAkSound {item.BankSourceData.akMediaInformation.SourceId}.wem \tId:[{item.Id}] \tParentId:[{item.NodeBaseParams.DirectParentID}]");
                _db.ReferensedSounds.Add(item.BankSourceData.akMediaInformation.SourceId);
            }

            void ProcessChild(CAkSwitchCntr item, VisualEventOutputNode currentNode)
            {
                var node = currentNode.AddChild($"CAkSwitchCntr EnumGroup:[{_db.NameHelper.GetName(item.ulGroupID)}] \tDefault:[{_db.NameHelper.GetName(item.ulDefaultSwitch)}] \tId:[{item.Id}] \tParentId:[{item.NodeBaseParams.DirectParentID}]" );
                foreach (var switchCase in item.SwitchList)
               {
                    var switchCaseNode = node.AddChild($"SwitchValue [{_db.NameHelper.GetName(switchCase.SwitchId)}]");
                    foreach (var child in switchCase.NodeIdList)
                    {
                        var childRef = _db.GetHircObject(child, _ownerFileName);
                        Debug.Assert(childRef.Count() <= 1);

                        if (childRef.FirstOrDefault() != null)
                            ProcessGenericChild(childRef.FirstOrDefault(), switchCaseNode);
                    }
               }
            }

            void ProcessChild(CAkLayerCntr item, VisualEventOutputNode currentNode)
            {
                var node = currentNode.AddChild($"CAkLayerCntr \tId:[{item.Id}] \tParentId:[{item.NodeBaseParams.DirectParentID}]");
                foreach (var layer in item.LayerList)
                {
                    var switchCaseNode = node.AddChild($"LayerChildItem Id:[{layer.ulLayerID}] \trtpcID:[{_db.NameHelper.GetName(layer.rtpcID)}]");
                    foreach (var child in layer.CAssociatedChildDataList)
                    {
                        var childRef = _db.GetHircObject(child.ulAssociatedChildID, _ownerFileName);
                        Debug.Assert(childRef.Count() <= 1);

                        if (childRef.FirstOrDefault() != null)
                            ProcessGenericChild(childRef.FirstOrDefault(), switchCaseNode);
                    }
                }
            }

            void ProcessChild(CAkRanSeqCnt item, VisualEventOutputNode currentNode)
            {
                var node = currentNode.AddChild($"CAkRanSeqCnt \tId:[{item.Id}] \tParentId:[{item.NodeBaseParams.DirectParentID}]");

                foreach (var playListItem in item.AkPlaylist)
                {
                    var playListRefs = _db.GetHircObject(playListItem.PlayId, _ownerFileName);
                    Debug.Assert(playListRefs.Count() <= 1);

                    if(playListRefs.FirstOrDefault() != null)
                        ProcessGenericChild(playListRefs.FirstOrDefault(), node);
                }
            }

            void ProcessChild(CAkDialogueEvent item, VisualEventOutputNode currentNode)
            {
                var name = _db.NameHelper.GetName(item.Id);
                var node = currentNode.AddChild($"-> DialogEvent:{name} \tId:[{item.Id}]");

                // arguments id and name

                foreach(var child in item.AkDecisionTree.Root.Children)
                    ProcessAkDecisionTreeNode(child, node);
            }


            void ProcessAkDecisionTreeNode(AkDecisionTree.Node node, VisualEventOutputNode currentOutputNode)
            {
                var name = _db.NameHelper.GetName(node.key);
                var outputNode = currentOutputNode.AddChild($"DialogNode {name} Id:[{node.key}]");

                foreach (var childNode in node.Children)
                    ProcessAkDecisionTreeNode(childNode, outputNode);

                foreach (var childNode in node.SoundNodes)
                {
                    var childNodeName = _db.NameHelper.GetName(childNode.key);
                    var soundChildNode = outputNode.AddChild($"Sound_Node {childNodeName}  Id:[{childNode.key}] AudioNodeId:[{childNode.audioNodeId}]");
                    
                    var nodes = _db.GetHircObject(childNode.audioNodeId, _ownerFileName);
                    if (nodes.FirstOrDefault() != null)
                        ProcessGenericChild(nodes.FirstOrDefault(), soundChildNode);
                }
            }

            void ProcessUnknownChild(HricItem item, VisualEventOutputNode currentNode)
            {
                currentNode.AddChild($"Unknown HricItem Type:[{item.Type}] \tId:[{item.Id}] ");
                ProcesedCorrectly = false;
                _db.UnknownObjectTypes.Add(item.Type.ToString());
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


