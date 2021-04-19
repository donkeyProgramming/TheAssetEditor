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

namespace AnimMetaEditor.ViewModels
{
    class MainViewModel : NotifyPropertyChangedImpl
    {
        public ActiveMetaDataContentModel ActiveMentaDataContent { get; set; } = new ActiveMetaDataContentModel();
        public TableDefinitionModel ActiveTableDefinition = new TableDefinitionModel();

        public MetaDataTable DataTable { get; set; }
        public TableDefinitionEditor TableDefinitionEditor { get; set; }
        public FieldExplorer FieldExplorer { get; set; }

        public MainViewModel(MetaDataFile metaDataFile, PackFileService pf, bool allTablesReadOnly)
        {
            TableDefinitionEditor = new TableDefinitionEditor(ActiveMentaDataContent, ActiveTableDefinition);
            DataTable = new MetaDataTable(ActiveTableDefinition, ActiveMentaDataContent, pf, allTablesReadOnly);
            FieldExplorer = new FieldExplorer(TableDefinitionEditor, ActiveMentaDataContent, ActiveTableDefinition);

            ActiveMentaDataContent.File = metaDataFile;
        }
    }
}
