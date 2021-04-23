using Common;
using Filetypes;
using Filetypes.ByteParsing;
using GalaSoft.MvvmLight.Command;
using AnimMetaEditor.DataType;
using AnimMetaEditor.ViewModels.Data;
using AnimMetaEditor.Views.MetadataTableViews;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommonControls.Services;
using CommonControls;
using System.Linq;
using CommonControls.Common;

namespace AnimMetaEditor.ViewModels
{
    public class MainDecoderViewModel : NotifyPropertyChangedImpl, IEditorViewModel
    {
        public ActiveMetaDataContentModel ActiveMentaDataContent { get; set; } = new ActiveMetaDataContentModel();
        public TableDefinitionModel ActiveTableDefinition = new TableDefinitionModel();

        public MetaDataTable DataTable { get; set; }
        public TableDefinitionEditor TableDefinitionEditor { get; set; }
        public FieldExplorer FieldExplorer { get; set; }
        public string DisplayName { get; set; } = "MetaDecoder";
        public IPackFile MainFile { get; set; }

        PackFileService _pf;
        public MainDecoderViewModel(SchemaManager schemaManager,/* MetaDataFile metaDataFile,*/ PackFileService pf, bool allTablesReadOnly = true)
        {
            _pf = pf;

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
                allMetaFiles = allMetaFiles.Where(f => f.Name.Contains("anm.meta")).ToList();
                List<MetaDataFile> allMetaData = new List<MetaDataFile>();


                MetaDataFile master = new MetaDataFile()
                {
                    FileName = "Master collection"
                };

                foreach (var file in allMetaFiles)
                {
                    var res = MetaDataFileParser.ParseFile(file, _pf);
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

                //var v = allMetaData.GroupBy(X => X.TagItems.Select(d => d.Name)).ToList();

                //foreach (var item in master.TagItems)
                //{
                //    var versions = item.DataItems.Select(x => x.Version).Distinct().ToList();
                //    var size = item.DataItems.Select(x => x.Size).Distinct().ToList();
                //}

                master.TagItems = master.TagItems.OrderBy(x => x.DisplayName).ToList();

                ActiveMentaDataContent.File = master;
            }
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
