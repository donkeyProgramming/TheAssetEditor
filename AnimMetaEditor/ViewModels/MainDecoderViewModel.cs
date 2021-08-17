using Common;
using AnimMetaEditor.ViewModels.Data;
using System;
using System.Collections.Generic;
using CommonControls.Services;
using System.Linq;
using CommonControls.Common;
using System.Text;
using System.IO;
using FileTypes.DB;
using FileTypes.MetaData;

namespace AnimMetaEditor.ViewModels
{
    public class MainDecoderViewModel : NotifyPropertyChangedImpl, IEditorViewModel
    {
        public ActiveMetaDataContentModel ActiveMentaDataContent { get; set; } = new ActiveMetaDataContentModel();
        public TableDefinitionModel ActiveTableDefinition = new TableDefinitionModel();

        SchemaManager _schemaManager;
        public MetaDataTable DataTable { get; set; }
        public TableDefinitionEditor TableDefinitionEditor { get; set; }
        public FieldExplorer FieldExplorer { get; set; }
        public string DisplayName { get; set; } = "MetaDecoder";
        public IPackFile MainFile { get; set; }

        PackFileService _pf;
        public MainDecoderViewModel(SchemaManager schemaManager,/* MetaDataFile metaDataFile,*/ PackFileService pf, bool allTablesReadOnly = true)
        {
            _pf = pf;
            _schemaManager = schemaManager;

            TableDefinitionEditor = new TableDefinitionEditor(schemaManager, ActiveMentaDataContent, ActiveTableDefinition);
            DataTable = new MetaDataTable(ActiveTableDefinition, ActiveMentaDataContent, pf, allTablesReadOnly);
            FieldExplorer = new FieldExplorer(TableDefinitionEditor, ActiveMentaDataContent, ActiveTableDefinition);

            //ActiveMentaDataContent.File = metaDataFile;
        }

        public void ConfigureAsDecoder()
        {
            using (new WaitCursor())
            {
                var allMetaFiles = _pf.FindAllWithExtention(".meta");
                //allMetaFiles = allMetaFiles.Where(f => f.Name.Contains("anm.meta")).ToList();
                List<MetaDataFile> allMetaData = new List<MetaDataFile>();


                MetaDataFile master = new MetaDataFile()
                {
                    FileName = "Master collection"
                };

                var errorList = new List<Tuple<string, string>>();
                
                foreach (var file in allMetaFiles)
                {
                    try
                    {
                        var fileName = _pf.GetFullPath(file);
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
                        errorList.Add(new Tuple<string,string>(_pf.GetFullPath(file) ,e.Message));
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
                        var headers = new List<string>() {"FileName", "Size" };
                        WriteRow(stringBuilder, headers);
                        for (int i = 0; i < propSetType.DataItems.Count(); i++)
                        {
                            var outputRow = new List<string>() { propSetType.DataItems[i].ParentFileName, propSetType.DataItems[i].Size.ToString()};
                            WriteRow(stringBuilder, outputRow);
                        }
                    }

                    DirectoryHelper.EnsureCreated(outputFolder);
                    File.WriteAllText(outputFolder + filename, stringBuilder.ToString());
                }


                master.TagItems = master.TagItems.OrderBy(x => x.DisplayName).ToList();
                ActiveMentaDataContent.File = master;
            }
        }

        void WriteRow(StringBuilder builder, List<string> data)
        {
            var str = String.Join(",", data) + "\n";
            builder.Append(str);
        }

        public bool Save()
        {
            return false;
        }

        public void Close()
        {
            //throw new NotImplementedException();
        }

        public bool HasUnsavedChanges()
        {
            return false;
        }
    }
}
