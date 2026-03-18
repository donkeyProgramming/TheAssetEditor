using System.Collections.ObjectModel;
using Editors.AnimationMeta.Presentation.View;
using Shared.Core.Events;
using Shared.GameFormats.AnimationMeta.Parsing;
using System.Windows;
using Editors.AnimationMeta.Presentation;

namespace Editors.AnimationMeta.MetaEditor.Commands
{
    internal class NewEntryCommand : IUiCommand
    {
        private readonly MetaDataFileParser _metaDataFileParser;
        private readonly IMetaDataDatabase _metaDataDatabase;

        public NewEntryCommand(MetaDataFileParser metaDataFileParser, IMetaDataDatabase metaDataDatabase) 
        {
            _metaDataFileParser = metaDataFileParser;
            _metaDataDatabase = metaDataDatabase;
        }

        public void Execute(MetaDataEditorViewModel controller)
        {
            var dialog = new NewMetaDataEntryWindow() { Owner = Application.Current.MainWindow };
            var allDefs = _metaDataDatabase.GetSupportedTypes();

            var model = new NewTagWindowViewModel
            {
                Items = new ObservableCollection<string>(allDefs)
            };
            dialog.DataContext = model;

            var res = dialog.ShowDialog();
            if (res.HasValue && res.Value == true)
            {
                var newEntry = _metaDataFileParser.CreateDefault(model.SelectedItem);
                controller.ParsedFile.Attributes.Add(newEntry);
                controller.UpdateView();
                controller.SelectedTag = controller.Tags.LastOrDefault();   
            }

            dialog.DataContext = null;
        }

    }
}
