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
            DoSomeStuff();
        }


        public static void DoSomeStuff()
        {
            using var application = new SimpleApplication();

            var pfs = application.GetService<PackFileService>();
            pfs.LoadAllCaFiles(GameTypeEnum.Warhammer3);
            pfs.CreateNewPackFileContainer("SoundOutput", PackFileCAType.MOD, true);
            //PackFileUtil.LoadFilesFromDisk(pfs, new[]
            //{
            //    new PackFileUtil.FileRef( packFilePath: @"audio\wwise", systemPath:@"D:\Research\Audio\Working pack\audio_ovn\wwise\english(uk)\campaign_diplomacy__ovn.bnk"),
            //    new PackFileUtil.FileRef( packFilePath: @"audio\wwise", systemPath:@"D:\Research\Audio\Working pack\audio_ovn\wwise\event_data__ovn.dat"),
            //});


            var audioRepo = application.GetService<IAudioRepository>();
            var sounds = audioRepo.GetAllOfType<CAkSound_v136>();

            var sortedItems =audioRepo.HircObjects
                .Select(x =>
                {
                    var itemTypes = x.Value.Select(x => x.Type).ToList();
                    var allTypesDistincted = itemTypes.Distinct().Count() == 1;
                    return new { Id = x.Key, Count = x.Value.Count(), Type=itemTypes.First(), Items = x.Value, ItemsSameType = allTypesDistincted };
                })
               .OrderByDescending(x => x.Count)
               .Where(x => x.Type == HircType.Sound)
               .ToList();

            // Write mixer
            // Write CAkFxShareSet
            // Fix write order
            // Fix bus override
            // Fix parent for sounds (mixer)
            // Fix bugs and be happy :) 
            // Work out a UI -> Add template for tables? Generate them in the weird rpfm export format

            foreach (var sound in sounds)
            {
                var result = JsonSerializer.Serialize(sound, new JsonSerializerOptions() { WriteIndented = true, IgnoreNullValues = true });
                File.WriteAllText($"D:\\Research\\Audio\\Temp\\{sound.Id}.json", result);
            }


            /*
             * Write order
             CAkAttenuation
For all mixers
	Find children - order by id (smallest top)
		For each child write audio - smallest top
		Write mixer
	Write mixer
CAkFxShareSet
Order events by id (smallest top)
	Order actions for event (Smallest top)
             */

            //
            //var projectExporter = new AudioProjectExporter();
            //projectExporter.CreateFromRepository(audioRepo, "OvnProject.json");

            //var researchHelper = application.GetService<AudioResearchHelper>();
            //researchHelper.GenerateActorMixerTree("cr_mixerTree.txt");
            //

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
    }
}
