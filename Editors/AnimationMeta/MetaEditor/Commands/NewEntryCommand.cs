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
        private readonly MetaDataTagDeSerializer _metaDataTagDeSerializer;

        public NewEntryCommand(MetaDataTagDeSerializer metaDataTagDeSerializer) 
        {
            _metaDataTagDeSerializer = metaDataTagDeSerializer;
        }

        public void Execute(MetaDataEditorViewModel controller)
        {
            var dialog = new NewMetaDataEntryWindow() { Owner = Application.Current.MainWindow };
            var allDefs = _metaDataTagDeSerializer.GetSupportedTypes();

            var model = new NewTagWindowViewModel
            {
                Items = new ObservableCollection<string>(allDefs)
            };
            dialog.DataContext = model;

            var res = dialog.ShowDialog();
            if (res.HasValue && res.Value == true)
            {
                var newEntry = _metaDataTagDeSerializer.CreateDefault(model.SelectedItem);
                var newTagView = new MetaDataEntry(newEntry, _metaDataTagDeSerializer);
                controller.Tags.Add(newTagView);
            }

            dialog.DataContext = null;
        }

    }
}
