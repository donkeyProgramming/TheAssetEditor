using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using Editors.Audio.AudioEditor.Data.AudioProjectDataService;
using Editors.Audio.AudioEditor.Data.AudioProjectService;
using Editors.Audio.Storage;

namespace Editors.Audio.AudioEditor.Data
{
    public enum DataGridColumnType
    {
        EditableTextBox,
        StateGroupEditableComboBox,
        ReadOnlyTextBlock
    }

    public class DataGridHelpers
    {
        public static DataGrid InitialiseDataGrid(string dataGridTag)
        {
            var dataGrid = GetDataGridByTag(dataGridTag);
            ClearDataGridColumns(dataGrid);
            ClearDataGridContextMenu(dataGrid);
            return dataGrid;
        }

        public static DataGridTemplateColumn CreateColumn(AudioProjectDataServiceParameters parameters, string columnHeader, double columnWidth, DataGridColumnType columnType, List<string> states = null)
        {
            var column = new DataGridTemplateColumn
            {
                Header = columnHeader,
                Width = new DataGridLength(columnWidth, DataGridLengthUnitType.Star),
                IsReadOnly = columnType == DataGridColumnType.ReadOnlyTextBlock
            };

            if (columnType == DataGridColumnType.EditableTextBox)
                column.CellTemplate = CreateEditableTextBoxTemplate(parameters.AudioEditorViewModel, parameters.AudioProjectService, parameters.AudioRepository, columnHeader);
            else if (columnType == DataGridColumnType.StateGroupEditableComboBox)
                column.CellTemplate = CreateStatesComboBoxTemplate(parameters.AudioEditorViewModel, parameters.AudioProjectService, parameters.AudioRepository, columnHeader, states);
            else if (columnType == DataGridColumnType.ReadOnlyTextBlock)
                column.CellTemplate = CreateReadOnlyTextBlockTemplate(columnHeader);
            return column;
        }

        public static DataTemplate CreateStatesComboBoxTemplate(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, IAudioRepository audioRepository, string stateGroupWithQualifierWithExtraUnderscores, List<string> states)
        {
            var template = new DataTemplate();
            var factory = new FrameworkElementFactory(typeof(ComboBox));

            factory.SetValue(ScrollViewer.CanContentScrollProperty, true);
            factory.SetValue(VirtualizingStackPanel.IsVirtualizingProperty, true);
            factory.SetValue(VirtualizingStackPanel.VirtualizationModeProperty, VirtualizationMode.Recycling);

            var comboBoxStyle = Application.Current.TryFindResource(typeof(ComboBox)) as Style;
            factory.SetValue(Control.StyleProperty, comboBoxStyle);

            // Use a VirtualizingStackPanel for the drop-down items
            var itemsPanelFactory = new FrameworkElementFactory(typeof(VirtualizingStackPanel));
            factory.SetValue(ItemsControl.ItemsPanelProperty, new ItemsPanelTemplate(itemsPanelFactory));

            // Initially, show the full list
            var fullObservableStates = new ObservableCollection<string>(states);
            factory.SetValue(ItemsControl.ItemsSourceProperty, fullObservableStates);

            factory.SetValue(ComboBox.IsEditableProperty, true);
            factory.SetValue(ItemsControl.IsTextSearchEnabledProperty, true);

            // Setting the SelectedItem and Text bindings allows us to determine control them elsewhere, for example to show "Any" by default
            factory.SetBinding(Selector.SelectedItemProperty, new Binding($"[{stateGroupWithQualifierWithExtraUnderscores}]")
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
            factory.SetBinding(ComboBox.TextProperty, new Binding($"[{stateGroupWithQualifierWithExtraUnderscores}]")
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });

            factory.AddHandler(FrameworkElement.LoadedEvent, new RoutedEventHandler((sender, args) =>
            {
                if (sender is ComboBox comboBox)
                {
                    if (comboBox.Template.FindName("PART_EditableTextBox", comboBox) is TextBox textBox)
                    {
                        // When text changes, filter asynchronously
                        textBox.TextChanged += async (s, e) =>
                        {
                            audioEditorViewModel.AudioProjectEditorViewModel.SetAddRowButtonEnablement();

                            var filterText = textBox.Text;
                            var filteredItems = await Task.Run(() =>
                            {
                                if (string.IsNullOrWhiteSpace(filterText))
                                    return fullObservableStates.ToList();
                                else
                                    return fullObservableStates.Where(item => item.Contains(filterText, StringComparison.OrdinalIgnoreCase)).ToList();
                            });

                            comboBox.ItemsSource = filteredItems;

                            // Open the drop down to display the results
                            if (!comboBox.IsDropDownOpen)
                                comboBox.IsDropDownOpen = true;
                        };

                        // When the user leaves the TextBox, validate the text
                        textBox.LostFocus += (s, e) =>
                        {
                            var match = fullObservableStates.FirstOrDefault(item => string.Equals(item, textBox.Text, StringComparison.OrdinalIgnoreCase));
                            if (match == null)
                            {
                                if (states.Contains("Any"))
                                {
                                    comboBox.SelectedItem = "Any";
                                    comboBox.Text = "Any";
                                }
                                else
                                {
                                    comboBox.SelectedItem = null;
                                    comboBox.Text = string.Empty;
                                }
                            }
                        };
                    }
                }
            }));

            template.VisualTree = factory;
            return template;
        }

        public static DataTemplate CreateEditableTextBoxTemplate(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, IAudioRepository audioRepository, string columnHeader)
        {
            var template = new DataTemplate();
            var factory = new FrameworkElementFactory(typeof(TextBox));

            factory.SetBinding(TextBox.TextProperty, new Binding($"[{columnHeader}]")
            {
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Mode = BindingMode.TwoWay
            });

            factory.SetValue(Control.PaddingProperty, new Thickness(5, 2.5, 5, 2.5));

            factory.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler((s, e) =>
            {
                audioEditorViewModel.AudioProjectEditorViewModel.SetAddRowButtonEnablement();
            }));

            template.VisualTree = factory;
            return template;
        }

        public static DataTemplate CreateReadOnlyTextBlockTemplate(string columnHeader)
        {
            var template = new DataTemplate();
            var factory = new FrameworkElementFactory(typeof(TextBlock));

            factory.SetBinding(TextBlock.TextProperty, new Binding($"[{columnHeader}]"));
            factory.SetValue(TextBlock.PaddingProperty, new Thickness(5, 3.5, 5, 3.5));

            template.VisualTree = factory;
            return template;
        }

        public static void CreateContextMenu(AudioProjectDataServiceParameters parameters, DataGrid dataGrid)
        {
            var contextMenu = new ContextMenu();

            var copyMenuItem = new MenuItem
            {
                Header = "Copy",
                Command = parameters.AudioEditorViewModel.AudioProjectViewerViewModel.CopyRowsCommand
            };

            BindingOperations.SetBinding(copyMenuItem, UIElement.IsEnabledProperty,
                new Binding("IsCopyEnabled")
                {
                    Source = parameters.AudioEditorViewModel.AudioProjectViewerViewModel,
                    Mode = BindingMode.OneWay
                });

            var pasteMenuItem = new MenuItem
            {
                Header = "Paste",
                Command = parameters.AudioEditorViewModel.AudioProjectViewerViewModel.PasteRowsCommand
            };

            BindingOperations.SetBinding(pasteMenuItem, UIElement.IsEnabledProperty,
                new Binding("IsPasteEnabled")
                {
                    Source = parameters.AudioEditorViewModel.AudioProjectViewerViewModel,
                    Mode = BindingMode.OneWay
                });

            contextMenu.Items.Add(copyMenuItem);
            contextMenu.Items.Add(pasteMenuItem);

            dataGrid.ContextMenu = contextMenu;
        }

        public static DataGrid GetDataGridByTag(string dataGridTag)
        {
            var mainWindow = Application.Current.MainWindow;
            return FindVisualChild<DataGrid>(mainWindow, dataGridTag);
        }

        public static void ClearDataGridCollection(ObservableCollection<Dictionary<string, string>> dataGrid)
        {
            dataGrid.Clear();
        }

        public static void ClearDataGridColumns(DataGrid dataGrid)
        {
            dataGrid.Columns.Clear();
        }

        public static void ClearDataGridContextMenu(DataGrid dataGrid)
        {
            if (dataGrid.ContextMenu != null)
                dataGrid.ContextMenu.Items.Clear();
        }

        public static T FindVisualChild<T>(DependencyObject parent, string identifier) where T : DependencyObject
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild && child is FrameworkElement element)
                {
                    // Check both Name and Tag because DataGrids use Tag as Name can't be set via a binding for some reason...
                    if (element.Name == identifier || element.Tag?.ToString() == identifier)
                        return typedChild;
                }

                var foundChild = FindVisualChild<T>(child, identifier);
                if (foundChild != null)
                    return foundChild;
            }
            return null;
        }
    }
}
