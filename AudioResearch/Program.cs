using CommonControls.Editors.AudioEditor;
using CommonControls.Editors.Sound;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.FileTypes.Sound;
using CommonControls.FileTypes.Sound.WWise;
using CommonControls.FileTypes.Sound.WWise.Hirc;
using CommonControls.FileTypes.Sound.WWise.Hirc.V136;
using CommonControls.Services;
using MoreLinq;
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
        static void Main(string[] args)
        {
            // Create a progam
            Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
            GameInformationFactory.Create();
            var pfs = GetPackFileService(skipLoadingWemFiles: false);

            var pathToPackFileWithSounds = "Path to packfile with sounds";
            if (File.Exists(pathToPackFileWithSounds))
                pfs.Load(pathToPackFileWithSounds);
            
            // Create an output pack
            var newPackFile = pfs.CreateNewPackFileContainer("CustomPackFile", PackFileCAType.MOD);
            pfs.SetEditablePack(newPackFile);

            // Compile some bnkFiles
            BnkCompilerTest.Run(@"Data\SimpleBnkProject.bnk.xml", pfs);

            // Load all game data
            WwiseDataLoader builder = new WwiseDataLoader();
            var bnkList = builder.LoadBnkFiles(pfs);
            var globalDb = builder.BuildMasterSoundDatabase(bnkList);
            var nameHelper = builder.BuildNameHelper(pfs);

            // Explore some data
            //Ole_DataExploration(globalDb, nameHelper);
            //Ole_DataExploration2(pfs, globalDb, nameHelper);
            Console.ReadLine();
        }

        private static void Ole_DataExploration2(PackFileService pfs, ExtenededSoundDataBase globalDb, WWiseNameLookUpHelper nameHelper)
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
        }

        private static void Ole_DataExploration(ExtenededSoundDataBase globalDb, WWiseNameLookUpHelper nameHelper)
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
        }



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

        static int GetMissingEventNameCount(ExtenededSoundDataBase globalDb, WWiseNameLookUpHelper nameHelper)
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
        }

        static PackFileService GetPackFileService(bool skipLoadingWemFiles = true)
        {
            var appSettings = new ApplicationSettingsService();
            appSettings.CurrentSettings.SkipLoadingWemFiles = skipLoadingWemFiles;
            var gamePath = appSettings.CurrentSettings.GameDirectories.First(x => x.Game == GameTypeEnum.Warhammer3);
            PackFileService pfs = new PackFileService(new PackFileDataBase(), new SkeletonAnimationLookUpHelper(), appSettings);
            pfs.LoadAllCaFiles(gamePath.Path, GameInformationFactory.GetGameById(GameTypeEnum.Warhammer3).DisplayName);

            return pfs;
        }
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
