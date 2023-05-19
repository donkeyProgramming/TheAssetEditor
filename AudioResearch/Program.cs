using Action = System.Action;
using Audio.FileFormats.WWise.Hirc.V136;
using Audio.Storage;
using Audio.Utility;
using CommonControls.Common;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using MoreLinq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using static CommonControls.Common.CustomExtensions;
using Audio.FileFormats.WWise;
using Microsoft.Xna.Framework.Media;
using Microsoft.WindowsAPICodePack.ApplicationServices;
using System.Windows.Navigation;
using Audio.FileFormats.WWise.Hirc;
using SharpDX.Direct3D9;
using static CommonControls.Editors.AnimationPack.Converters.AnimationBinWh3FileToXmlConverter;
using Audio.AudioEditor;
using static System.ComponentModel.Design.ObjectSelectorEditor;
using FluentValidation.Results;
using SharpDX.X3DAudio;
using SharpDX.Direct3D11;
using System.Windows.Forms.VisualStyles;
using System.Runtime.InteropServices.WindowsRuntime;
using SharpDX.DXGI;
using System.Transactions;
using System.Threading.Tasks;

namespace AudioResearch
{
    class Program
    {
        static void Main(string[] args)
        {
            if (Environment.GetEnvironmentVariable("KlissanEnv") != null)
            {
                TestDialogEventSerialization();
                return;
            }

            DataExplore();
            //TableTest();
            //OvnTest.GenerateProjectFromBnk(false);

            // OvnTest.Compile();
            //GeneratOvnProject();
            TestDialogEventSerialization();
            // LogicalChainingTest();

            var currentProjectName = $"Data\\OvnExample\\ProjectSimple.json";
            //OvnTest.GenerateProjectFromBnk(currentProjectName);

            OvnTest.Compile(currentProjectName, false, false, false);
        }

        class AudioBusTreeBuilder
        {
            public void DoWork(IAudioRepository audioRepo)
            {
                var busses = audioRepo.HircObjects
                    .SelectMany(x => x.Value)
                    .DistinctBy(x => x.Id)
                    .Where(x => x.Type == Audio.FileFormats.WWise.HircType.Audio_Bus)
                    .Cast<CAkBus_v136>()
                    .ToList();


                var rootItems = new List<TreeBusItem>();
                var roots = busses.Where(x => x.OverrideBusId == 0).ToList();
                roots.ForEach(x => rootItems.Add(new TreeBusItem(x, audioRepo)));

                foreach (var item in roots)
                    busses.Remove(item);

                while (busses.Count != 0)
                {
                    var toDelete = new List<CAkBus_v136>();
                    foreach (var bus in busses)
                    {

                        var parent = FindParent(rootItems, bus.OverrideBusId);
                        if (parent != null)
                            toDelete.Add(bus);

                        parent.Children.Add(new TreeBusItem(bus, audioRepo));
                    }

                    foreach (var item in toDelete)
                        busses.Remove(item);
                }

                Print(rootItems);
            }

            TreeBusItem FindParent(List<TreeBusItem> tree, uint overrrideId)
            {
                foreach (var item in tree)
                {
                    if (item.Id == overrrideId)
                        return item;

                    var res = FindParent(item.Children, overrrideId);
                    if (res != null)
                        return res;
                }

                return null;
            }

            void Print(List<TreeBusItem> tree, int indentation = 0)
            {
                foreach (var item in tree)
                {
                    var displayName = $"{item.Id}";
                    if (item.HasName)
                        displayName = $"{item.Name}: {displayName}";
                    Console.WriteLine($"{new string('\t', indentation)}{displayName}");

                    Print(item.Children, indentation + 1);
                }
            }

            class TreeBusItem
            {
                public TreeBusItem(CAkBus_v136 item, IAudioRepository repo)
                {
                    Id = item.Id;
                    OverrideBusId = item.OverrideBusId;
                    Name = repo.GetNameFromHash(Id, out var nameFound);
                    HasName = nameFound;
                }

                public uint Id { get; set; }
                public uint OverrideBusId { get; set; }
                public string Name { get; set; }
                public bool HasName { get; set; }
                public List<TreeBusItem> Children { get; set; } = new List<TreeBusItem>();

            }

        }


        class EventBusBuilder
        {
            public void DoWork(IAudioRepository audioRepository)
            {
                var events = audioRepository.HircObjects
                    .SelectMany(x => x.Value)
                    .DistinctBy(x => x.Id)
                    .Where(x => x.Type == HircType.Event)
                    .Cast<CAkEvent_v136>()
                    .ToList();

                var eventToSoundsMap = events
                   .Select(x => new 
                   { 
                       Event = x, 
                       EventName = audioRepository.GetNameFromHash(x.Id), 
                       Sound = ParseChildren(x.OwnerFile, x.GetActionIds(), audioRepository).Where(x=>x != null).ToList()
                   })
                   .ToList();


                var eventToBus = eventToSoundsMap
                    .Select(x => new
                    {
                        EventName = x.EventName,
                        SoundCount = x.Sound.Count(),
                        OverrideBusIds = x.Sound.Select(x => x.NodeBaseParams.OverrideBusId).Distinct().ToList(),
                        OverrideBusIdsEqual = x.Sound.Select(x => x.NodeBaseParams.OverrideBusId).Distinct().Count() == 1
                    })
                    .Where(x=>x.SoundCount != 0)
                    .ToList();

                var allSameBus = eventToBus.Where(x=>x.OverrideBusIdsEqual == true).ToList();
                var allNotSameBus = eventToBus.Where(x => x.OverrideBusIdsEqual == false).ToList();

                var busToEventMap = eventToBus
                    .SelectMany(@event => @event.OverrideBusIds.Select(bus => new { EventName = @event.EventName, Bus = bus }))
                    .ToList();
            }

            private List<CAkSound_v136> ParseChildren(string ownerFile, List<uint> list, IAudioRepository audioRepository)
            {
                if(list.Count  == 0) 
                    return new List<CAkSound_v136>();

                var result = list
                    .Select(x => audioRepository.GetHircObject(x, ownerFile))
                    .SelectMany(x=>x)
                    .ToList();

                if (result.Count == 0)
                    return new List<CAkSound_v136>();

                var output = new List<CAkSound_v136>();
                foreach(var item in result)
                {
                    if (item is CAkSound_v136 sound)
                        output.Add(sound);

                    else if (item is CAkAction_v136 action)
                    {
                        if (action.ActionType == ActionType.Play)
                            output.AddRange(ParseChildren(ownerFile, new List<uint> { action.GetChildId() }, audioRepository));
                    }

                    else if (item is CAkRanSeqCntr_v136 cakRand)
                        output.AddRange(ParseChildren(ownerFile, cakRand.GetChildren(), audioRepository));

                    else if (item is CAkLayerCntr_v136 cakLayer)
                        output.AddRange(ParseChildren(ownerFile, cakLayer.Children.ChildIdList, audioRepository));

                    else if (item is CAkSwitchCntr_v136 cakSwitch)
                        output.AddRange(ParseChildren(ownerFile, cakSwitch.Children.ChildIdList, audioRepository));

                    else if (item is CAkMusicRanSeqCntr_v136)
                    { }

                    else if (item is CAkMusicSegment_v136)
                    { }

                    else if (item is CAkMusicTrack_v136)
                    { }

                    else if (item is CAkMusicSwitchCntr_v136)
                    { }

                    else if (item is CAkEvent_v136)
                    { }

                    else
                        throw new NotImplementedException();
                }

                return output;
            }

            CAkEvent_v136 FindRootParent(uint parentID, IAudioRepository audioRepository)
            {
                var result = audioRepository.GetHircObject(parentID)
                    .DistinctBy(x => x.Id)
                    .ToList();

                if (result.Count != 1)
                    throw new Exception("Not expected!");
                
                var instance = result.First();
                if (instance is CAkEvent_v136 cakEvent)
                    return cakEvent;

                if (instance is CAkRanSeqCntr_v136 cakRand)
                    return FindRootParent(cakRand.NodeBaseParams.DirectParentID, audioRepository);
                if (instance is CAkLayerCntr_v136 cakLayer)
                    return FindRootParent(cakLayer.NodeBaseParams.DirectParentID, audioRepository);
                if (instance is CAkSwitchCntr_v136 cakSwitch)
                    return FindRootParent(cakSwitch.NodeBaseParams.DirectParentID, audioRepository);
                if (instance is CAkActorMixer_v136 cakMixer)
                    return null;//FindRootParent(cakMixer.NodeBaseParams.DirectParentID, audioRepository);

                throw new Exception("Not expected!");
                //return FindRootParent(parentID, audioRepository);   
            }
        }



        static void DataExplore()
        {
            using var application = new SimpleApplication();

            var pfs = application.GetService<PackFileService>();
            pfs.LoadAllCaFiles(GameTypeEnum.Warhammer3);

            //var helper = application.GetService<AudioResearchHelper>();
            //helper.GenerateActorMixerTree();

            var audioRepo = application.GetService<IAudioRepository>();
            //new EventBusBuilder().DoWork(audioRepo);
            //new AudioBusTreeBuilder().DoWork(audioRepo);

            var sounds = audioRepo.HircObjects
                .SelectMany(x => x.Value)
                .DistinctBy(x => x.Id)
                .Where(x => x.Type == HircType.Sound)
                .Cast<CAkSound_v136>()
                .ToList();


            var counter = 1;
            var totalCount = sounds.Count;
            var helper = new FindAudioParentStructureHelper(audioRepo);
            Parallel.ForEach(sounds, sound => 
            {
                counter++;
                helper.Compute(sound, audioRepo);
            
            
              if(counter % 1000 == 0)
                Console.WriteLine($"{counter}/{totalCount}");
            });

            //foreach (var sound in sounds)
            //{
            //    helper.Compute(sound, audioRepo);
            //    counter++;
            //      if (counter % 1000 == 0)
            //        Console.WriteLine($"{counter}/{totalCount}");
            //}

        }

        
        public class FindAudioParentStructureHelper
        {
            List<CAkAction_v136> _allActions;
            List<CAkEvent_v136> _AllEvents;
            public FindAudioParentStructureHelper(IAudioRepository audioRepository)
            {
                _allActions = audioRepository.HircObjects
                    .SelectMany(x => x.Value)
                    .DistinctBy(x => x.Id)
                    .Where(x => x.Type == HircType.Action)
                    .Cast<CAkAction_v136>()
                    .Where(x => x.ActionType == ActionType.Play)
                    .DistinctBy(x => x.idExt)
                    .ToList();


                _AllEvents = audioRepository.HircObjects
                    .SelectMany(x => x.Value)
                    .DistinctBy(x => x.Id)
                    .Where(x => x.Type == HircType.Event)
                    .Cast<CAkEvent_v136>()
                    .ToList();
            }

            public class ParentStructure
            { 
                public string Description { get; set; }
                public List<string> GraphItems { get; set; } = new List<string>();
            }

            class BusItem
            { 
                public string SourceDescription { get; set; }
                public uint BusId { get; set; }
            }

            public void DebugPrint(List<ParentStructure> parentStructures)
            {
                foreach(var  parentStructure in parentStructures) 
                {
                    Console.WriteLine(parentStructure.Description);
                    foreach(var item in  parentStructure.GraphItems)
                        Console.WriteLine("\t"+ item);
                    Console.WriteLine();
                }
            }


            string GetEventNameForSound(List<HircTreeItem> soundHierarchyAsFlatList, IAudioRepository audioRepository)
            {
                foreach (var node in soundHierarchyAsFlatList)
                {
                    uint itemId = 0;
                    if (node.Item is CAkSound_v136)
                        itemId = node.Item.Id;

                    else if (node.Item is CAkRanSeqCntr_v136)
                        itemId = node.Item.Id;

                    else if (node.Item is CAkLayerCntr_v136)
                        itemId = node.Item.Id;

                    else if (node.Item is CAkSwitchCntr_v136 )
                        itemId = node.Item.Id;






                    // Handle music,

                    if (itemId != 0)
                    {
                        var eventName = GetEventName(itemId, audioRepository, out var found);
                        if (found == true)
                            return eventName;
                        else
                        { 
                        
                        }

                    }
                }

                return "test";
            }

            string GetEventName(uint idBeforeAction, IAudioRepository audioRepository, out bool found)
            {
                found = false;
                var actionParents = _allActions
                    .Where(x => x.idExt == idBeforeAction)
                    .ToList();

                if (actionParents.Count() != 1)
                {
                    Console.WriteLine("actionParents.Count() != 1 : " + actionParents.Count());

                    //var act = audioRepository.HircObjects
                    //    .SelectMany(x => x.Value)
                    //    .DistinctBy(x => x.Id)
                    //    .Where(x => x.Type == HircType.Action)
                    //    .Cast<CAkAction_v136>()
                    //    .Where(x => x.idExt == idBeforeAction)
                    //    .ToList();

                }


                if (actionParents.Count() == 1)
                {
                    var parentEvent = _AllEvents
                        .Where(x => x.Actions.Select(a => a.ActionId).Contains(actionParents.First().Id))
                        .ToList();

                    if (parentEvent.Count() != 1)
                    {
                        Console.WriteLine("parentEvent.Count() != 1 : " + parentEvent.Count());
                    }

                    found = true;
                    return audioRepository.GetNameFromHash(parentEvent.First().Id, out var nameFound);
                }

                Console.WriteLine("Not Found");
                return "not found";
            }

            ParentStructure GetAudioParentStructure(CAkSound_v136 sound, IAudioRepository audioRepository, out List<BusItem> busses)
            {
                busses = new List<BusItem>();
                var output = new ParentStructure()
                {
                    Description = "Graph structure:"
                };

                var parser = new WWiseTreeParserParent(audioRepository, true, true, true);
                var nodes = parser.BuildHierarchyAsFlatList(sound);

                GetEventNameForSound(nodes, audioRepository);
                return output;
                nodes.Reverse();

                foreach (var node in nodes)
                {
                    var busInfo = "";
                    if (node.Item is CAkActorMixer_v136 mixerInstance && mixerInstance.NodeBaseParams.OverrideBusId != 0)
                    {
                        busInfo = $" - With AudioBus [{mixerInstance.NodeBaseParams.OverrideBusId}]";
                        busses.Add(new BusItem() { SourceDescription = $"{node.Item.Type}[{node.Item.Id}]", BusId = mixerInstance.NodeBaseParams.OverrideBusId });
                    }

                    else if (node.Item is CAkSound_v136 soundInstance && soundInstance.NodeBaseParams.OverrideBusId != 0)
                    {
                        busInfo = $" - With AudioBus [{soundInstance.NodeBaseParams.OverrideBusId}]";
                        busses.Add(new BusItem() { SourceDescription = $"{node.Item.Type}[{node.Item.Id}]", BusId = soundInstance.NodeBaseParams.OverrideBusId });
                    }

                    else if (node.Item is CAkRanSeqCntr_v136 randInstance && randInstance.NodeBaseParams.OverrideBusId != 0)
                    {
                        busInfo = $" - With AudioBus [{randInstance.NodeBaseParams.OverrideBusId}]";
                        busses.Add(new BusItem() { SourceDescription = $"{node.Item.Type}[{node.Item.Id}]", BusId = randInstance.NodeBaseParams.OverrideBusId });
                    }

                    else if (node.Item is CAkLayerCntr_v136 layerInstance && layerInstance.NodeBaseParams.OverrideBusId != 0)
                    {
                        busInfo = $" - With AudioBus [{layerInstance.NodeBaseParams.OverrideBusId}]";
                        busses.Add(new BusItem() { SourceDescription = $"{node.Item.Type}[{node.Item.Id}]", BusId = layerInstance.NodeBaseParams.OverrideBusId });
                    }

                    else if (node.Item is CAkSwitchCntr_v136 switchInstance && switchInstance.NodeBaseParams.OverrideBusId != 0)
                    {
                        busInfo = $" - With AudioBus [{switchInstance.NodeBaseParams.OverrideBusId}]";
                        busses.Add(new BusItem() { SourceDescription = $"{node.Item.Type}[{node.Item.Id}]", BusId = switchInstance.NodeBaseParams.OverrideBusId });
                    }

                    var str = $"{node.Item.Type}[{node.Item.Id}]{busInfo}";
                    output.GraphItems.Add(str);
                }

                return output;


            }

            List<ParentStructure> GetBusParentStructure(IAudioRepository audioRepository, List<BusItem> busItems)
            {
                var output = new List<ParentStructure>();

                foreach (var currentBusItem in busItems)
                {
                    output.Add(new ParentStructure() { Description = $"AudioBus graph for {currentBusItem.SourceDescription}:" });

                    var firstBus = audioRepository.GetHircObject(currentBusItem.BusId)
                        .Where(x => x.Type == HircType.Audio_Bus)
                        .Cast<CAkBus_v136>()
                        .First();

                    var item = firstBus;
                    while (item.OverrideBusId != 0)
                    {
                        item = audioRepository.GetHircObject(item.OverrideBusId)
                                .Where(x => x.Type == HircType.Audio_Bus)
                                .Cast<CAkBus_v136>()
                                .First();

                        var name = audioRepository.GetNameFromHash(item.Id, out var found);
                        if (found == false)
                            name = "";
                        var str = $"{name}[{item.Id}]";
                        output.Last().GraphItems.Add(str);
                    }
                }
            
                return output;
            }



            public List<ParentStructure> Compute(CAkSound_v136 sound, IAudioRepository audioRepository)
            {
                var output = new List<ParentStructure>();
                output.Add(GetAudioParentStructure(sound, audioRepository, out var overrideBusIds));
                //output.AddRange(GetBusParentStructure(audioRepository, overrideBusIds));

                //DebugPrint(output);
                return output;
             }
        }







            static void TableTest()
        {
            using var application = new SimpleApplication();

            var pfs = application.GetService<PackFileService>();
            pfs.CreateNewPackFileContainer("WwiseData", PackFileCAType.MOD, true);
            var loadedFiles = PackFileUtil.LoadFilesFromDisk(pfs, new[]
            {
                new PackFileUtil.FileRef( packFilePath: @"audio\wwise", systemPath:@"D:\Research\Audio\Warhammer3Data\battle_vo_conversational__core.bnk"),
                new PackFileUtil.FileRef( packFilePath: @"audioProjectExport", systemPath:@"D:\Research\Audio\Warhammer3Data\AudioNames.wwiseids")
            });

            var audioRepo = application.GetService<IAudioRepository>();

            // Load
            var dialogEvent = audioRepo.GetHircObject(263775993).First() as CAkDialogueEvent_v136;  // battle_vo_conversation_own_unit_under_ranged_attack


            // Update
            // Sort ids
            // Get as bytes
            // Load whole bnk with each item as byte item
            // Replace the item
            // Save

            // Question:
            // Can all referenced things be in an other bnk? Check

            var audioResearchHelper = application.GetService<AudioResearchHelper>();
            //audioResearchHelper.ExportDialogEventsToFile(dialogEvent);

            var manipulator = new BnkFileManipulator();
            var hirc = manipulator.FindHirc(loadedFiles.First(), "myBnk.bnk", 263775993);



            // Load and update - dialogEvent
            // Combine
            // Insert back into




        }




        static void TestDialogEventSerialization()
        {
            using var application = new SimpleApplication();

            var pfs = application.GetService<PackFileService>();
            pfs.LoadAllCaFiles(GameTypeEnum.Warhammer3);
            var audioRepo = application.GetService<IAudioRepository>();

            Func<uint, string> unHash = audioRepo.GetNameFromHash;
            string DEFAULT_KEYWORD = "DEFAULT";
            Func<uint, string> unHashSpecial = h =>
            {
                var x = audioRepo.GetNameFromHash(h);
                return x == "0" ? DEFAULT_KEYWORD : x;
            };


            var deTypes = new ArrayList();

            var dialogEvents = audioRepo.GetAllOfType<CAkDialogueEvent_v136>();

            dialogEvents.ForEach(e => e.AkDecisionTree.VerifyState());

            var modes = dialogEvents.GroupBy(e => e.uMode);
            Console.WriteLine($"Modes:");
            modes.ForEach(e => Console.WriteLine($"{(e.Key, e.Count())}"));
            modes.First(e => e.Key == 1)
                .ForEach(e => Console.WriteLine($"\t{audioRepo.GetNameFromHash(e.Id)}, {e.AkDecisionTree.NodeCount()}"));

            Console.WriteLine("uWeight != 50:");
            dialogEvents.ForEach(
                e =>
                {
                    var nodes = new List<AkDecisionTree.Node>();
                    e.AkDecisionTree.BfsTreeTraversal(
                        node => If(node.Content.uWeight != 50).Then(_ => nodes.Add(node))
                    );
                    If(nodes.Count > 0).Then(_ =>
                        Console.WriteLine($"\t{unHash(e.Id)}: {String.Join(", ", nodes.Select(e => (unHash(e.Content.Key), e.Content.uWeight)))}")
                    );
                }
            );

            Console.WriteLine("uProbability != 100:");
            dialogEvents.ForEach(
                e =>
                {
                    var nodes = new List<AkDecisionTree.Node>();
                    e.AkDecisionTree.BfsTreeTraversal(
                        node => If(node.Content.uProbability != 100).Then(_ => nodes.Add(node))
                    );
                    If(nodes.Count > 0).Then(_ =>
                        Console.WriteLine($"\t{unHash(e.Id)}: {String.Join(", ", nodes.Select(e => (unHash(e.Content.Key), e.Content.uProbability)))}")
                    );
                }
            );


            Console.WriteLine("uWeight != 50 && uProbability != 100:");
            dialogEvents.ForEach(
                e =>
                {
                    var nodes = new List<AkDecisionTree.Node>();
                    e.AkDecisionTree.BfsTreeTraversal(
                        node => If(node.Content.uWeight != 50 && node.Content.uProbability != 100).Then(_ => nodes.Add(node))
                    );
                    If(nodes.Count > 0).Then(_ =>
                        Console.WriteLine($"\t{unHash(e.Id)}: {String.Join(", ", nodes.Select(e => (unHash(e.Content.Key), e.Content.uWeight, e.Content.uProbability)))}")
                    );
                }
            );

            Console.WriteLine("Actual depth != _maxDepth:");
            dialogEvents.ForEach(
                e => If(e.AkDecisionTree.Depth() != e.AkDecisionTree._maxTreeDepth)
                    .Then(_ => Console.WriteLine($"\t{unHash(e.Id)}: {e.AkDecisionTree.Depth()}/{e.AkDecisionTree._maxTreeDepth}"))
            );

            Console.WriteLine("Depths of the leaves:");
            dialogEvents.ForEach(
                e =>
                {
                    var values = new HashSet<int>();
                    e.AkDecisionTree.BfsTreeTraversal(
                        (node, depth) => If(node.Children.Count == 0 && depth != e.AkDecisionTree._maxTreeDepth)
                            .Then(_ => values.Add(depth))
                    );
                    If(values.Count > 0).Then(_ =>
                        Console.WriteLine($"\t{unHash(e.Id)}(nc={e.AkDecisionTree.NodeCount()})(d={e.AkDecisionTree._maxTreeDepth}): {String.Join(", ", values)}")
                    );
                }
            );



            /////////////////////////////////////////////////////////////
            /// CSVs
            ///

            var exts = new Dictionary<char, string>();
            exts.Add(',', ".csv");
            exts.Add('\t', ".tsv");

            var ext2sep = new Dictionary<string, char>();
            exts.ForEach(kv => ext2sep.Add(kv.Value, kv.Key));

            (string, string) DumpAsXsv(CAkDialogueEvent_v136 dialogEvent, char sep = '\t', bool dumpRoot = false, bool dumpDecisionNodesFull = false)
            {
                if (sep != ',' && sep != '\t')
                {
                    throw new ArgumentException();
                }

                int idx = dumpDecisionNodesFull ? 3 : 1;

                List<string> args = null;
                if (!dumpDecisionNodesFull)
                {
                    args = dialogEvent.ArgumentList.Arguments.Select(x => unHash(x.ulGroupId)).ToList();
                }
                else
                {
                    args = new List<string>();
                    dialogEvent.ArgumentList.Arguments.ForEach(x =>
                    {
                        var name = unHash(x.ulGroupId);
                        args.Add(name);
                        args.Add($"{name}:Probability");
                        args.Add($"{name}:Weight");
                    });
                }

                If(!dumpRoot).Then(_ =>  //do not dump the root node
                    args = args.GetRange(idx, args.Count - idx));
                args.Add("AudioNode:Key");
                args.Add("AudioNode:Probability");
                args.Add("AudioNode:Weight");
                args.Add("AudioNode:Id");
                var header = string.Join(sep, args);
                var paths = dialogEvent.AkDecisionTree.GetDecisionPaths();
                var pathStrs = paths.Select(e =>
                {
                    List<string> keys = null;
                    if (!dumpDecisionNodesFull)
                    {
                        keys = e.Item1.Select(n => unHashSpecial(n.Key)).ToList();
                    }
                    else
                    {
                        keys = new List<string>();
                        e.Item1.ForEach(n =>
                        {
                            keys.Add(unHashSpecial(n.Key));
                            keys.Add(n.uProbability.ToString());
                            keys.Add(n.uWeight.ToString());
                        });
                    }
                    If(!dumpRoot).Then(_ =>  //do not dump the root node
                        keys = keys.GetRange(idx, keys.Count - idx));
                    var audioNode = e.Item1[^1];
                    if (!dumpDecisionNodesFull)
                    {
                        keys.Add(audioNode.uProbability.ToString());
                        keys.Add(audioNode.uWeight.ToString());
                    }
                    keys.Add(e.Item2.ToString());
                    return string.Join(sep, keys);
                }).ToList();
                pathStrs.Insert(0, header);
                var opath = $"F:\\dump\\{unHash(dialogEvent.Id)}{exts[sep]}";
                var contents = string.Join('\n', pathStrs);
                File.WriteAllText(opath, contents);
                return (opath, contents);
            }

            //TODO: checks for input values
            AkDecisionTree ReadFromXsv(string fpath, bool hasRoot = false, bool hasDecisionNodesFull = false)
            {
                //TODO verify header
                var fname = Path.GetFileNameWithoutExtension(fpath);
                var ext = Path.GetExtension(fpath);
                var sep = ext2sep[ext];
                var content = File.ReadAllText(fpath).Split('\n').ToList();
                var header = content[0];
                content = content.GetRange(1, content.Count - 1).ToList();
                var colNames = header.Split(sep);

                var fnameHash = WWiseHash.Compute(fname);
                var dialogEvent = dialogEvents.Find(e => e.Id == fnameHash);
                if (dialogEvent is null)
                {
                    throw new ArgumentException($"Game has no dialogueEvent with the following name: {fname}");
                }

                var treeCopy = dialogEvent.AkDecisionTree.BaseCopy();
                var nodeChainLength = treeCopy._maxTreeDepth;
                if (hasRoot)
                {
                    throw new ArgumentException("Root is not supported in the import as it copied from the reference tree");
                    // nodeChainLength += 1;    
                }
                content.ForEach((e, ln) =>
                {
                    var strings = e.Split(sep);
                    var nodes = new List<AkDecisionTree.NodeContent>();
                    var step = hasDecisionNodesFull ? 3 : 1;
                    For(0, nodeChainLength * step, step, i =>
                    {
                        uint key = 0;
                        try
                        {
                            key = uint.Parse(strings[i]);
                            Console.WriteLine($"WARNING: File {fpath} contains numeric Key ({strings[i]}), line {ln + 1}");
                        }
                        catch
                        {
                            key = strings[i] == DEFAULT_KEYWORD ? 0 : WWiseHash.Compute(strings[i]);
                        }

                        AkDecisionTree.NodeContent node;
                        if (hasDecisionNodesFull)
                        {
                            var uProbability = ushort.Parse(strings[i + 1]);
                            var uWeight = ushort.Parse(strings[i + 2]);
                            node = new AkDecisionTree.NodeContent(key, uWeight, uProbability);
                        }
                        else
                        {
                            if (i == nodeChainLength - step)
                            {
                                //the last one
                                var uProbability = ushort.Parse(strings[^3]);
                                var uWeight = ushort.Parse(strings[^2]);
                                node = new AkDecisionTree.NodeContent(key, uWeight, uProbability);
                            }
                            else
                            {
                                node = new AkDecisionTree.NodeContent(key);
                            }
                        }

                        nodes.Add(node);
                    });
                    var audioNodeId = uint.Parse(strings[^1]);
                    treeCopy.AddAudioNode(nodes, audioNodeId);
                });
                return treeCopy;
            }


            // DumpAsXsv(dialogEvents[0], dumpRoot:false, dumpDecisionNodesFull:false);
            // var tree = ReadFromXsv("F:\\dump\\Battle_Individual_Melee_Weapon_Hit.tsv");
            // dialogEvents[0].AkDecisionTree = tree;
            // DumpAsXsv(dialogEvents[0], dumpRoot:false, dumpDecisionNodesFull:false);
            dialogEvents.ForEach(e =>
            {
                var WeirdOnes = new string[] // TODO AUTOCORRECTION FOR THESE One
                {
                    "battle_vo_order_guard_on",
                    "battle_vo_order_climb",
                    "battle_vo_order_pick_up_engine",
                    "battle_vo_order_move_siege_tower",
                    "battle_vo_order_change_ammo",
                    "battle_vo_order_fire_at_will_on",
                    "battle_vo_order_short_order",
                    "battle_vo_order_formation_lock",
                    "battle_vo_order_fire_at_will_off",
                    "battle_vo_order_man_siege_tower",
                    "battle_vo_order_move_ram",
                    "battle_vo_order_melee_off",
                    "battle_vo_order_attack_alternative",
                    "battle_vo_order_melee_on",
                    "battle_vo_order_formation_unlock"
                };

                var (path, before) = DumpAsXsv(e);
                foreach (var wo in WeirdOnes)
                {
                    if (path.Contains(wo))
                    {
                        return;
                    }
                }
                var tree = ReadFromXsv(path);
                e.AkDecisionTree = tree;
                var (_, after) = DumpAsXsv(e);
                Console.WriteLine(path);
                var beforeLines = before.Split('\n');
                var afterLines = after.Split('\n');
                if (beforeLines.Length != afterLines.Length)
                {
                    Console.WriteLine("NotEqual size!!!");
                }
                For(beforeLines.Length, i =>
                {
                    if (beforeLines[i] != afterLines[i])
                    {
                        Console.WriteLine($"LINE #{i}");
                        Console.WriteLine(beforeLines[i]);
                        Console.WriteLine(afterLines[i]);
                    }
                });
                Debug.Assert(before == after);
            });
            return;
            foreach (var dialogEvent in dialogEvents)
            {
                // dialogEvent.AkDecisionTree.VerifyState();
                // var bytes = dialogEvent.GetAsByteArray();
                // var chuck = new Filetypes.ByteParsing.ByteChunk(bytes);
                // var reParsedObject = new CAkDialogueEvent_v136();
                // reParsedObject.Parse(chuck);
                // reParsedObject.AkDecisionTree.VerifyState();
                // Debug.Assert(dialogEvent.AkDecisionTree.Flatten());

                deTypes.Add(dialogEvent.Type);


                // dialogEvent.AkDecisionTree.BfsTreeTraversal(CheckWeightAndProb);
                dialogEvent.AkDecisionTree.BfsTreeTraversal(node =>
                {
                    if (node.AudioNodeId == 0 && node.Children.Count == 0)
                    {
                        Console.WriteLine($"Weird Node ({audioRepo.GetNameFromHash(node.Content.Key)}): {audioRepo.GetNameFromHash(dialogEvent.Id)}({dialogEvent.Id}) | nodeCount: {dialogEvent.AkDecisionTree.NodeCount()}");
                    }
                });
                // Console.WriteLine($"Main.Success: {audioRepo.GetNameFromHash(dialogEvent.Id)}({dialogEvent.Id})");
            }

        }
    }
}
