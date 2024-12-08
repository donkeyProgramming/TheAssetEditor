using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Editors.Audio.AudioEditor.AudioProject;
using Editors.Audio.AudioEditor.ViewModels;
using Editors.Audio.Storage;
using static Editors.Audio.AudioEditor.AudioEditorHelpers;
using static Editors.Audio.AudioEditor.Converters.TooltipConverter;

namespace Editors.Audio.AudioEditor.DataGrids
{
    public class FullDataGridConfiguration
    {
        public static void ConfigureAudioProjectEditorFullDataGridForModdedStates(AudioEditorViewModel audioEditorViewModel, string moddedStateGroup)
        {
            var dataGrid = GetDataGridByTag(audioEditorViewModel.AudioProjectEditorFullDataGridTag);
            ClearDataGridColumns(audioEditorViewModel.AudioProjectEditorFullDataGridTag);

            // Column for the ModdedStateGroup.
            var stateGroupColumn = new DataGridTemplateColumn
            {
                Header = AddExtraUnderscoresToString(moddedStateGroup),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                IsReadOnly = true
            };

            var textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
            textBlockFactory.SetBinding(TextBlock.TextProperty, new Binding($"[{AddExtraUnderscoresToString(moddedStateGroup)}]"));
            textBlockFactory.SetValue(TextBlock.PaddingProperty, new Thickness(5, 2.5, 2.5, 5));

            var cellTemplate = new DataTemplate();
            cellTemplate.VisualTree = textBlockFactory;
            stateGroupColumn.CellTemplate = cellTemplate;
            dataGrid.Columns.Add(stateGroupColumn);
        }

        public static void ConfigureAudioProjectEditorFullDataGridForActionEventSoundBank(AudioEditorViewModel audioEditorViewModel, IAudioRepository audioRepository, SoundBank soundBank)
        {
            var dataGrid = GetDataGridByTag(audioEditorViewModel.AudioProjectEditorFullDataGridTag);
            ClearDataGridColumns(audioEditorViewModel.AudioProjectEditorFullDataGridTag);

            var columnsCount = 3;
            var columnWidth = columnsCount > 0 ? 1.0 / columnsCount : 1.0;

            // Column for the Event.
            var columnHeader = "Event";
            var eventNameColumn = new DataGridTemplateColumn
            {
                Header = columnHeader,
                Width = new DataGridLength(columnWidth, DataGridLengthUnitType.Star),
                IsReadOnly = true
            };

            var textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
            textBlockFactory.SetBinding(TextBlock.TextProperty, new Binding($"[{columnHeader}]"));
            textBlockFactory.SetValue(TextBlock.PaddingProperty, new Thickness(5, 2.5, 2.5, 5));

            var cellTemplate = new DataTemplate();
            cellTemplate.VisualTree = textBlockFactory;
            eventNameColumn.CellTemplate = cellTemplate;
            dataGrid.Columns.Add(eventNameColumn);

            // Column for Audio files TextBox with Tooltip.
            var soundsTextColumn = new DataGridTemplateColumn
            {
                Header = "Audio Files",
                Width = new DataGridLength(columnWidth, DataGridLengthUnitType.Star),
                IsReadOnly = true
            };

            var soundsCellTemplate = new DataTemplate();
            var soundsTextBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
            soundsTextBlockFactory.SetBinding(TextBlock.TextProperty, new Binding("[AudioFilesDisplay]"));

            // Create and set the tooltip binding.
            var tooltipBinding = new Binding("[AudioFiles]")
            {
                Mode = BindingMode.OneWay,
                Converter = new ConvertTooltipCollectionToString()
            };

            soundsTextBlockFactory.SetBinding(FrameworkElement.ToolTipProperty, tooltipBinding);
            soundsTextBlockFactory.SetValue(TextBlock.PaddingProperty, new Thickness(5, 2.5, 2.5, 5));
            soundsCellTemplate.VisualTree = soundsTextBlockFactory;
            soundsTextColumn.CellTemplate = soundsCellTemplate;
            dataGrid.Columns.Add(soundsTextColumn);
        }

        public static void ConfigureAudioProjectEditorFullDataGridForDialogueEvent(AudioEditorViewModel audioEditorViewModel, IAudioRepository audioRepository, IAudioProjectService audioProjectService, DialogueEvent dialogueEvent)
        {
            var dataGrid = GetDataGridByTag(audioEditorViewModel.AudioProjectEditorFullDataGridTag);
            ClearDataGridColumns(audioEditorViewModel.AudioProjectEditorFullDataGridTag);

            var stateGroups = audioRepository.DialogueEventsWithStateGroups[dialogueEvent.Name];
            var stateGroupsWithQualifiers = audioProjectService.DialogueEventsWithStateGroupsWithQualifiersAndStateGroupsRepository[dialogueEvent.Name];

            var stateGroupsCount = stateGroups.Count + 1; // e.g. + 1 is plus the number of columns in addition to the State Group columns e.g. plus 1 represents the Audio Files TextBox column
            var columnWidth = stateGroupsCount > 0 ? 1.0 / stateGroupsCount : 1.0;

            foreach (var kvp in stateGroupsWithQualifiers)
            {
                var stateGroupWithQualifier = kvp.Key;
                var stateGroupWithQualifierWithExtraUnderscores = AddExtraUnderscoresToString(stateGroupWithQualifier);

                // Column for State Group.
                var stateGroupColumn = new DataGridTemplateColumn
                {
                    Header = stateGroupWithQualifierWithExtraUnderscores,
                    Width = new DataGridLength(columnWidth, DataGridLengthUnitType.Star),
                    IsReadOnly = true
                };

                var textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
                textBlockFactory.SetBinding(TextBlock.TextProperty, new Binding($"[{stateGroupWithQualifierWithExtraUnderscores}]"));
                textBlockFactory.SetValue(TextBlock.PaddingProperty, new Thickness(5, 2.5, 2.5, 5));

                var cellTemplate = new DataTemplate();
                cellTemplate.VisualTree = textBlockFactory;
                stateGroupColumn.CellTemplate = cellTemplate;
                dataGrid.Columns.Add(stateGroupColumn);
            }

            // Column for Audio files TextBox with Tooltip.
            var soundsTextColumn = new DataGridTemplateColumn
            {
                Header = "Audio Files",
                Width = new DataGridLength(columnWidth, DataGridLengthUnitType.Star),
                IsReadOnly = true
            };

            var soundsCellTemplate = new DataTemplate();
            var soundsTextBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
            soundsTextBlockFactory.SetBinding(TextBlock.TextProperty, new Binding("[AudioFilesDisplay]"));

            // Create and set the tooltip binding.
            var tooltipBinding = new Binding("[AudioFiles]")
            {
                Mode = BindingMode.OneWay,
                Converter = new ConvertTooltipCollectionToString()
            };

            soundsTextBlockFactory.SetBinding(FrameworkElement.ToolTipProperty, tooltipBinding);
            soundsTextBlockFactory.SetValue(TextBlock.PaddingProperty, new Thickness(5, 2.5, 2.5, 5));
            soundsCellTemplate.VisualTree = soundsTextBlockFactory;
            soundsTextColumn.CellTemplate = soundsCellTemplate;
            dataGrid.Columns.Add(soundsTextColumn);
        }
    }
}
