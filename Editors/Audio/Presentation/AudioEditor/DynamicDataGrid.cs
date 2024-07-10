using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Editors.Audio.Presentation.AudioEditor.ViewModels;
using Editors.Audio.Storage;
using static Editors.Audio.Presentation.AudioEditor.AudioEditorViewModelHelpers;

namespace Editors.Audio.Presentation.AudioEditor
{
    public class DynamicDataGrid
    {
        public static DataGrid GetDataGrid()
        {
            var mainWindow = Application.Current.MainWindow;
            return FindVisualChild<DataGrid>(mainWindow, "AudioEditorDataGrid");
        }

        public static T FindVisualChild<T>(DependencyObject parent, string name) where T : DependencyObject
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild && child is FrameworkElement element && element.Name == name)
                    return typedChild;

                else
                {
                    var foundChild = FindVisualChild<T>(child, name);

                    if (foundChild != null)
                        return foundChild;
                }
            }

            return null;
        }

        public static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            while ((child = VisualTreeHelper.GetParent(child)) != null)
            {
                if (child is T parent)
                    return parent;
            }
            return null;
        }

        public static void ConfigureDataGrid(AudioEditorViewModel viewModel, IAudioRepository audioRepository)
        {
            var audioEditorDataGridItems = AudioEditorData.Instance.AudioEditorDataGridItems;
            var selectedAudioProjectEvent = AudioEditorData.Instance.SelectedAudioProjectEvent;

            var dataGrid = GetDataGrid();

            // DataGrid settings.
            dataGrid.CanUserAddRows = false;
            dataGrid.ItemsSource = audioEditorDataGridItems;

            // Clear existing data.
            dataGrid.Columns.Clear();
            audioEditorDataGridItems.Clear();

            var stateGroups = audioRepository.DialogueEventsWithStateGroups[selectedAudioProjectEvent];
            var stateGroupsWithQualifiers = DialogueEventsWithStateGroupsWithQualifiers[selectedAudioProjectEvent];

            var stateGroupsCount = stateGroups.Count();
            var columnWidth = stateGroupsCount > 0 ? 1.0 / (stateGroupsCount + 2) : 1.0;

            foreach (var (stateGroup, stateGroupWithQualifier) in stateGroups.Zip(stateGroupsWithQualifiers))
            {
                var column = new DataGridTemplateColumn
                {
                    Header = AddExtraUnderScoresToStateGroup(stateGroupWithQualifier),
                    CellTemplate = CreateStatesComboBoxTemplate(audioRepository, stateGroup, stateGroupWithQualifier),
                    Width = new DataGridLength(columnWidth, DataGridLengthUnitType.Star),
                };

                dataGrid.Columns.Add(column);
                Debug.WriteLine($"Added column for state group: {AddExtraUnderScoresToStateGroup(stateGroupWithQualifier)}");
            }

            // Add column for Audio TextBox.
            var soundsTextBoxColumn = new DataGridTemplateColumn
            {
                Header = "Audio Files",
                CellTemplate = CreateSoundsTextBoxTemplate(),
                Width = new DataGridLength(columnWidth, DataGridLengthUnitType.Star),
            };

            dataGrid.Columns.Add(soundsTextBoxColumn);
            Debug.WriteLine($"Added textBoxColumn for Sounds");

            // Add column for ... button.
            var soundsButtonColumn = new DataGridTemplateColumn
            {
                CellTemplate = CreateSoundsButtonTemplate(audioRepository),
                Width = 30.0,
                CanUserResize = false
            };

            dataGrid.Columns.Add(soundsButtonColumn);
            Debug.WriteLine($"Added '...' buttonColumn");

            // Add column for Remove button.
            var removeButtonColumn = new DataGridTemplateColumn
            {
                CellTemplate = CreateRemoveButtonTemplate(viewModel),
                Width = new DataGridLength(columnWidth, DataGridLengthUnitType.Star),
                MinWidth = 120,
            };

            dataGrid.Columns.Add(removeButtonColumn);
            Debug.WriteLine($"Added 'Remove' buttonColumn");
        }

        public static DataTemplate CreateStatesComboBoxTemplate(IAudioRepository audioRepository, string stateGroup, string stateGroupWithQualifier)
        {
            var template = new DataTemplate();
            var factory = new FrameworkElementFactory(typeof(ComboBox));
            var states = audioRepository.StateGroupsWithStates[stateGroup];

            var binding = new Binding($"[{AddExtraUnderScoresToStateGroup(stateGroupWithQualifier)}]")
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };

            // ComboBox settings.
            factory.SetBinding(ComboBox.SelectedItemProperty, binding);
            factory.SetValue(ComboBox.IsTextSearchEnabledProperty, true); // Enable text search.
            factory.SetValue(ComboBox.IsEditableProperty, true); // Enable text search.

            // Create the Loaded event handler to initialize and attach the TextChanged event.
            factory.AddHandler(ComboBox.LoadedEvent, new RoutedEventHandler((sender, args) =>
            {
                if (sender is ComboBox comboBox)
                {
                    comboBox.ItemsSource = states; // Set initial full list of States.

                    if (comboBox.Template.FindName("PART_EditableTextBox", comboBox) is TextBox textBox)
                    {
                        // TextChanged event for filtering items.
                        textBox.TextChanged += (s, e) =>
                        {
                            var filterText = textBox.Text;
                            var filteredItems = states.Where(item => item.Contains(filterText, StringComparison.OrdinalIgnoreCase)).ToList();

                            comboBox.ItemsSource = filteredItems; // Set filtered list of States.
                            comboBox.IsDropDownOpen = true; // Keep the drop-down open to show filtered results.
                        };
                    }
                }
            }));

            template.VisualTree = factory;

            return template;
        }

        public static DataTemplate CreateSoundsTextBoxTemplate()
        {
            var template = new DataTemplate();
            var factory = new FrameworkElementFactory(typeof(TextBox));
            factory.SetValue(FrameworkElement.NameProperty, "AudioFilesDisplay");

            var binding = new Binding("[AudioFilesDisplay]")
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };

            var tooltipBinding = new Binding("[AudioFiles]")
            {
                Mode = BindingMode.TwoWay,
                Converter = new ConvertToolTipCollectionToString()
            };

            factory.SetBinding(TextBox.TextProperty, binding);
            factory.SetBinding(TextBox.ToolTipProperty, tooltipBinding);

            factory.SetValue(TextBox.IsReadOnlyProperty, true);

            template.VisualTree = factory;

            return template;
        }

        public static DataTemplate CreateSoundsButtonTemplate(IAudioRepository audioRepository)
        {
            var template = new DataTemplate();
            var factory = new FrameworkElementFactory(typeof(Button));
            factory.SetValue(Button.ContentProperty, "...");
            factory.SetValue(Button.ToolTipProperty, "Browse wav files");

            // Handle button click event
            factory.AddHandler(Button.ClickEvent, new RoutedEventHandler((sender, e) =>
            {
                var button = sender as Button;
                var dataGridRow = FindVisualParent<DataGridRow>(button);

                if (dataGridRow != null)
                {
                    var textBox = FindVisualChild<TextBox>(dataGridRow, "AudioFilesDisplay");

                    if (textBox != null)
                    {
                        if (dataGridRow.DataContext is Dictionary<string, object> dataGridRowContext)
                            AudioEditorViewModel.AddAudioFiles(dataGridRowContext, textBox);
                    }
                }
            }));

            template.VisualTree = factory;

            return template;
        }

        public static DataTemplate CreateRemoveButtonTemplate(AudioEditorViewModel viewModel)
        {
            var template = new DataTemplate();
            var factory = new FrameworkElementFactory(typeof(Button));
            factory.SetValue(Button.ContentProperty, "Remove State Path");

            // Handle button click event
            factory.AddHandler(Button.ClickEvent, new RoutedEventHandler((sender, e) =>
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

        public class ConvertToolTipCollectionToString : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is IEnumerable<string> collection)
                    return string.Join(", ", collection.Select(item => $"\"{item}\""));

                else if (value is IList<string> list)
                    return string.Join(", ", list.Select(item => $"\"{item}\""));

                else if (value is IEnumerable enumerable)
                {
                    var stringValue = new StringBuilder();

                    foreach (var item in enumerable)
                    {
                        stringValue.Append($"\"{item.ToString()}\"");
                        stringValue.Append(", ");
                    }

                    return stringValue.ToString().TrimEnd([',', ' ']);
                }

                return string.Empty;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return null;
            }
        }
    }
}
