using CommonControls.Common;
using CommonControls.Editors.AnimationPack;
using CommonControls.FileTypes.AnimationPack;
using CommonControls.FileTypes.AnimationPack.AnimPackFileTypes;
using CommonControls.FileTypes.MetaData;
using CommonControls.FileTypes.MetaData.Definitions;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Wh3AnimPackCreator
{
    partial class Program
    {
        static void Main(string[] args)
        {
            var wh3MetaTags = LoadValidWh3MetaTags();
            var _logService = new LogService(@"C:\temp\animationConverter\");
            var resourceSwapRules = new TroyResourceSwapRules(wh3MetaTags, _logService);

            MetaDataTagDeSerializer.EnsureMappingTableCreated();

            var troyGameSettings = new ApplicationSettingsService().CurrentSettings.GameDirectories.First(x => x.Game == GameTypeEnum.Troy);
            var troyPfs = new PackFileService(null, new PackFileDataBase(), new SkeletonAnimationLookUpHelper(), new ApplicationSettingsService(), new GameInformationFactory());
            troyPfs.LoadAllCaFiles(troyGameSettings.Path, troyGameSettings.Game.ToString());
            troyPfs.TriggerFileUpdates = false;

            var wh3Pfs = new PackFileService(null, new PackFileDataBase(), new SkeletonAnimationLookUpHelper(), new ApplicationSettingsService(), new GameInformationFactory());
            wh3Pfs.TriggerFileUpdates = false;
            var pfsContainer = wh3Pfs.CreateNewPackFileContainer("AnimResource_v0_cerberus", PackFileCAType.MOD);
            wh3Pfs.SetEditablePack(pfsContainer);
            var wh3AnimPack = new AnimationPackFile();

            bool shouldSave = false;
            string onlyProcessOneAnim = null;
            //onlyProcessOneAnim = @"animations/animation_tables/ce1_myth_dlc_centaur_spear_and_shield.frg";
            //onlyProcessOneAnim = @"animations/animation_tables/cerb1_mth_dlc_cerberus.frg";
            
            try
            {
                var binsToProcess = GetAllTroyAnimationFragments(troyPfs, onlyProcessOneAnim);
                var animationCounter = 1;
                foreach (var currentFragmentName in binsToProcess)
                {
                    Console.WriteLine($"Processing {animationCounter}/{binsToProcess.Length} - {currentFragmentName}");
                    var instance = new AnimationTransferHelper(_logService, troyPfs, resourceSwapRules, wh3Pfs, wh3AnimPack);
                    instance.Convert(currentFragmentName);
                    animationCounter++;
                }

                if (shouldSave)
                {
                    // Copy the skeleton db
                    var battleSkeletonTable = troyPfs.FindFile(@"db\battle_skeletons_tables\data__");
                    SaveHelper.Save(wh3Pfs, @"db\battle_skeletons_tables\troy_data__", null, battleSkeletonTable.DataSource.ReadData());

                    // Save the animPack
                    var bytes = AnimationPackSerializer.ConvertToBytes(wh3AnimPack);
                    SaveHelper.Save(wh3Pfs, @"animations/database/battle/bin/AnimPackTest.animpack", null, bytes);

                    // Save the packfile
                    wh3Pfs.Save(pfsContainer, "C:\\temp\\temp_animResources.pack", false);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            _logService.Save();
        }

        static List<string> LoadValidWh3MetaTags()
        {
            List<string> outout = new List<string>();

            var summaryPath = @"C:\Users\ole_k\AssetEditor\Reports\MetaData\Warhammer III_20220309153753573\Summary.csv";
            var lines = File.ReadAllLines(summaryPath);

            // Skip 2 first lines, csv config related
            for(int i = 2; i < lines.Length; i++)
            {
                var line = lines[i].Split('|');
                outout.Add(line[0].Trim().ToUpper());
            }

            return outout;
        }

        static string[] GetAllTroyAnimationFragments(PackFileService pfs, string onlyProcessMe = null)
        {
            if(onlyProcessMe != null)
                return new string[] { onlyProcessMe };

            var gameAnimPackFile = pfs.FindFile(@"animations\animation_tables\animation_tables.animpack");
            var gameAnimPack = AnimationPackSerializer.Load(gameAnimPackFile, pfs, GameTypeEnum.Troy);
            var fragmentNames = gameAnimPack.Files
                .Where(x => x as AnimationFragmentFile != null)
                .Select(x => x.FileName)
                .ToArray();

            return fragmentNames;
        }
    }



}
