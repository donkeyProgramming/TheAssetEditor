using CommonControls.Common;
using CommonControls.FileTypes.MetaData;
using CommonControls.Services;
using CsvHelper;
using Serilog;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace AssetEditor.Report
{
    class AnimMetaDataReportGenerator
    {
        ILogger _logger = Logging.Create<AnimMetaDataReportGenerator>();

        class FileReport
        {
            //public string MetaType { get; set; }
            public List<string> FailedFiles { get; set; } = new List<string>();
            public List<string> CompletedFiles { get; set; } = new List<string>();
            public List<string> Headers { get; set; } = new List<string>() { "FileName", "Error"};
        }


        PackFileService _pfs;
        public AnimMetaDataReportGenerator(PackFileService pfs)
        {
            _pfs = pfs;
        }

        public void Create(string gameDirectory, string outputDir = @"c:\temp\AssReports\Meta\")
        {
            // Create folders
            var gameOutputDir = outputDir + $"\\{gameDirectory}\\";
            var gameOutputDirFailed = outputDir + $"\\{gameDirectory}\\Failed\\";
            if (Directory.Exists(gameOutputDir))
                Directory.Delete(gameOutputDir, true);
            DirectoryHelper.EnsureCreated(gameOutputDir);
            DirectoryHelper.EnsureCreated(gameOutputDirFailed);

            var output = new Dictionary<string, FileReport>();
            
            var fileList = _pfs.FindAllWithExtentionIncludePaths(".meta");
            //fileList = fileList.Where(x => x.FileName.Contains("snd.meta") == false).ToList();
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
                    var metaData = MetaDataFileParser.ParseFileV2(data);
                    metaTable.Add( (fileName, metaData) );

                    var completedTags = 0;
                    foreach (var item in metaData.Items)
                    {
                        var tagName = item.Name + "_v" + item.Version;
                        tagName = tagName.ToLower();

                        if (output.ContainsKey(tagName) == false)
                            output[tagName] = new FileReport();

                        try
                        {
                            var variables = MetaEntrySerializer.DeSerializeToStrings(item);

                            if (output[tagName].CompletedFiles.Count == 0)
                            {
                                foreach (var variable in variables)
                                    output[tagName].Headers.Add(variable.Header);
                            }

                            var variableValues = variables.Select(x => x.Value).ToList();
                            variableValues.Insert(0, fileName);
                            variableValues.Insert(1, "");

                            output[tagName].CompletedFiles.Add(string.Join("|", variableValues));
                            completedTags++;
                        }
                        catch(Exception e)
                        {
                            var variableValues = new List<string>() { fileName, e.Message };
                            output[tagName].FailedFiles.Add(string.Join("|", variableValues));
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
                        content.WriteLine(competed);

                    var fileName = gameOutputDir + item.Key + $"_{item.Value.CompletedFiles.Count}.csv";
                    File.WriteAllText(fileName, content.ToString());
                }

                if (item.Value.FailedFiles.Count != 0)
                {
                    var content = new StringWriter();
                    content.WriteLine("sep=|");
                    foreach (var failed in item.Value.FailedFiles)
                        content.WriteLine(failed);

                    var fileName = gameOutputDirFailed + item.Key + $"_{item.Value.FailedFiles.Count}.csv";
                    File.WriteAllText(fileName, content.ToString());
                }
            }

            var orderedOutputList = output
                .GroupBy(x => x.Value.CompletedFiles.Count / (x.Value.FailedFiles.Count + x.Value.CompletedFiles.Count))
                .ToList();

            foreach (var item in output)
            {
                var content = new StringWriter();
                content.WriteLine("sep=|");

                var str = $"{item.Key}| {item.Value.CompletedFiles.Count}/{item.Value.CompletedFiles.Count + item.Value.FailedFiles.Count}";
                _logger.Here().Information(str);
                content.WriteLine(str);

                var fileName = gameOutputDir + "Summary.csv";
                File.WriteAllText(fileName, content.ToString());
            }
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
