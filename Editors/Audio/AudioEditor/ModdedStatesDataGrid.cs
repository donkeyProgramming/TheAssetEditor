using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Editors.Audio.AudioEditor.ViewModels;
using static Editors.Audio.AudioEditor.AudioEditorHelpers;
using static Editors.Audio.AudioEditor.DataGridHelpers;

namespace Editors.Audio.AudioEditor
{
    public class ModdedStatesDataGrid
    {
        public ModdedStatesDataGrid()
        {
        }

        public static void ConfigureDataGrid(AudioEditorViewModel viewModel)
        {
            var dataGridData = viewModel.DataGridData;
            var selectedAudioProjectEvent = viewModel.SelectedAudioProjectEvent;

            var dataGrid = GetDataGrid();

            // DataGrid settings:
            dataGrid.CanUserAddRows = false; // Setting this bastard to false ensures that data won't go missing from the last row when a new row is added. Wtf WPF.
            dataGrid.ItemsSource = dataGridData;

            // Clear existing data:
            dataGrid.Columns.Clear();
            dataGridData.Clear();

            var stateGroups = StatesProjectData.ModdedStateGroups;

            var stateGroupsCount = stateGroups.Count() + 3;
            var columnWidth = stateGroupsCount > 0 ? 1.0 / stateGroupsCount : 1.0;

            // Column for Remove State StatePath button:
            var removeButtonColumn = new DataGridTemplateColumn
            {
                CellTemplate = CreateRemoveRowButtonTemplate(viewModel),
                Width = 20,
                CanUserResize = false
            };

            dataGrid.Columns.Add(removeButtonColumn);

            foreach (var stateGroup in stateGroups)
            {
                // Column for State Group:
                var column = new DataGridTemplateColumn
                {
                    Header = AddExtraUnderscoresToString(stateGroup),
                    CellTemplate = CreateStatesTextBoxTemplate(stateGroup),
                    Width = new DataGridLength(columnWidth, DataGridLengthUnitType.Star),
                };

                dataGrid.Columns.Add(column);
            }
        }

        public static DataTemplate CreateStatesTextBoxTemplate(string stateGroup)
        {
            var template = new DataTemplate();
            var factory = new FrameworkElementFactory(typeof(TextBox));

            var binding = new Binding($"[{AddExtraUnderscoresToString(stateGroup)}]")
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };

            factory.SetBinding(TextBox.TextProperty, binding);

            template.VisualTree = factory;

            return template;
        }

        public static DataTemplate CreateRemoveRowButtonTemplate(AudioEditorViewModel viewModel)
        {
            var template = new DataTemplate();
            var factory = new FrameworkElementFactory(typeof(Button));
            factory.SetValue(ContentControl.ContentProperty, "✖");
            factory.SetValue(Control.FontFamilyProperty, new FontFamily("Segoe UI Symbol")); // This font supports the character.
            factory.SetValue(FrameworkElement.ToolTipProperty, "Remove State StatePath");

            // Handle button click event
            factory.AddHandler(System.Windows.Controls.Primitives.ButtonBase.ClickEvent, new RoutedEventHandler((sender, e) =>
            {
                var button = sender as Button;
                var dataGridRow = FindVisualParent<DataGridRow>(button);

                if (dataGridRow != null && viewModel != null)
                {
                    if (dataGridRow.DataContext is Dictionary<string, object> dataGridRowContext)
                        viewModel.RemoveStatePath(dataGridRowContext);
                }
            }));

            template.VisualTree = factory;

            return template;
        }
    }
}
