using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using System;
using System.Linq;

namespace Wh3AnimPackCreator
{
    class Program
    {
        static void Main(string[] args)
        {
            var wh3 = new InformationContainer(GameTypeEnum.Warhammer3);
            //var troy = new InformationContainer(GameTypeEnum.Troy);


        }



        static void CollectData(PackFile s)
        { }


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
