using System.Diagnostics;
using System.Globalization;
using System.Windows;
using CsvHelper;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Events;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.Settings;
using Shared.GameFormats.AnimationMeta.Parsing;

namespace Editors.Reports.Animation
{
    public class GenerateMetaDataReportCommand(AnimMetaDataReportGenerator generator) : IUiCommand
    {
        public void Execute() => generator.Create();
    }

    public class AnimMetaDataReportGenerator
    {
        private readonly ILogger _logger = Logging.Create<AnimMetaDataReportGenerator>();

        class FileReport
        {
            //public string MetaType { get; set; }
            public List<List<string>> FailedFiles { get; set; } = new List<List<string>>();
            public List<List<string>> CompletedFiles { get; set; } = new List<List<string>>();
            public List<string> Headers { get; set; } = new List<string>() { "FileName", "Error" };
        }

        private readonly IPackFileService _pfs;
        private readonly ApplicationSettingsService _settingsService;
        private readonly MetaDataTagDeSerializer _metaDataTagDeSerializer;

        public AnimMetaDataReportGenerator(IPackFileService pfs, ApplicationSettingsService settingsService, MetaDataTagDeSerializer metaDataTagDeSerializer)
        {
            _pfs = pfs;
            _settingsService = settingsService;
            _metaDataTagDeSerializer = metaDataTagDeSerializer;
        }

        public static void Generate(IPackFileService pfs, ApplicationSettingsService settingsService, MetaDataTagDeSerializer metaDataTagDeSerializer)
        {
            var instance = new AnimMetaDataReportGenerator(pfs, settingsService, metaDataTagDeSerializer);
            instance.Create();
        }

        public void Create()
        {
            var gameName = GameInformationDatabase.GetGameById(_settingsService.CurrentSettings.CurrentGame).DisplayName;
            var timeStamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            var gameOutputDir = $"{DirectoryHelper.ReportsDirectory}\\MetaData\\{gameName}_{timeStamp}\\";
            var gameOutputDirFailed = $"{gameOutputDir}\\Failed\\";
            if (Directory.Exists(gameOutputDir))
                Directory.Delete(gameOutputDir, true);
            DirectoryHelper.EnsureCreated(gameOutputDir);
            DirectoryHelper.EnsureCreated(gameOutputDirFailed);

            var output = new Dictionary<string, FileReport>();

            var fileList = PackFileServiceUtility.FindAllWithExtentionIncludePaths(_pfs, ".meta");
            var failedFiles = new List<string>();

            var metaTable = new List<(string Path, MetaDataFile File)>();
            for (var i = 0; i < fileList.Count; i++)
            {
                var fileName = fileList[i].FileName;
                //if (fileName.Contains("hu1_2hh_knockback_l_01.anm.meta") == false)
                //    continue;

                var packFile = fileList[i].Pack;
                try
                {
                    var data = packFile.DataSource.ReadData();
                    if (data.Length == 0)
                        continue;

                    var parser = new MetaDataFileParser();
                    var metaData = parser.ParseFile(data, _metaDataTagDeSerializer);
                    metaTable.Add((fileName, metaData));

                    var completedTags = 0;
                    foreach (var item in metaData.Items)
                    {
                        var tagName = item.DisplayName;
                        tagName = tagName.ToLower();

                        if (output.ContainsKey(tagName) == false)
                            output[tagName] = new FileReport();

                        try
                        {
                            var variables = _metaDataTagDeSerializer.DeSerializeToStrings(item, out var errorMessage);
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
            foreach (var item in output.OrderBy(x => x.Key))
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
        }

        void Write(List<dynamic> dataRecords, string filePath)
        {
            using var writer = new StringWriter();
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteRecords(dataRecords);
            File.WriteAllText(filePath, writer.ToString());
        }

    }
}
