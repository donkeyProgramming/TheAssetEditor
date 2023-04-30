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
using System.Linq;
using static AudioResearch.Program.LogicalChaining;

namespace AudioResearch
{
    class Program
    {
        static void Main(string[] args)
        {
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
            //TestDialogEventSerialization();

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
                        node => If(node.uWeight != 50).Then(_ => nodes.Add(node))
                    );
                    If(nodes.Count > 0).Then(_ =>
                        Console.WriteLine($"\t{unHash(e.Id)}: {String.Join(", ", nodes.Select(e => (unHash(e.Key), e.uWeight)))}")
                    );
                }
            );
            
            Console.WriteLine("uProbability != 100:");
            dialogEvents.ForEach(
                e =>
                {
                    var nodes = new List<AkDecisionTree.Node>();
                    e.AkDecisionTree.BfsTreeTraversal(
                        node => If(node.uProbability != 100).Then(_ => nodes.Add(node))
                    );
                    If(nodes.Count > 0).Then(_ =>
                        Console.WriteLine($"\t{unHash(e.Id)}: {String.Join(", ", nodes.Select(e => (unHash(e.Key), e.uProbability)))}")
                    );
                }
            );
            
                        
            Console.WriteLine("uWeight != 50 && uProbability != 100:");
            dialogEvents.ForEach(
                e =>
                {
                    var nodes = new List<AkDecisionTree.Node>();
                    e.AkDecisionTree.BfsTreeTraversal(
                        node => If(node.uWeight != 50 && node.uProbability != 100).Then(_ => nodes.Add(node))
                    );
                    If(nodes.Count > 0).Then(_ =>
                        Console.WriteLine($"\t{unHash(e.Id)}: {String.Join(", ", nodes.Select(e => (unHash(e.Key), e.uWeight, e.uProbability)))}")
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
                        Console.WriteLine($"Weird Node ({audioRepo.GetNameFromHash(node.Key)}): {audioRepo.GetNameFromHash(dialogEvent.Id)}({dialogEvent.Id}) | nodeCount: {dialogEvent.AkDecisionTree.NodeCount()}");
                    }
                });
                // Console.WriteLine($"Main.Success: {audioRepo.GetNameFromHash(dialogEvent.Id)}({dialogEvent.Id})");
            }

        }



        public class LogicalChaining
        {

            private bool _condition;
            private bool _isValid = true;
            private LogicalChaining(bool condition)
            {
                _condition = condition;
            }

            public static LogicalChaining If(bool condition) 
                => new LogicalChaining(condition);
            
            public LogicalChaining Then(Action action)
            {
                if (_isValid && _condition){
                    action();
                }
                return this;
            }

            //Enable style: _ => doStuff()
            public LogicalChaining Then(Action<bool> action) 
            {
                if (_isValid && _condition){
                    action(true);
                }
                return this;
            }
            
            public void Else(Action action)
            {
                if (_isValid && !_condition){
                    action();
                }
            }
            
            //Enable style: _ => doStuff()
            public void Else(Action<bool> action)
            {
                if (_isValid && !_condition){
                    action(true);
                }
            }
            
            
            public LogicalChaining ElseIf(bool condition)
            {
                if (!_isValid){
                    return this;
                }
                
                if (_condition){
                    _isValid = false; // Turn off all the rest sequence
                }else{
                    _condition = condition;
                }
                return this;
            }

            private static void LogicalChainingTest()
            {
                {
                    int x = 2;
                    If(true).Then(_ => x *= 2).Else(_ => x /= 2);
                    Debug.Assert(x == 4);
                }
                {
                    int x = 2, y = 3;
                    If(false).Then(_ => x *= 2).Else(_ => x /= 2);
                    Debug.Assert(x == 1);
                }
                {
                    int x = 2, y = 3;
                    If(false).Else(_ => x /= 2);
                    Debug.Assert(x == 1);
                }
                {
                    int x = 2, y = 3;
                    If(true).ElseIf(true).Then(_ => x *= 2);
                    Debug.Assert(x == 2);
                }
                {
                    int x = 2, y = 3;
                    If(false).ElseIf(true).Then(_ => x *= 2);
                    Debug.Assert(x == 4);
                }
            
            }
        }
    }
}
