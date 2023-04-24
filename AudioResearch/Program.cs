using Audio.AudioEditor;
using Audio.FileFormats.WWise;
using Audio.FileFormats.WWise.Hirc;
using Audio.FileFormats.WWise.Hirc.V136;
using Audio.Storage;
using Audio.Utility;
using CommonControls.Common;
using CommonControls.Editors.AudioEditor.BnkCompiler;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using CommunityToolkit.Diagnostics;
using MoreLinq;
using MoreLinq.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Action = CommonControls.Editors.AudioEditor.BnkCompiler.Action;

namespace AudioResearch
{
    class Program
    {
        static void Main(string[] args)
        {
            // CompileTest();
            //TableTest();
            //OvnTest.GenerateProjectFromBnk(false);
            var currentProjectName = $"Data\\OvnExample\\Project.json";
            OvnTest.GenerateProjectFromBnk(currentProjectName);
            OvnTest.Compile(currentProjectName, false, true , false);
            //TestDialogEventSerialization();
        }






        static void CompileTest()
        {
            using var application = new SimpleApplication();

            var pfs = application.GetService<PackFileService>();
            //pfs.LoadAllCaFiles(GameTypeEnum.Warhammer3);
            pfs.CreateNewPackFileContainer("SoundOutput", PackFileCAType.MOD, true);
            PackFileUtil.LoadFilesFromDisk(pfs, new[]
            {
                new PackFileUtil.FileRef( packFilePath: @"audio\wwise", systemPath:@"Data\CustomSoundCompile\790209750.wem"),
                new PackFileUtil.FileRef( packFilePath: @"audioprojects", systemPath:@"Data\CustomSoundCompile\Project.json")
            });

            var compiler = application.GetService<Compiler>();
            var compileResult = compiler.CompileProject(@"audioprojects\Project.json", out var errorList);
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



        //static void TestDialogEventSerialization()
        //{
        //    using var application = new SimpleApplication();
        //
        //    var pfs = application.GetService<PackFileService>();
        //    pfs.LoadAllCaFiles(GameTypeEnum.Warhammer3);
        //    var audioRepo = application.GetService<IAudioRepository>();
        //
        //    var dialogEvents = audioRepo.GetAllOfType<CAkDialogueEvent_v136>();
        //    foreach(var dialogEvent in dialogEvents) 
        //    {
        //        dialogEvent.AkDecisionTree.VerifyState();
        //        var bytes = dialogEvent.GetAsByteArray();
        //        var chuck = new Filetypes.ByteParsing.ByteChunk(bytes);
        //        var reParsedObject = new CAkDialogueEvent_v136();
        //        reParsedObject.Parse(chuck);
        //        // reParsedObject.AkDecisionTree.VerifyState();
        //        // Debug.Assert(dialogEvent.AkDecisionTree.Flatten());
        //        Console.WriteLine($"Main.Success: {dialogEvent.Id}");
        //    }
        //}
    }
}
