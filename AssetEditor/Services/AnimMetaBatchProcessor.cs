using Common;
using CommonControls.Services;
using Filetypes.ByteParsing;
using FileTypes.DB;
using FileTypes.MetaData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AssetEditor.Services
{
    class AnimMetaBatchProcessor
    {



        class Entry
        {
            //public string FileName { get; set; }
            public List<string> Data { get; set; } = new List<string>();
            public List<string> Errors { get; set; } = new List<string>();
        }


        string GetAsCommaSeperatedList(string filename, MetaEntry entry)
        {
            var output = new List<string>();
            string errorStr = "";
            var chuck = new ByteChunk(entry.GetData());
            int index = 0;
            foreach (var field in entry.Schema.ColumnDefinitions)
            {
                var parser = ByteParserFactory.Create(field.Type);
                try
                {
                    chuck.Read(parser, out var value, out var error);
                    if (string.IsNullOrWhiteSpace(error) == false)
                    {
                        output.Add(error);
                    }
                    else
                    {
                       // if (value.Contains(","))
                       //     value = "{\"" + value + "\"}";
                        output.Add(value);
                    }
                }
                catch (Exception e)
                {
                    errorStr =  field.Name + $"at index {index}:" + e.Message;
                    break;
                }
                index++;
            }

            if (chuck.BytesLeft != 0 && errorStr != "")
                errorStr = "Bytes left = " + chuck.BytesLeft;

            var finalOutput = new List<string>();
            finalOutput.Add(filename);
            finalOutput.Add(errorStr);
            finalOutput.AddRange(output);
            return string.Join("|", finalOutput);
        }

        string GenerateColoumNames(MetaEntry entry)
        {
            var output = new List<string>();
            output.Add("Filename");
            output.Add("Error");

            foreach (var field in entry.Schema.ColumnDefinitions)
                output.Add(field.Name);
            return string.Join("|", output);
        }

        string GenerateErrorColoumNames()
        {
            var output = new List<string>();
            output.Add("Filename");
            output.Add("Error");
            return string.Join("|", output);
        }

        void EnsureTagCreated(Dictionary<string, Entry> output, string tagName)
        {
            if (output.ContainsKey(tagName) == false)
                output[tagName] = new Entry();
        }

        public void BatchProcess(PackFileService pfs, SchemaManager schemaManager, string gameName)
        {
            var allMetaFiles = pfs.FindAllWithExtention(".meta");
            allMetaFiles = allMetaFiles.Where(f => f.Name.Contains("anm.meta")).ToList();

            Dictionary<string, Entry> output = new Dictionary<string, Entry>();

            foreach (var file in allMetaFiles)
            { 
                var metaFile = MetaDataFileParser.ParseFile(file.DataSource.ReadData(), schemaManager, false);
                var fullPath = pfs.GetFullPath(file);

                foreach (var metaEntry in metaFile.Items)
                {
                    var entryName = metaEntry.Name + "_v" + metaEntry.Version;
                    EnsureTagCreated(output, entryName);

                    if (metaEntry.DecodedCorrectly == false)
                    {
                        if (output[entryName].Errors.Count == 0)
                            output[entryName].Errors.Add(GenerateErrorColoumNames());
                        output[entryName].Errors.Add($"{fullPath}| Error - {metaEntry.GetData().Length} bytes");
                    }
                    else 
                    {
                        var entry = metaEntry as MetaEntry;
                        if (output[entryName].Data.Count == 0)
                            output[entryName].Data.Add(GenerateColoumNames(entry));
                            
                        var valueList = GetAsCommaSeperatedList(fullPath, entry);
                        output[entryName].Data.Add(valueList);
                    }
                }
            }

            string outputFolder = @"C:\temp\metaExport_" + gameName + @"_v2\";
            DirectoryHelper.EnsureCreated(outputFolder);
            foreach (var item in output)
            {
                if (item.Value.Data.Count != 0)
                {
                    var fileName = item.Key + "_" + item.Value.Data.Count + ".csv";
                    var stringbuilder = new StringBuilder();
                    stringbuilder.AppendLine("sep=|");

                    foreach (var dataItem in item.Value.Data)
                        stringbuilder.AppendLine(dataItem);

                    File.WriteAllText(outputFolder + fileName, stringbuilder.ToString());
                }

                if (item.Value.Errors.Count != 0)
                {
                    var fileName = item.Key + "_Error_" + item.Value.Errors.Count + ".csv";
                    var stringbuilder = new StringBuilder();
                    stringbuilder.AppendLine("sep=|");

                    foreach (var errorItem in item.Value.Errors)
                        stringbuilder.AppendLine(errorItem);

                    File.WriteAllText(outputFolder + fileName, stringbuilder.ToString());
                }
            }

            return;
                
                /*
                
                foreach (var file in allMetaFiles)
                {
                    try
                    {
                        var fileName = pfs.GetFullPath(file);
                        var fileContent = file.DataSource.ReadData();
                        var res = MetaDataFileParser.ParseFile(fileContent, fileName);
                        allMetaData.Add(res);

                        foreach (var resultDataItem in res.TagItems)
                        {
                            var masterDataItem = master.TagItems.FirstOrDefault(x => x.Name == resultDataItem.Name && x.Version == resultDataItem.Version);
                            if (masterDataItem == null)
                            {
                                master.TagItems.Add(new MetaDataTagItem() { Name = resultDataItem.Name, Version = resultDataItem.Version });
                                masterDataItem = master.TagItems.Last();
                            }

                            foreach (var tag in resultDataItem.DataItems)
                            {
                                masterDataItem.DataItems.Add(tag);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        errorList.Add(new Tuple<string, string>(_pf.GetFullPath(file), e.Message));
                    }
                }

                var outputFolder = @"C:\temp\MetaDataDecoded\";

                foreach (var propSetType in master.TagItems)
                {
                    var filename = $"{propSetType.DisplayName}_{propSetType.DataItems.Count}.csv";
                    StringBuilder stringBuilder = new StringBuilder();

                    var tableDef = _schemaManager.GetMetaDataDefinition(propSetType.Name, propSetType.Version);
                    if (tableDef != null)
                    {
                        var headers = new List<string>() { "FileName", "Error" };

                        for (int outputRowIndex = 0; outputRowIndex < tableDef.ColumnDefinitions.Count; outputRowIndex++)
                            headers.Add(tableDef.ColumnDefinitions[outputRowIndex].Name + " - " + tableDef.ColumnDefinitions[outputRowIndex].Type.ToString());
                        WriteRow(stringBuilder, headers);

                        for (int i = 0; i < propSetType.DataItems.Count(); i++)
                        {
                            var outputRow = new List<string>();
                            for (int outputRowIndex = 0; outputRowIndex < tableDef.ColumnDefinitions.Count + 2; outputRowIndex++)
                                outputRow.Add("");

                            var dataRow = new DataTableRow(propSetType.DisplayName, i, tableDef.ColumnDefinitions, propSetType.DataItems[i]);
                            outputRow[0] = propSetType.DataItems[i].ParentFileName;
                            outputRow[1] = dataRow.GetError();
                            if (string.IsNullOrEmpty(outputRow[1]))
                            {
                                for (int valueIndex = 0; valueIndex < dataRow.Values.Count; valueIndex++)
                                    outputRow[valueIndex + 2] = dataRow.Values[valueIndex].Value;
                            }
                            WriteRow(stringBuilder, outputRow);
                        }
                    }
                    else
                    {
                        var headers = new List<string>() { "FileName", "Size" };
                        WriteRow(stringBuilder, headers);
                        for (int i = 0; i < propSetType.DataItems.Count(); i++)
                        {
                            var outputRow = new List<string>() { propSetType.DataItems[i].ParentFileName, propSetType.DataItems[i].Size.ToString() };
                            WriteRow(stringBuilder, outputRow);
                        }
                    }

                    DirectoryHelper.EnsureCreated(outputFolder);
                    File.WriteAllText(outputFolder + filename, stringBuilder.ToString());
                }


                master.TagItems = master.TagItems.OrderBy(x => x.DisplayName).ToList();
                ActiveMentaDataContent.File = master;
            }*/
        }
    }
}
