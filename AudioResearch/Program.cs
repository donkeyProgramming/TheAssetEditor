using AssetEditor;
using Audio.FileFormats.WWise;
using Audio.FileFormats.WWise.Hirc.V136;
using Audio.Storage;
using Audio.Utility;
using CommonControls.Common;
using CommonControls.Editors.AudioEditor;
using CommonControls.Editors.AudioEditor.BnkCompiler;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using Microsoft.Extensions.DependencyInjection;
using MoreLinq;
using Octokit;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AudioResearch
{

    // RPFC - Real time parameter control 
    class Program
    {

        /*
        public static bool Run(string projectFilePath, PackFileService pfs, out string outputFile)
        {
            outputFile = null;
            var compiler = new Compiler(pfs);
            var fileContent = File.ReadAllBytes(projectFilePath);
            var packFile = new PackFile("project", new MemorySource(fileContent));

            var compileResultLog = new ErrorListViewModel.ErrorList();
            var result = compiler.CompileProject(packFile, ref compileResultLog);

            if (compileResultLog.Errors.Count == 0 && result != null)
            {
                var project = compiler.ProjectFile;

                // Save to disk for easy debug
                var bnkFile = result.OutputBnkFile;
                var chunk = bnkFile.DataSource.ReadDataAsChunk();
                outputFile = $"Data\\{project.OutputFile}".ToLower().Trim();
                chunk.SaveToFile(outputFile);

                return true;
            }

            throw new System.Exception("Something went wrong");
        }
         */

        static void Main(string[] args)
        {
            var applicationBuilder = new DependencyInjectionConfig();
            using var scope = applicationBuilder.ServiceProvider.CreateScope();

            var settings = scope.ServiceProvider.GetService<ApplicationSettingsService>();
            settings.CurrentSettings.SkipLoadingWemFiles = true;

            var pfs = scope.ServiceProvider.GetService<PackFileService>();
            pfs.LoadAllCaFiles(GameTypeEnum.Warhammer3);
            PackFileUtil.LoadFilesFromDisk(pfs, new[] 
            { 
                new PackFileUtil.FileRef( packFilePath: @"audio\wwise", systemPath:@"Data\CustomSoundCompile\790209750.wem"),
                new PackFileUtil.FileRef( packFilePath: @"audioprojects", systemPath:@"Data\CustomSoundCompile\Project.xml")
            } );

            var compiler = scope.ServiceProvider.GetService<Compiler>();
            compiler.ExportResultToFile = true;
            compiler.ConvertResultToXml = true;
            compiler.ThrowOnCompileError = true;
            var result = compiler.CompileProject(@"audioprojects\Project.xml", out var errorList);

            
            
            
            
            
            //PackFileUtil.SaveFile(result.OutputBnkFile, $"Data\\{compiler.ProjectFile.OutputFile}".ToLower().Trim());
            //ConvertBnkToXml($"Data\\{compiler.ProjectFile.OutputFile}".ToLower().Trim())


            //
            // var container = pfs.CreateNewPackFileContainer("myStuff", PackFileCAType.MOD);
            // pfs.AddFilesToPack(container, "asd\asdasd", new PackFile());
            // pfs.AddFilesToPack(container, "asd\asdasd", new PackFile());
            // pfs.AddFilesToPack(container, "asd\asdasd", new PackFile());
            //

            //
            // var bnkLoader = scope.ServiceProvider.GetService<WWiseBnkLoader>();
            // var load = bnkLoader.LoadBnkFile(null, "", true);



            // Create a progam
            Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
            //GameInformationFactory.Create();

            //LoadBnkFromFile(@"C:\temp\wwiseextracttest.bnk");
            //BnkFileManipulatorTests.ExtractToOwnPack();
            //LoadAllTest(); <- Make this work
            //CompileTest0();

            //DialogEventsAsTables.ExportAllDialogEventsAsTable();
            //DialogEventsAsTables.ExportSystemFileAsTable(@"C:\Users\ole_k\Downloads\battle_vo_orders_999999999__core.bnk", "custom_");

            //CompileTestH
        }

       /* static void LoadBnkFromFile(string path)
        {
            var pfs = PackFileUtil.CreatePackFileService();

            var audioFile = PackFileUtil.CreateNewPackFileFromSystem(path, out var _);
            pfs.AddFilesToPack(pfs.GetEditablePack(), new List<string>() { @"audio\wwise", }, new List<PackFile>() { audioFile });

            WwiseDataLoader builder = new WwiseDataLoader();
            var bnkList = builder.LoadBnkFiles(pfs);
            var globalDb = builder.BuildMasterSoundDatabase(bnkList);
            var nameHelper = builder.BuildNameHelper(pfs);
        }*/

        static void CompileTest0()
        {
            var pfs = PackFileUtil.CreatePackFileService();

            var audioFile = PackFileUtil.CreateNewPackFileFromSystem(@"Data\CustomSoundCompile\790209750.wem", out var _);
            pfs.AddFilesToPack(pfs.GetEditablePack(), new List<string>() { @"audio\wwise", }, new List<PackFile>() { audioFile });


            // var audioProject = PackFileUtil.CreatePackFileFromSystem("", out var _);
            // pfs.AddFilesToPack(pfs.GetEditablePack(), new List<string>() { "audio//wwise", }, new List<PackFile>() { audioProject });

            BnkCompilerTest.Run(@"Data\CustomSoundCompile\Project.xml", pfs, out var outputFile);

            // Run wwiser with export to xml and wwiser names 
            var fullPath = Directory.GetCurrentDirectory() + "\\" + outputFile; 
            ExecuteCommand("C:\\Users\\ole_k\\Desktop\\audio_research\\WWiser\\wwiser.pyz " + fullPath);
            DirectoryHelper.OpenFolderAndSelectFile(fullPath);
        }

        static void ConvertBnkToXml(string path)
        {
            var fullPath = Directory.GetCurrentDirectory() + "\\" + path;
            ExecuteCommand("C:\\Users\\ole_k\\Desktop\\audio_research\\WWiser\\wwiser.pyz " + fullPath);
            DirectoryHelper.OpenFolderAndSelectFile(fullPath);
        }

        public static void ExecuteCommand(string Command)
        {
            ProcessStartInfo ProcessInfo;
            Process Process;

            ProcessInfo = new ProcessStartInfo("cmd.exe", "/K " + Command);
            ProcessInfo.CreateNoWindow = true;
            ProcessInfo.UseShellExecute = true;

            Process = Process.Start(ProcessInfo);
        }


       /* static void LoadAllTest()
        {
            var pfs = ResearchUtil.GetPackFileService(skipLoadingWemFiles: false);

            var pathToPackFileWithSounds = "Path to packfile with sounds";
            if (File.Exists(pathToPackFileWithSounds))
                pfs.Load(pathToPackFileWithSounds);

            // Create an output pack
            var newPackFile = pfs.CreateNewPackFileContainer("CustomPackFile", PackFileCAType.MOD);
            pfs.SetEditablePack(newPackFile);

            // Load all game data
            WwiseDataLoader builder = new WwiseDataLoader();
            var bnkList = builder.LoadBnkFiles(pfs);
            var globalDb = builder.BuildMasterSoundDatabase(bnkList);
            var nameHelper = builder.BuildNameHelper(pfs);
            //nameHelper.SaveToFile(@"C:\Users\ole_k\Desktop\audio research\WWiser\wwnames.txt");

            Console.ReadLine();
        }*/

       /* static void CompileTest()
        {
            var pfs = ResearchUtil.GetPackFileService(skipLoadingWemFiles: false);

            var pathToPackFileWithSounds = "Path to packfile with sounds";
            if (File.Exists(pathToPackFileWithSounds))
                pfs.Load(pathToPackFileWithSounds);

            var allNonLangFiles = pfs.FindAllFilesInDirectory("audio\\wwise", false);
            var orderedAllNonLangFiles = allNonLangFiles.OrderByDescending(x => x.DataSource.Size).ToList();


            SoundPlayer player = new SoundPlayer(pfs, null);
            player.PlaySound(713853811);    // I will continue, later


            // Create an output pack
            var newPackFile = pfs.CreateNewPackFileContainer("CustomPackFile", PackFileCAType.MOD);
            pfs.SetEditablePack(newPackFile);

            // Compile some bnkFiles
            //BnkCompilerTest.Run(@"Data\SimpleBnkProject.bnk.xml", pfs);

            // Load all game data
            WwiseDataLoader builder = new WwiseDataLoader();
            var bnkList = builder.LoadBnkFiles(pfs);
            var globalDb = builder.BuildMasterSoundDatabase(bnkList);
            var nameHelper = builder.BuildNameHelper(pfs);

            var a0 = nameHelper.GetName(3113488887);
            var a1 = nameHelper.GetName(112324978);
            var a2 = nameHelper.GetName(2411227174);

            // Explore some data
            //Ole_DataExploration(globalDb, nameHelper);
            //Ole_DataExploration2(pfs, globalDb, nameHelper);
            Console.ReadLine();
        }*/

       
       /* private static void Ole_DataExploration2(PackFileService pfs, ExtenededSoundDataBase globalDb, WWiseNameLookUpHelper nameHelper)
        {
            var allSounds = globalDb.HircList.Where(X => X.Value.First() is CAkSound_v136).Select(x => x.Value.First()).Cast<CAkSound_v136>().ToList();

            var streamTypes = allSounds.CountBy(x => x.AkBankSourceData.StreamType).ToList();
            var parentIds = allSounds.CountBy(x => nameHelper.GetName(x.NodeBaseParams.DirectParentID)).OrderByDescending(x => x.Value).ToList();

            var actorsWithName = allSounds.Select(x =>
            {
                var name = nameHelper.GetName(x.Id, out var found);
                return new { name = name, Found = found, Obj = x };
            })
             .OrderByDescending(x => x.Found)
             .ToList();

            var onluFound = actorsWithName.Where(X => X.Found).Select(X => X.name).ToList();



            foreach (var sound in allSounds)
            {
                var streamType = sound.AkBankSourceData.StreamType;
                var filename = $"audio\\wwise\\{sound.AkBankSourceData.akMediaInformation.SourceId}.wem";
                var bnkSize = sound.AkBankSourceData.akMediaInformation.uInMemoryMediaSize;
                var file = pfs.FindFile(filename);
                var size = file?.DataSource.Size;

                if (file != null)
                {

                }
                else
                {

                }

                if (file != null && streamType == SourceType.Data_BNK)
                {

                }
            }
        }*/

       /* private static void Ole_DataExploration(ExtenededSoundDataBase globalDb, WWiseNameLookUpHelper nameHelper)
        {
            var allHircs = globalDb.HircList.SelectMany(x => x.Value).ToList();
            var allActorMixers = allHircs.Where(x => x.Type == HircType.Audio_Bus || x.Type == HircType.AuxiliaryBus);
            var actorsWithName = allActorMixers.Select(x =>
            {
                var name = nameHelper.GetName(x.Id, out var found);
                return new { name = name, Found = found, Obj = x };
            })
                .OrderByDescending(x => x.Found).ToList();
            var instance = allActorMixers.First(x => x.Id == 260354713);
            //var instanceChildIds = instance.Children.ChildIdList;
            //var instanceOfChildren = allHircs.Where(x => instanceChildIds.Contains(x.Id)).ToList();


            var s = nameHelper.GetName(803409642, out var found);

            // Check data
            var missingEvents = GetMissingEventNameCount(globalDb, nameHelper);
            Debug.Assert(missingEvents == 0);

            // Investigate when items share values 
            var multiRef = globalDb.HircList.Where(x => x.Value.Count > 1).OrderByDescending(x => x.Value.Count).ToList();
            var countByType = multiRef.CountBy(x => x.Value.First().Type).ToList();

            var multiRefNames = multiRef.Select(x =>
            {
                var name = nameHelper.GetName(x.Key, out var found) + $" {x.Value.First().Type}";
                var areIdentical = AreEqual(x.Value);
                var differentTypes = x.Value.Select(x => x.Type).Distinct().ToList();
                return new { DisplayName = name, Found = found, AreIdentical = areIdentical, Values = x.Value, Types = differentTypes };
            });

            var multiRefNames_notIdentical = multiRefNames.Where(x => x.AreIdentical == false).ToList();
            var multiRefNames_notIdentical_count = multiRefNames_notIdentical.CountBy(x => x.Values.First().Type).ToList();

            var dialog = multiRefNames_notIdentical.Where(x => x.Values.First().Type == HircType.Dialogue_Event).ToList();

            var multiRefNames_Identical = multiRefNames.Where(x => x.AreIdentical == true).ToList();
            var multiRefNames_Identical_count = multiRefNames_Identical.CountBy(x => x.Values.First().Type).ToList();

            var itemsWhereTypeIsDifferent = multiRefNames.Where(X => X.Types.Count() != 1).ToList();

            var itemsWhereWeKnowTheName = multiRefNames_notIdentical.Where(x => x.Found == true);


            var stuff = globalDb.HircList.Where(x => x.Value.Count > 1).OrderByDescending(x => x.Value.Count).ToList();


            var hircInFile = allHircs.Where(x => x.OwnerFile.Contains("battle_vo_orders_")).ToList();
            var eventHircsInFile = hircInFile.Where(x => x.Type == HircType.Event).Select(x => nameHelper.GetName(x.Id)).ToList();

            //var fileNames = hircInFile
            //    .Select(x => x.OwnerFile)
            //    .Distinct()
            //    .Select(x=>x.Replace("battle_vo_orders_", ""))
            //    .Select(x => x.Replace("_core.bnk", ""))
            //    .Select(x => x.Replace("__warhammer3.bnk", ""))
            //    .Select(x => x.Replace("__warhammer2.bnk", ""))
            //    .Select(x => x.Replace("warhammer3.bnk", ""))
            //    .Select(x => x.Replace("warhammer2.bnk", ""))
            //    .Select(x => x.Replace("_", ""))
            //    .Where(x=>x.Length != 0)
            //    .Select(x=>uint.Parse(x))
            //    .Select(x=>nameHelper.GetName(x))
            //    .ToList();
            //
            //
            //var fileNamesD = fileNames.Distinct().ToList();

            var areHircsEqual = multiRef.Select(x => AreEqual(x.Value)).ToList();

            var notFoundMulti = multiRefNames.Where(x => x.Found == false).ToList();
            var foundMulti = multiRefNames.Where(x => x.Found == true).ToList();
        }*/



        static bool AreEqual(List<HircItem> hircList)
        {
            var first = hircList.First();
            var bytes = first.Size;
            foreach (var hirc in hircList.Skip(1))
            {
                if (bytes != hirc.Size)
                    return false;
            }

            return true;
        }

       /* static int GetMissingEventNameCount(ExtenededSoundDataBase globalDb, WWiseNameLookUpHelper nameHelper)
        {
            var hircEvents = globalDb.HircList
                .Select(x => x.Value.First())
                .Where(x => x.Type == HircType.Event)
                .ToList();

            var hircNames = hircEvents.Select(x =>
            {
                var name = nameHelper.GetName(x.Id, out var found);
                return new { DisplayName = name, Found = found };
            });

            var notFoundEvents = hircNames.Where(x => x.Found == false).ToList();
            return notFoundEvents.Count;
        }*/


    }


    /*
      void ParsBnkFiles(ExtenededSoundDataBase masterDb, NameLookupHelper nameHelper, List<PackFile> files, VisualEventOutputNode parent, Stopwatch timer)
        {
            for(int fileIndex = 0; fileIndex < files.Count; fileIndex++)
            {
                try
                {
                    var soundDb = Bnkparser.Parse(files[fileIndex]);

                    var events = soundDb.Hircs
                        .Where(x => x.Type == HircType.Event || x.Type == HircType.Dialogue_Event)
                        .Where(x => x.HasError == false);

                    var eventsCount = events.Count();
                    var fileNodeOutputStr = $"{files[fileIndex].Name} Total EventCount:{eventsCount}";
                    _logger.Here().Information($"{fileIndex}/{files.Count} {fileNodeOutputStr}");

                    var fileOutput = parent.AddChild(fileNodeOutputStr);
                    var fileOutputError = fileOutput.AddChild("Errors while parsing :");
                    bool procesedCorrectly = true;

                    var itemsProcessed = 0;
                    foreach (var currentEvent in events)
                    {
                        var visualEvent = new EventHierarchy(currentEvent, masterDb, nameHelper, fileOutput, fileOutputError, files[fileIndex].Name);

                        if (itemsProcessed % 100 == 0 && itemsProcessed != 0)
                            _logger.Here().Information($"\t{itemsProcessed}/{eventsCount} events processsed [{timer.Elapsed.TotalSeconds}s]");

                        itemsProcessed++;
                        procesedCorrectly = visualEvent.ProcesedCorrectly && procesedCorrectly;
                    }

                    if (procesedCorrectly == true)
                        fileOutput.Children.Remove(fileOutputError);

                    if (events.Any())
                        _logger.Here().Information($"\t{itemsProcessed}/{eventsCount} events processsed [{timer.Elapsed.TotalSeconds}s]");
                }
                catch
                { }
            }
        }
     */
}
