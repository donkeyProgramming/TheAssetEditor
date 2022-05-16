using CommonControls.FileTypes.AnimationPack;
using CommonControls.FileTypes.AnimationPack.AnimPackFileTypes;
using CommonControls.FileTypes.AnimationPack.AnimPackFileTypes.Wh3;
using CommonControls.FileTypes.MetaData;
using CommonControls.FileTypes.MetaData.Definitions;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Wh3AnimPackCreator
{
    class Program
    {
        static void Main(string[] args)
        {
            //AnimationSlotTypeHelper
            var content = File.ReadAllLines(@"C:\Users\ole_k\Desktop\Strings\AllAnimTypesTroy.txt");
            var counter = 0;
            foreach (var line in content)
            {
                var result =  AnimationSlotTypeHelper.GetfromValue(line);
                if (result == null)
                {
                    Console.WriteLine(line);
                    counter++;
                }


            }




            MetaDataTagDeSerializer.EnsureMappingTableCreated();
            var metaParser = new MetaDataFileParser();
            var settings = new ApplicationSettingsService();
            var gameSettings = settings.CurrentSettings.GameDirectories.First(x => x.Game == GameTypeEnum.Troy);

            var pfs = new PackFileService(new PackFileDataBase(), null, settings);
            pfs.LoadAllCaFiles(gameSettings.Path, gameSettings.Game.ToString());

            try
            {
                // Create output packfile
                var outputPfs = new PackFileService(new PackFileDataBase(), null, new ApplicationSettingsService());
                outputPfs.CreateNewPackFileContainer("AnimResource_v0_cerberus", PackFileCAType.MOD);
                AnimationPackFile outputAnimPackFile = new AnimationPackFile();


                var currentFragmentName = @"animations/animation_tables/cerb1_mth_dlc_cerberus.frg";
                PrintDebugInformation(pfs, currentFragmentName);

                // Create output bin
                //var currentOutputAnimBin = new AnimationBinWh3(Path.GetFileNameWithoutExtension(currentFragmentName));
                
                // Do the work
                var animContainer = GetAnimationContainers(pfs, currentFragmentName);

                var groupedSlots = animContainer.FragmentFile.Fragments.GroupBy(x => x.Slot.Value).ToList();

                var animFilesToCopy = new List<string>();
                var metaFilesToCopy = new List<string>();
                foreach (var groupedSlot in groupedSlots)
                {
                    Console.WriteLine($"\t {groupedSlot.Key}[{groupedSlot.Count()}]");

                    foreach (var slot in groupedSlot)
                    {
                        if (string.IsNullOrWhiteSpace(slot.AnimationFile) == false)
                            animFilesToCopy.Add(slot.AnimationFile);

                        if (string.IsNullOrWhiteSpace(slot.MetaDataFile) == false)
                            metaFilesToCopy.Add(slot.MetaDataFile);
                    }
                }

                var distinctAnimFiles = animFilesToCopy.Distinct();
                Console.WriteLine($"AnimFiles {distinctAnimFiles.Count()}:");
                distinctAnimFiles.ForEach(i => Console.WriteLine($"\t {i}"));

                var distinctMetaFiles = metaFilesToCopy.Distinct();
                Console.WriteLine($"MetaFiles {distinctMetaFiles.Count()}:");
                distinctMetaFiles.ForEach(i => Console.WriteLine($"\t {i}"));

                // Add the packfile


                //outputAnimPackFile.

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }


            //var wh3 = new InformationContainer(GameTypeEnum.Warhammer3);
            //var troy = new InformationContainer(GameTypeEnum.Troy);


        }


        static (AnimationBin AnimBin, AnimationFragmentFile FragmentFile) GetAnimationContainers(PackFileService pfs, string fragmentName)
        {
            var gameAnimPackFile = pfs.FindFile(@"animations\animation_tables\animation_tables.animpack");
            var gameAnimPack = AnimationPackSerializer.Load(gameAnimPackFile, pfs);
            var animBin = gameAnimPack.Files.First(x => x.FileName == @"animations/animation_tables/animation_tables.bin") as AnimationBin;
            var fragment = gameAnimPack.Files.First(x => x.FileName == fragmentName) as AnimationFragmentFile;

            return (animBin, fragment);
        }


        static void PrintDebugInformation(PackFileService pfs, string fragmentName)
        {
            try
            {
                var metaParser = new MetaDataFileParser();
                var currentFragmentName = fragmentName;
                Console.Clear();
                Console.WriteLine($"Starting Debug Print - {currentFragmentName}");

                var gameAnimPackFile = pfs.FindFile(@"animations\animation_tables\animation_tables.animpack");
                var gameAnimPack = AnimationPackSerializer.Load(gameAnimPackFile, pfs);
                var animBin = gameAnimPack.Files.First(x => x.FileName == @"animations/animation_tables/animation_tables.bin") as AnimationBin;
                var fragment = gameAnimPack.Files.First(x => x.FileName == currentFragmentName) as AnimationFragmentFile;

                var allMetaTags = new List<string>();
                var allEffects = new List<string>();
                var allSoundEvents = new List<string>();

                foreach (var slot in fragment.Fragments)
                {
                    var animationName = slot.AnimationFile;
                    var meta = slot.MetaDataFile;
                    var sound = slot.SoundMetaDataFile;

                    if (string.IsNullOrWhiteSpace(meta) == false)
                    {
                        var metaPackFile = pfs.FindFile(meta);
                        if (metaPackFile != null)
                        {
                            var metaFile = metaParser.ParseFile(metaPackFile.DataSource.ReadData());
                            foreach (var metaEntry in metaFile.Items)
                            {
                                allMetaTags.Add(metaEntry.DisplayName);

                                if (metaEntry is Effect_v11 effectMeta)
                                    allEffects.Add(effectMeta.VfxName);
                            }
                        }
                    }

                    if (string.IsNullOrWhiteSpace(sound) == false)
                    {
                        var metaPackFile = pfs.FindFile(sound);
                        if (metaPackFile != null)
                        {
                            var metaFile = metaParser.ParseFile(metaPackFile.DataSource.ReadData());
                            foreach (var metaEntry in metaFile.Items)
                            {
                                allMetaTags.Add(metaEntry.DisplayName);

                                if (metaEntry is SoundTrigger_v10 soundMeta)
                                    allSoundEvents.Add(soundMeta.SoundEvent);
                            }
                        }
                    }
                }

                // Print data
                var distinctMetaTags = allMetaTags.Distinct();
                var distinctEffects = allEffects.Distinct();
                var distinctSoundEvents = allSoundEvents.Distinct();

                Console.WriteLine("\t MetaTags:");
                foreach (var item in distinctMetaTags)
                    Console.WriteLine($"\t\t {item}");

                Console.WriteLine("\n\t Effects:");
                foreach (var item in distinctEffects)
                    Console.WriteLine($"\t\t {item}");

                Console.WriteLine("\n\t SoundEvents:");
                foreach (var item in distinctSoundEvents)
                    Console.WriteLine($"\t\t {item}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            Console.WriteLine($"Done Debug Print");
        }


    }


    class InformationContainer
    {
        public PackFileService Pfs { get; set; }

        public InformationContainer(GameTypeEnum game)
        {
            var settings = new ApplicationSettingsService();
            var gameSettings = settings.CurrentSettings.GameDirectories.First(x => x.Game == game);

            Pfs = new PackFileService(new PackFileDataBase(), null, settings) ;
            Pfs.LoadAllCaFiles(gameSettings.Path, gameSettings.Game.ToString());
        }


        /*public void Create()
        {
            var gameName = GameInformationFactory.GetGameById(_settingsService.CurrentSettings.CurrentGame).DisplayName;
            var timeStamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            var gameOutputDir = $"{DirectoryHelper.ReportsDirectory}\\MetaData\\{gameName}_{timeStamp}\\";
            var gameOutputDirFailed = $"{gameOutputDir}\\Failed\\";
            if (Directory.Exists(gameOutputDir))
                Directory.Delete(gameOutputDir, true);
            DirectoryHelper.EnsureCreated(gameOutputDir);
            DirectoryHelper.EnsureCreated(gameOutputDirFailed);

            var output = new Dictionary<string, FileReport>();

            var fileList = Pfs.FindAllWithExtentionIncludePaths(".meta");
            var failedFiles = new List<string>();

            var metaTable = new List<(string Path, MetaDataFile File)>();
            for (int i = 0; i < fileList.Count; i++)
            {
                var fileName = fileList[i].FileName;
                var packFile = fileList[i].Pack;
                try
                {
                    var data = packFile.DataSource.ReadData();
                    if (data.Length == 0)
                        continue;

                    var parser = new MetaDataFileParser();
                    var metaData = parser.ParseFile(data);
                    metaTable.Add((fileName, metaData));

                    var completedTags = 0;
                    foreach (var item in metaData.Items)
                    {
                        var tagName = item.Name + "_v" + item.Version;
                        tagName = tagName.ToLower();

                        if (output.ContainsKey(tagName) == false)
                            output[tagName] = new FileReport();

                        try
                        {
                            var variables = MetaDataTagDeSerializer.DeSerializeToStrings(item, out var errorMessage);
                            if (variables != null)
                            {

                                if (output[tagName].CompletedFiles.Count == 0)
                                {
                                    foreach (var variable in variables)
                                        output[tagName].Headers.Add(variable.Header);
                                }

                                var variableValues = variables.Select(x => x.Value).ToList();
                                variableValues.Insert(0, fileName);
                                variableValues.Insert(1, "");

                                output[tagName].CompletedFiles.Add(variableValues);
                                completedTags++;
                            }
                            else
                            {
                                var variableValues = new List<string>() { fileName, errorMessage, item.Data.Length.ToString() };
                                output[tagName].FailedFiles.Add(variableValues);
                            }
                        }
                        catch (Exception e)
                        {
                            var variableValues = new List<string>() { fileName, e.Message, item.Data.Length.ToString() };
                            output[tagName].FailedFiles.Add(variableValues);
                        }
                    }

                    _logger.Here().Information($"File processed {i}/{fileList.Count} - {completedTags}/{metaData.Items.Count} tags loaded correctly");
                }
                catch
                {
                    _logger.Here().Information($"File processed {i}/{fileList.Count} - Parsing failed completly");
                    failedFiles.Add(fileName);
                }
            }

            // Write the data 
            foreach (var item in output)
            {
                if (item.Value.CompletedFiles.Count != 0)
                {
                    var content = new StringWriter();
                    content.WriteLine("sep=|");
                    content.WriteLine(string.Join("|", item.Value.Headers));
                    foreach (var competed in item.Value.CompletedFiles)
                        content.WriteLine(string.Join("|", competed));

                    var fileName = gameOutputDir + item.Key + $"_{item.Value.CompletedFiles.Count}.csv";
                    File.WriteAllText(fileName, content.ToString());
                }

                if (item.Value.FailedFiles.Count != 0)
                {
                    var content = new StringWriter();
                    content.WriteLine("sep=|");
                    content.WriteLine("FileName|Error|DataLength");
                    foreach (var failed in item.Value.FailedFiles)
                        content.WriteLine(string.Join("|", failed));

                    var fileName = gameOutputDirFailed + item.Key + $"_{item.Value.FailedFiles.Count}.csv";
                    File.WriteAllText(fileName, content.ToString());
                }
            }

            var summaryContent = new StringWriter();
            summaryContent.WriteLine("sep=|");
            summaryContent.WriteLine("Tag|Completed|Failed|Ratio");
            foreach (var item in output)
            {
                var str = $"{item.Key}| {item.Value.CompletedFiles.Count}| {item.Value.FailedFiles.Count} |{item.Value.FailedFiles.Count}/{item.Value.CompletedFiles.Count + item.Value.FailedFiles.Count}";
                _logger.Here().Information(str);
                summaryContent.WriteLine(str);
            }
            var summaryFileName = gameOutputDir + "Summary.csv";
            File.WriteAllText(summaryFileName, summaryContent.ToString());

            var commonHeaderContent = new StringWriter();
            commonHeaderContent.WriteLine("sep=|");
            commonHeaderContent.WriteLine("Type|FileName|Error|Version|StartTime|EndTime|Filter|Id");
            foreach (var item in output)
            {
                foreach (var competed in item.Value.CompletedFiles)
                {
                    commonHeaderContent.Write(item.Key + "|");
                    commonHeaderContent.WriteLine(string.Join("|", competed.Take(7)));
                }
            }

            var commonHeaderFile = gameOutputDir + "CommonHeader.csv";
            File.WriteAllText(commonHeaderFile, commonHeaderContent.ToString());

            MessageBox.Show($"Done - Created at {gameOutputDir}");
            Process.Start("explorer.exe", gameOutputDir);
        }*/

        void LoadMetaDataFiles()
        { 
        
        }

        void GetAllEffects()
        { }

        void GetAllSounds()
        { }

        void GetAllMetaDataTags()
        {

        }
    }
}
