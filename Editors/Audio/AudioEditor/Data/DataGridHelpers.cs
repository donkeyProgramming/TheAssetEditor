using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Editors.Audio.AudioEditor.Data;
using Editors.Audio.Storage;
using static Editors.Audio.AudioEditor.ButtonEnablement;
using static Editors.Audio.AudioEditor.Converters.TooltipConverter;
using static Editors.Audio.AudioEditor.Data.AudioProjectDataService;

namespace Editors.Audio.AudioEditor
{
    public enum DataGridColumnType
    {
        EditableTextBox,
        StateGroupEditableComboBox,
        AudioFilesEditableTextBox,
        ReadOnlyTextBlock,
        AudioFilesReadOnlyTextBlock
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
                IsReadOnly = columnType == DataGridColumnType.ReadOnlyTextBlock || columnType == DataGridColumnType.AudioFilesReadOnlyTextBlock
            };

            if (columnType == DataGridColumnType.EditableTextBox)
                column.CellTemplate = CreateEditableTextBoxTemplate(parameters.AudioEditorViewModel, parameters.AudioProjectService, parameters.AudioRepository, columnHeader);
            else if (columnType == DataGridColumnType.StateGroupEditableComboBox)
                column.CellTemplate = CreateStatesComboBoxTemplate(parameters.AudioEditorViewModel, parameters.AudioProjectService, parameters.AudioRepository, columnHeader, states);
            else if (columnType == DataGridColumnType.AudioFilesEditableTextBox)
                column.CellTemplate = CreateEditableAudioFilesTextBoxTemplate();
            else if (columnType == DataGridColumnType.ReadOnlyTextBlock)
                column.CellTemplate = CreateReadOnlyTextBlockTemplate(columnHeader);
            else if (columnType == DataGridColumnType.AudioFilesReadOnlyTextBlock)
                column.CellTemplate = CreateReadOnlyAudioFilesTextBoxTemplate();
            return column;
        }

        public static DataTemplate CreateStatesComboBoxTemplate(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, IAudioRepository audioRepository, string stateGroupWithQualifierWithExtraUnderscores, List<string> states)
        {
            var template = new DataTemplate();
            var factory = new FrameworkElementFactory(typeof(ComboBox));

            var observableStates = new ObservableCollection<string>(states);
            var collectionView = CollectionViewSource.GetDefaultView(observableStates);

            var isFiltered = false;
            var selectionMade = false;

            factory.SetValue(ItemsControl.ItemsSourceProperty, collectionView);
            factory.SetValue(ComboBox.IsEditableProperty, true);
            factory.SetValue(ItemsControl.IsTextSearchEnabledProperty, true);

            factory.SetBinding(Selector.SelectedItemProperty, new Binding($"[{stateGroupWithQualifierWithExtraUnderscores}]")
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
                        var debounceTimer = new DispatcherTimer
                        {
                            Interval = TimeSpan.FromMilliseconds(200),
                            IsEnabled = false
                        };

                        var lastFilterText = string.Empty;

                        textBox.TextChanged += (s, e) =>
                        {
                            if (e.Changes.Count > 0 && !(Keyboard.IsKeyDown(Key.Up) || Keyboard.IsKeyDown(Key.Down)))
                            {
                                lastFilterText = textBox.Text;
                                if (!selectionMade)
                                {
                                    debounceTimer.Stop();
                                    debounceTimer.Start();
                                }
                                selectionMade = false;

                                SetIsAddRowButtonEnabled(audioEditorViewModel, audioProjectService, audioRepository);
                            }
                        };

                        debounceTimer.Tick += (s, e) =>
                        {
                            debounceTimer.Stop();
                            collectionView.Filter = item =>
                            {
                                if (item is string state)
                                {
                                    return state.Contains(lastFilterText, StringComparison.OrdinalIgnoreCase);
                                }
                                return false;
                            };

                            isFiltered = !string.IsNullOrEmpty(lastFilterText);

                            if (!comboBox.IsDropDownOpen && !selectionMade)
                            {
                                comboBox.IsDropDownOpen = true;
                            }
                        };

                        textBox.LostFocus += (s, e) =>
                        {
                            var finalText = textBox.Text;

                            if (!string.IsNullOrWhiteSpace(finalText) && !states.Contains(finalText))
                            {
                                textBox.Text = string.Empty;
                                comboBox.SelectedItem = null;
                            }
                        };

                        comboBox.SelectionChanged += (s, e) =>
                        {
                            selectionMade = true;

                            if (comboBox.IsDropDownOpen)
                                comboBox.IsDropDownOpen = false;

                            textBox.Select(0, 0);
                            textBox.CaretIndex = textBox.Text.Length;
                        };

                        comboBox.DropDownOpened += (s, e) =>
                        {
                            if (string.IsNullOrEmpty(textBox.Text) && isFiltered)
                            {
                                collectionView.Filter = null;
                                isFiltered = false;
                            }
                        };

                        comboBox.DropDownClosed += (s, e) =>
                        {
                            debounceTimer.Stop();
                        };
                    }
                }
            }));

            template.VisualTree = factory;
            return template;
        }

        public static DataTemplate CreateEditableAudioFilesTextBoxTemplate()
        {
            var template = new DataTemplate();
            var factory = new FrameworkElementFactory(typeof(TextBox));

            factory.SetBinding(TextBox.TextProperty, new Binding($"[AudioFilesDisplay]")
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });

            factory.SetValue(Control.PaddingProperty, new Thickness(5, 2.5, 5, 2.5));

            var tooltipBinding = new Binding("[AudioFiles]")
            {
                Mode = BindingMode.TwoWay,
                Converter = new ConvertTooltipCollectionToString()
            };
            factory.SetBinding(FrameworkElement.ToolTipProperty, tooltipBinding);

            factory.SetValue(FrameworkElement.NameProperty, "AudioFilesDisplay");
            factory.SetValue(TextBoxBase.IsReadOnlyProperty, true);
            template.VisualTree = factory;
            return template;
        }

        public static DataTemplate CreateReadOnlyAudioFilesTextBoxTemplate()
        {
            var template = new DataTemplate();
            var factory = new FrameworkElementFactory(typeof(TextBlock));

            factory.SetBinding(TextBlock.TextProperty, new Binding("[AudioFilesDisplay]"));

            factory.SetValue(TextBlock.PaddingProperty, new Thickness(5, 3.5, 5, 3.5));

            factory.SetBinding(FrameworkElement.ToolTipProperty, new Binding("[AudioFiles]")
            {
                Converter = new ConvertTooltipCollectionToString()
            });

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
                SetIsAddRowButtonEnabled(audioEditorViewModel, audioProjectService, audioRepository);
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
                Command = parameters.AudioEditorViewModel.CopyRowsCommand
            };

            BindingOperations.SetBinding(copyMenuItem, UIElement.IsEnabledProperty,
                new Binding("IsCopyEnabled")
                {
                    Source = parameters.AudioEditorViewModel,
                    Mode = BindingMode.OneWay
                });

            var pasteMenuItem = new MenuItem
            {
                Header = "Paste",
                Command = parameters.AudioEditorViewModel.PasteRowsCommand
            };

            BindingOperations.SetBinding(pasteMenuItem, UIElement.IsEnabledProperty,
                new Binding("IsPasteEnabled")
                {
                    Source = parameters.AudioEditorViewModel,
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

        public static void ClearDataGridCollection(ObservableCollection<Dictionary<string, object>> dataGrid)
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
