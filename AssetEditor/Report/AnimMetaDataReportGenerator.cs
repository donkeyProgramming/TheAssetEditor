using CommonControls.FileTypes.MetaData;
using CommonControls.Services;
using CsvHelper;
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
        PackFileService _pfs;
        public AnimMetaDataReportGenerator(PackFileService pfs)
        {
            _pfs = pfs;
        }

        public void Create(string gameDirectory, string outputDir = @"c:\temp\AssReports\Meta\")
        {
            var fileList = _pfs.FindAllWithExtentionIncludePaths(".meta");

            var metaTable = new List<(string Path, MetaDataFile File)>();
            for (int i = 0; i < fileList.Count; i++)
            {
                try
                {
                    var meteData = MetaDataFileParser.ParseFileV2(fileList[i].Item2.DataSource.ReadData());

                    if (fileList[i].Item1.Contains("aim", StringComparison.InvariantCultureIgnoreCase))
                    { 
                    
                    }

                    var props = meteData.GetUnkItemsOfType("animated_prop", false);
                    if (props.Count() != 0)
                    { 
                        var res = MetaEntrySerializer.DeSerialize(props.First());
                    }

                    metaTable.Add( (fileList[i].Item1, meteData) );
                }
                catch
                {
                    metaTable.Add((fileList[i].Item1, null));
                }
            }

            // Create overview
            var overViewList = metaTable
                .SelectMany(x=>x.File.Items)
                .GroupBy(x => x.Name + "_" + x.Version)
                .Select(x => new 
                {
                    MetaTagName = x.First().Name + "_" + x.First().Version,
                    Count = x.Count(),
                    DecodedCorrectly = x.Count(x=>x.DecodedCorrectly == false) == 0
                })
                .OrderByDescending(x=>x.MetaTagName)
                .ToList();



            //var prop  =overViewList.Where(x => x.MetaTagName == "ANIMATED_PROP_11").FirstOrDefault();

          

            var outputTagList = new Dictionary<string, List<dynamic>>();
            foreach (var metaFile in metaTable)
            {
                foreach (var metaTag in metaFile.File.Items)
                {
                    var tagName = metaTag.Name + "_" + metaTag.Version;

                    if (outputTagList.ContainsKey(tagName) == false)
                        outputTagList[tagName] = new List<dynamic>();

                    dynamic rowItem = new ExpandoObject();
                    rowItem.Path = metaFile.Path;
                    rowItem.DecodedCorrectly = metaTag.DecodedCorrectly;

                    outputTagList[tagName].Add(rowItem);
                }
            }



            // Overview game:
            //      Tag, count, OK

            // Where
            //      N files Tag_version_game_count_hasError
            //      FileName | Values




            //var failedMeshRecords = new List<dynamic>();
            //var versionInfoRecords = new List<dynamic>();
            //var weightedMaterialRecords = new List<dynamic>();
            //
            //try
            //{
            //    var fullOutputDir = outputDir + gameDirectory + "\\";
            //
            //    CommonControls.Common.DirectoryHelper.EnsureCreated(outputDir);
            //    Write(failedMeshRecords, outputDir + "LoadResult.csv");
            //    Write(versionInfoRecords, outputDir + "VersionAndMaterialInfo.csv");
            //    Write(weightedMaterialRecords, outputDir + "WeightedMaterialInfo.csv");
            //
            //    MessageBox.Show($"Done - Created at {outputDir}");
            //}
            //catch
            //{
            //    MessageBox.Show("Unable to write reports to file!");
            //}
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
