using System.Collections.ObjectModel;
using Editors.AnimationMeta.Presentation.View;
using Shared.Core.Events;
using Shared.GameFormats.AnimationMeta.Parsing;
using System.Windows;

namespace Editors.AnimationMeta.Presentation.Commands
{
    internal class NewEntryCommand : IUiCommand
    {
        public void Execute(MetaDataEditorViewModel controller)
        {
            var dialog = new NewMetaDataEntryWindow() { Owner = Application.Current.MainWindow };
            var allDefs = MetaDataTagDeSerializer.GetSupportedTypes();

            var model = new NewTagWindowViewModel
            {
                Items = new ObservableCollection<string>(allDefs)
            };
            dialog.DataContext = model;

            var res = dialog.ShowDialog();
            if (res.HasValue && res.Value == true)
            {
                var newEntry = MetaDataTagDeSerializer.CreateDefault(model.SelectedItem);
                var newTagView = new MetaDataEntry(newEntry);
                controller.Tags.Add(newTagView);
            }

            dialog.DataContext = null;
        }

    }
}
