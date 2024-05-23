using AssetEditor;
using Audio.FileFormats.WWise;
using Audio.FileFormats.WWise.Hirc.V136;
using Audio.Storage;
using Audio.Utility;
using SharedCore.PackFiles;
using SharedCore.PackFiles.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AudioResearch
{
    partial class Program
    {
        static void Main(string[] args)
        {
            new LotrDataLoading().Run();
            return;

            //DecisionPathHelper test = new DecisionPathHelper(null);
            //test.Write();
            //
            //if (Environment.GetEnvironmentVariable("KlissanEnv") != null)
            //{
            //    //TestDialogEventSerialization();
            //    return;
            //}
            //
            //
            //
            //DataExplore();
            ////TableTest();
            ////OvnTest.GenerateProjectFromBnk(false);
            //
            //// OvnTest.Compile();
            ////GeneratOvnProject();
            /// TestDialogEventSerialization();
            //// LogicalChainingTest();
            //
            //var currentProjectName = $"Data\\OvnExample\\ProjectSimple.json";
            ////OvnTest.GenerateProjectFromBnk(currentProjectName);
            //
            //OvnTest.Compile(currentProjectName, false, false, false);
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
                       Sound = ParseChildren(x.OwnerFile, x.GetActionIds(), audioRepository).Where(x => x != null).ToList()
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
                    .Where(x => x.SoundCount != 0)
                    .ToList();

                var allSameBus = eventToBus.Where(x => x.OverrideBusIdsEqual == true).ToList();
                var allNotSameBus = eventToBus.Where(x => x.OverrideBusIdsEqual == false).ToList();

                var busToEventMap = eventToBus
                    .SelectMany(@event => @event.OverrideBusIds.Select(bus => new { EventName = @event.EventName, Bus = bus }))
                    .ToList();
            }

            private List<CAkSound_v136> ParseChildren(string ownerFile, List<uint> list, IAudioRepository audioRepository)
            {
                if (list.Count == 0)
                    return new List<CAkSound_v136>();

                var result = list
                    .Select(x => audioRepository.GetHircObject(x, ownerFile))
                    .SelectMany(x => x)
                    .ToList();

                if (result.Count == 0)
                    return new List<CAkSound_v136>();

                var output = new List<CAkSound_v136>();
                foreach (var item in result)
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
            using var application = new SimpleApplication()
            {
                SkipLoadingWemFiles = true,
            };

            //var helper = application.GetService<AudioResearchHelper>();
            //helper.GenerateActorMixerTree();

            var audioRepo = application.GetService<IAudioRepository>();
            //new EventBusBuilder().DoWork(audioRepo);
            //new AudioBusTreeBuilder().DoWork(audioRepo);


            //var myDialog = audioRepo.GetHircObject(674874555).First() as CAkDialogueEvent_v136;
            //var worker = new DecisionPathHelper();
            //var lines = worker.CreateLines(myDialog, audioRepo);


            var x = audioRepo.HircObjects.SelectMany(x => x.Value).Where(x => x.Type == HircType.State).ToList();

            var tests = audioRepo.GetHircObject(964666289);

            var allBusses = audioRepo.HircObjects
             .SelectMany(x => x.Value)
             .Where(x => x.Type == HircType.Audio_Bus || x.Type == HircType.AuxiliaryBus)
             //.DistinctBy(x => x.Id)
             .Cast<CAkBus_v136>()
             .ToList();


            var allBussesStateGroupIDs = allBusses
                .SelectMany(x => x.StateChunk.StateChunks.Select(y => y.ulStateGroupID))
                .Distinct()
                .Select(x =>
                {
                    var name = audioRepo.GetNameFromHash(x, out var found);
                    return new { Id = x, Name = name, Found = found };
                })
                .OrderByDescending(x => x.Found)
                .ToList();


            var allSwitches = audioRepo.HircObjects
               .SelectMany(x => x.Value)
               .Where(x => x.Type == HircType.SwitchContainer)
               .Cast<CAkSwitchCntr_v136>()
               .ToList();


            var allSwitchesSwitchId = allSwitches
            .SelectMany(x => x.SwitchList.Select(y => y.SwitchId))
            .Distinct()
            .Select(x =>
            {
                var name = audioRepo.GetNameFromHash(x, out var found);
                return new { Id = x, Name = name, Found = found };
            })
            .OrderByDescending(x => x.Found)
            .ToList();

            var option = new JsonSerializerOptions()
            {
                Converters = { new JsonStringEnumConverter(), new WwiseJsonNumberConverterFactory(audioRepo) },
                WriteIndented = true
            };

            var allHircs = audioRepo.HircObjects.SelectMany(x => x.Value);


            /*
                     State = 0x01,
        Sound = 0x02,
        Action = 0x03,
         = 0x04,
         = 0x05,
         = 0x06,
         = 0x07,
         = 0x08,
         = 0x09,
        //
         = 0x0a,
         = 0x0b,
         = 0x0c,
         = 0x0d,
        //
        Attenuation = 0x0e,
         = 0x0f,
        FxShareSet = 0x10,
        FxCustom = 0x11,
         = 0x12,
        LFO = 0x13,
        Envelope = 0x14,
        AudioDevice = 0x15,
        TimeMod = 0x16,
             */


            var objectTypes = new List<HircType>
            {
                HircType.Event,
                HircType.SequenceContainer,
                HircType.SwitchContainer,
                HircType.ActorMixer,
                HircType.Audio_Bus,
                HircType.LayerContainer,
                HircType.Music_Segment,
                HircType.Music_Track,
                HircType.Music_Random_Sequence,
                HircType.Music_Switch,
                HircType.Dialogue_Event,
                HircType.AuxiliaryBus,
                //HircType.Sound,
                HircType.FxCustom,
                HircType.FxShareSet,
                HircType.Action
            };

            StringBuilder master = new StringBuilder();
            StringBuilder ids = new StringBuilder();
            foreach (var item in objectTypes)
            {
                var itemGroup = allHircs.Where(x => x.Type == item).ToArray();
                var hircAsString = JsonSerializer.Serialize<object[]>(itemGroup, option);
                File.WriteAllText($"c:\\temp\\HircList\\{item}.json", hircAsString);

                master.AppendLine(item + " count:" + itemGroup.Count());
                master.AppendLine(hircAsString + "\n\n");

                var localIds = itemGroup.Select(x => $"{x.Id} {x.Type} {x.OwnerFile} {x.IndexInFile}").ToList();
                localIds.ForEach(x => ids.AppendLine(x));
            }

            //File.WriteAllText($"c:\\temp\\HircList\\master.json", master.ToString());
            File.WriteAllText($"c:\\temp\\HircList\\ids.txt", ids.ToString());

            return;

            // var switchulStateGroupIDs = allSwitches
            //     .Select(x => x.ulGroupID)
            //     .Distinct()
            //     .Select(x =>
            //     {
            //         var name = audioRepo.GetNameFromHash(x, out var found);
            //         return new { Id = x, Name = name, Found = found };
            //     })
            //     .OrderByDescending(x => x.Found)
            //     .ToList();

            //var devents = audioRepo.HircObjects
            // .SelectMany(x => x.Value)
            // .Where(x => x.Type == HircType.Dialogue_Event)
            // .DistinctBy(x => x.Id)
            // .Cast<CAkDialogueEvent_v136>()
            // .ToList();




            //var baseParamsProviders = audioRepo.HircObjects
            //   .SelectMany(x => x.Value)
            //   .Where(x => x is INodeBaseParamsAccessor)
            //   //.DistinctBy(x => x.Id)
            //   .ToList();
            //
            //var ulStateGroupIDs = baseParamsProviders
            //    .SelectMany(x => (x as INodeBaseParamsAccessor).NodeBaseParams.StateChunk.StateChunks.Select(y => y.ulStateGroupID))
            //    .Distinct()
            //    .Select(x =>
            //    {
            //        var name = audioRepo.GetNameFromHash(x, out var found);
            //        return new { Id = x, Name = name, Found = found };
            //    })
            //    .OrderByDescending(x => x.Found)
            //    .ToList();
            //
            //
            //var baseParams = audioRepo.HircObjects
            // .SelectMany(x => x.Value)
            // .Where(x=>x is INodeBaseParamsAccessor)
            // .DistinctBy(x => x.Id)
            // //.Cast<INodeBaseParamsAccessor>()
            // .ToList();
            //
            //
            //
            //
            //var handyData = baseParams
            //    .Select(x => new
            //    {
            //        Id = x.Id,
            //        BaseParamsAccessor = x as INodeBaseParamsAccessor,
            //        AllStateProps = (x as INodeBaseParamsAccessor).NodeBaseParams.StateChunk.StateProps,
            //        AllSateChunks = (x as INodeBaseParamsAccessor).NodeBaseParams.StateChunk.StateChunks,
            //        AllSateChunksStates = (x as INodeBaseParamsAccessor).NodeBaseParams.StateChunk.StateChunks.SelectMany(x=>x.States).ToList(),
            //    });
            //
            //
            //var test0 = handyData.Where(x => x.AllSateChunksStates.FirstOrDefault(x => x.ulStateID == 964666289) != null).ToList();
            //var test1 = handyData.Where(x => x.AllSateChunksStates.FirstOrDefault(x => x.ulStateID == 3501906231) != null).ToList();    // This is correct 
            //
            //
            //var test2 = handyData.Where(x => x.AllSateChunksStates.FirstOrDefault(x => x.ulStateInstanceID == 964666289) != null).ToList();
            //var test3 = handyData.Where(x => x.AllSateChunksStates.FirstOrDefault(x => x.ulStateInstanceID == 3501906231) != null).ToList();
            //
            ////var eventAndName = events.Select(x =>
            ////    {
            ////        var name = audioRepo.GetNameFromHash(x.Id, out var found);
            ////        return new { Id = x.Id, Name = name, Found = found };
            ////    })
            ////    .OrderByDescending(x => x.Found)
            ////    .ToList();
            ////
            ////var missingCount = eventAndName.Where(x => x.Found == false).Count();
            ////
            ////
            //var t = audioRepo.GetHircObject(110788530);
            ////var t2 = audioRepo.GetHircObject(3501906231);



            /*   var sounds = audioRepo.HircObjects
                           .SelectMany(x => x.Value)
                           .DistinctBy(x => x.Id)
                           .Where(x => x.Type == HircType.Sound)
                           .Cast<CAkSound_v136>()
                           .ToList();

                       */
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




        /* static void TestDialogEventSerialization()
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


          }*/
    }
}
