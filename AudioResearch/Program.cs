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

namespace AudioResearch
{
    class Program
    {
        static void Main(string[] args)
        {
            if (Environment.GetEnvironmentVariable("KlissanEnv") != null){
                TestDialogEventSerialization();
                return;
            }
            
            // CompileTest();
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


       



        static void CompileTest()
        {
            //using var application = new SimpleApplication();
            //
            //var pfs = application.GetService<PackFileService>();
            ////pfs.LoadAllCaFiles(GameTypeEnum.Warhammer3);
            //pfs.CreateNewPackFileContainer("SoundOutput", PackFileCAType.MOD, true);
            //PackFileUtil.LoadFilesFromDisk(pfs, new[]
            //{
            //    new PackFileUtil.FileRef( packFilePath: @"audio\wwise", systemPath:@"Data\CustomSoundCompile\790209750.wem"),
            //    new PackFileUtil.FileRef( packFilePath: @"audioprojects", systemPath:@"Data\CustomSoundCompile\Project.json")
            //});
            //
            //var compiler = application.GetService<Compiler>();
            //var compileResult = compiler.CompileProject(@"audioprojects\Project.json", out var errorList);
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

            (string, string) DumpAsXsv(CAkDialogueEvent_v136 dialogEvent, char sep='\t', bool dumpRoot=false, bool dumpDecisionNodesFull=false)
            {
                if (sep != ',' && sep != '\t'){
                    throw new ArgumentException();
                }

                int idx = dumpDecisionNodesFull ? 3 : 1;
                
                List<string> args = null;
                if (!dumpDecisionNodesFull){
                    args = dialogEvent.ArgumentList.Arguments.Select(x => unHash(x.ulGroupId)).ToList();
                }else{
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
                    if (!dumpDecisionNodesFull){
                        keys = e.Item1.Select(n => unHashSpecial(n.Key)).ToList();
                    }else{
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
                    if (!dumpDecisionNodesFull){
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
                if (dialogEvent is null){
                    throw new ArgumentException($"Game has no dialogueEvent with the following name: {fname}");
                }

                var treeCopy = dialogEvent.AkDecisionTree.BaseCopy();
                var nodeChainLength = treeCopy._maxTreeDepth;
                if (hasRoot){
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
                        try{
                            key = uint.Parse(strings[i]);
                            Console.WriteLine($"WARNING: File {fpath} contains numeric Key ({strings[i]}), line {ln + 1}");
                        }
                        catch{
                            key = strings[i] == DEFAULT_KEYWORD ? 0 : WWiseHash.Compute(strings[i]);
                        }

                        AkDecisionTree.NodeContent node;
                        if (hasDecisionNodesFull){
                            var uProbability = ushort.Parse(strings[i + 1]);
                            var uWeight = ushort.Parse(strings[i + 2]);
                            node = new AkDecisionTree.NodeContent(key, uWeight, uProbability);
                        }
                        else{
                            if (i == nodeChainLength - step){
                                //the last one
                                var uProbability = ushort.Parse(strings[^3]);
                                var uWeight = ushort.Parse(strings[^2]);
                                node = new AkDecisionTree.NodeContent(key, uWeight, uProbability);
                            }
                            else{
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
                foreach (var wo in WeirdOnes){
                    if (path.Contains(wo)){
                        return;
                    }
                }
                var tree = ReadFromXsv(path);
                e.AkDecisionTree = tree;
                var (_, after) = DumpAsXsv(e);
                Console.WriteLine(path);
                var beforeLines = before.Split('\n');
                var afterLines = after.Split('\n');
                if (beforeLines.Length != afterLines.Length){
                    Console.WriteLine("NotEqual size!!!");
                }
                For(beforeLines.Length, i =>
                {
                    if (beforeLines[i] != afterLines[i]){
                        Console.WriteLine($"LINE #{i}");
                        Console.WriteLine(beforeLines[i]);
                        Console.WriteLine(afterLines[i]);
                    }
                });
                Debug.Assert(before == after);
            });
            return;
            foreach(var dialogEvent in dialogEvents) 
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
                    if (node.AudioNodeId == 0 && node.Children.Count == 0){
                        Console.WriteLine($"Weird Node ({audioRepo.GetNameFromHash(node.Content.Key)}): {audioRepo.GetNameFromHash(dialogEvent.Id)}({dialogEvent.Id}) | nodeCount: {dialogEvent.AkDecisionTree.NodeCount()}");
                    }
                });
                // Console.WriteLine($"Main.Success: {audioRepo.GetNameFromHash(dialogEvent.Id)}({dialogEvent.Id})");
            }

        }
    }
}
