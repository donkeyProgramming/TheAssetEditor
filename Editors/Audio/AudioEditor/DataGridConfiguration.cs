using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Editors.Audio.AudioEditor.ViewModels;
using Editors.Audio.Storage;
using static Editors.Audio.AudioEditor.AudioEditorHelpers;
using static Editors.Audio.AudioEditor.AudioEditorSettings;
using static Editors.Audio.AudioEditor.Converters.TooltipConverter;
using static Editors.Audio.AudioEditor.ViewModels.AudioEditorViewModel;

namespace Editors.Audio.AudioEditor
{
    public class DataGridConfiguration
    {
        public static void ConfigureAudioProjectEditorDataGridForModdedStates(string dataGridName, ObservableCollection<Dictionary<string, object>> audioProjectEditorDataGrid, string stateGroupWithExtraUnderscores)
        {
            var dataGrid = GetDataGrid(dataGridName);
            dataGrid.CanUserAddRows = false;
            dataGrid.ItemsSource = audioProjectEditorDataGrid;
            dataGrid.Columns.Clear();

            // Column for the Modded States.
            var stateGroupColumn = new DataGridTemplateColumn
            {
                Header = stateGroupWithExtraUnderscores,
                CellTemplate = CreateEditableTextBoxTemplate(stateGroupWithExtraUnderscores),
                CellEditingTemplate = CreateEditableTextBoxTemplate(stateGroupWithExtraUnderscores),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            };

            dataGrid.Columns.Add(stateGroupColumn);
        }

        public static void ConfigureAudioProjectEditorDataGridForActionEventSoundBank(AudioEditorViewModel viewModel, IAudioRepository audioRepository, string dataGridName, ObservableCollection<Dictionary<string, object>> audioProjectEditorDataGrid)
        {
            var dataGrid = GetDataGrid(dataGridName);
            dataGrid.CanUserAddRows = false;
            dataGrid.ItemsSource = audioProjectEditorDataGrid;
            dataGrid.Columns.Clear();

            var columnsCount = 3;
            var columnWidth = columnsCount > 0 ? 1.0 / columnsCount : 1.0;

            // Column for the Event.
            var columnHeader = "Event";
            var eventNameColumn = new DataGridTemplateColumn
            {
                Header = columnHeader,
                CellTemplate = CreateEditableTextBoxTemplate(columnHeader),
                CellEditingTemplate = CreateEditableTextBoxTemplate(columnHeader),
                Width = new DataGridLength(columnWidth, DataGridLengthUnitType.Star)
            };

            dataGrid.Columns.Add(eventNameColumn);

            // Column for Audio files TextBox with Tooltip.
            var soundsTextBoxColumn = new DataGridTemplateColumn
            {
                Header = "Audio Files",
                CellTemplate = CreateSoundsTextBoxTemplate(),
                Width = new DataGridLength(columnWidth, DataGridLengthUnitType.Star),
            };

            dataGrid.Columns.Add(soundsTextBoxColumn);

            // Create and set the tooltip binding.
            var soundsButtonColumn = new DataGridTemplateColumn
            {
                CellTemplate = CreateSoundsButtonTemplate(viewModel),
                Width = 30.0,
                CanUserResize = false
            };

            dataGrid.Columns.Add(soundsButtonColumn);
        }

        public static void ConfigureAudioProjectEditorDataGridForDialogueEvent(AudioEditorViewModel viewModel, IAudioRepository audioRepository, DialogueEvent dialogueEvent, bool showModdedStatesOnly, string dataGridName, ObservableCollection<Dictionary<string, object>> audioProjectEditorDataGrid, Dictionary<string, List<string>> stateGroupsWithCustomStates)
        {
            var dataGrid = GetDataGrid(dataGridName);
            dataGrid.CanUserAddRows = false;
            dataGrid.ItemsSource = audioProjectEditorDataGrid;
            dataGrid.Columns.Clear();

            var stateGroups = audioRepository.DialogueEventsWithStateGroups[dialogueEvent.Name];
            var stateGroupsWithQualifiers = DialogueEventsWithStateGroupsWithQualifiersAndStateGroups[dialogueEvent.Name];

            var stateGroupsCount = stateGroups.Count + 1;
            var columnWidth = stateGroupsCount > 0 ? 1.0 / stateGroupsCount : 1.0;

            foreach (var kvp in stateGroupsWithQualifiers)
            {
                var stateGroupWithQualifier = kvp.Key;
                var stateGroupWithQualifierWithExtraUnderscores = AddExtraUnderscoresToString(stateGroupWithQualifier);
                var stateGroup = kvp.Value;

                var states = new List<string>();
                var customStates = new List<string>();

                var vanillaStates = audioRepository.StateGroupsWithStates[stateGroup];

                if (stateGroupsWithCustomStates != null && stateGroupsWithCustomStates.Count > 0)
                {
                    if (stateGroup == "VO_Actor" || stateGroup == "VO_Culture" || stateGroup == "VO_Battle_Selection" || stateGroup == "VO_Battle_Special_Ability" || stateGroup == "VO_Faction_Leader")
                        customStates = stateGroupsWithCustomStates[stateGroup];
                }

                if (showModdedStatesOnly && (stateGroup == "VO_Actor" || stateGroup == "VO_Culture" || stateGroup == "VO_Battle_Selection" || stateGroup == "VO_Battle_Special_Ability" || stateGroup == "VO_Faction_Leader"))
                {
                    states.Add("Any");
                    states.AddRange(customStates);
                }

                else
                {
                    if (stateGroup == "VO_Actor" || stateGroup == "VO_Culture" || stateGroup == "VO_Battle_Selection" || stateGroup == "VO_Battle_Special_Ability" || stateGroup == "VO_Faction_Leader")
                        states.AddRange(customStates);

                    states.AddRange(vanillaStates);
                }

                var column = new DataGridTemplateColumn
                {
                    Header = stateGroupWithQualifierWithExtraUnderscores,
                    CellTemplate = CreateStatesComboBoxTemplate(states, stateGroupWithQualifierWithExtraUnderscores),
                    Width = new DataGridLength(columnWidth, DataGridLengthUnitType.Star),
                };

                dataGrid.Columns.Add(column);
            }           

            var soundsTextBoxColumn = new DataGridTemplateColumn
            {
                Header = "Audio Files",
                CellTemplate = CreateSoundsTextBoxTemplate(),
                Width = new DataGridLength(columnWidth, DataGridLengthUnitType.Star),
            };

            dataGrid.Columns.Add(soundsTextBoxColumn);

            var soundsButtonColumn = new DataGridTemplateColumn
            {
                CellTemplate = CreateSoundsButtonTemplate(viewModel),
                Width = 30.0,
                CanUserResize = false
            };

            dataGrid.Columns.Add(soundsButtonColumn);
        }

        public static void ConfigureAudioProjectViewerDataGridForModdedStates(string dataGridName, ObservableCollection<Dictionary<string, object>> audioProjectViewerDataGrid, string stateGroupWithExtraUnderscores)
        {
            var dataGrid = GetDataGrid(dataGridName);
            dataGrid.CanUserAddRows = false; // Setting this bastard to false ensures that data won't go missing from the last row when a new row is added. Wtf WPF.
            dataGrid.ItemsSource = audioProjectViewerDataGrid;
            dataGrid.Columns.Clear();

            // Column for the ModdedStateGroup.
            var stateGroupColumn = new DataGridTemplateColumn
            {
                Header = stateGroupWithExtraUnderscores,
                Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                IsReadOnly = true
            };

            var textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));

            textBlockFactory.SetBinding(TextBlock.TextProperty, new Binding($"[{stateGroupWithExtraUnderscores}]"));
            textBlockFactory.SetValue(TextBlock.PaddingProperty, new Thickness(5, 2.5, 2.5, 5));

            var cellTemplate = new DataTemplate();

            cellTemplate.VisualTree = textBlockFactory;

            stateGroupColumn.CellTemplate = cellTemplate;

            dataGrid.Columns.Add(stateGroupColumn);
        }

        public static void ConfigureAudioProjectViewerDataGridForActionEventSoundBank(AudioEditorViewModel viewModel, IAudioRepository audioRepository, SoundBank soundBank, string dataGridName, ObservableCollection<Dictionary<string, object>> audioProjectViewerDataGrid)
        {
            var dataGrid = GetDataGrid(dataGridName);
            dataGrid.CanUserAddRows = false; // Setting this bastard to false ensures that data won't go missing from the last row when a new row is added. Wtf WPF.
            dataGrid.ItemsSource = audioProjectViewerDataGrid;
            dataGrid.Columns.Clear();

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

        public static void ConfigureAudioProjectViewerDataGridForDialogueEvent(AudioEditorViewModel viewModel, IAudioRepository audioRepository, DialogueEvent dialogueEvent, string dataGridName, ObservableCollection<Dictionary<string, object>> audioProjectViewerDataGrid)
        {
            var dataGrid = GetDataGrid(dataGridName);
            dataGrid.CanUserAddRows = false; // Setting this bastard to false ensures that data won't go missing from the last row when a new row is added. Wtf WPF.
            dataGrid.ItemsSource = audioProjectViewerDataGrid;
            dataGrid.Columns.Clear();

            var stateGroups = audioRepository.DialogueEventsWithStateGroups[dialogueEvent.Name];
            var stateGroupsWithQualifiers = DialogueEventsWithStateGroupsWithQualifiersAndStateGroups[dialogueEvent.Name];

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

        public static DataTemplate CreateStatesComboBoxTemplate(List<string> states, string stateGroupWithQualifierWithExtraUnderscores)
        {
            var template = new DataTemplate();
            var factory = new FrameworkElementFactory(typeof(ComboBox));

            var observableStates = new ObservableCollection<string>(states);
            var collectionView = CollectionViewSource.GetDefaultView(observableStates);

            var isFiltered = false;
            var selectionMade = false;

            factory.SetValue(ComboBox.ItemsSourceProperty, collectionView);
            factory.SetValue(ComboBox.IsEditableProperty, true);
            factory.SetValue(ComboBox.IsTextSearchEnabledProperty, true);

            var binding = new Binding($"[{stateGroupWithQualifierWithExtraUnderscores}]")
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };

            factory.SetBinding(ComboBox.SelectedItemProperty, binding);

            factory.AddHandler(FrameworkElement.LoadedEvent, new RoutedEventHandler((sender, args) =>
            {
                if (sender is ComboBox comboBox)
                {
                    var textBox = comboBox.Template.FindName("PART_EditableTextBox", comboBox) as TextBox;

                    if (textBox != null)
                    {
                        var debounceTimer = new DispatcherTimer
                        {
                            Interval = TimeSpan.FromMilliseconds(200),
                            IsEnabled = false
                        };

                        var lastFilterText = string.Empty;

                        textBox.TextChanged += (s, e) =>
                        {
                            if (e.Changes.Count > 0 && !IsNavigatingWithArrowKeys(e))
                            {
                                lastFilterText = textBox.Text;
                                if (!selectionMade) // Only debounce if a selection hasn't been made
                                {
                                    debounceTimer.Stop();
                                    debounceTimer.Start();
                                }
                                selectionMade = false; // Reset the selection flag when the text changes
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

                        // Handle the DropDownClosed event to prevent reopening the dropdown unintentionally
                        comboBox.DropDownClosed += (s, e) =>
                        {
                            // Ensure that the selectionMade flag remains true after a selection
                            // and prevent the debounce timer from reopening the dropdown.
                            debounceTimer.Stop();
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

            var binding = new Binding("[AudioFilesDisplay]")
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };

            var tooltipBinding = new Binding("[AudioFiles]")
            {
                Mode = BindingMode.TwoWay,
                Converter = new ConvertTooltipCollectionToString()
            };

            factory.SetValue(FrameworkElement.NameProperty, "AudioFilesDisplay");
            factory.SetBinding(TextBox.TextProperty, binding);
            factory.SetBinding(FrameworkElement.ToolTipProperty, tooltipBinding);
            factory.SetValue(System.Windows.Controls.Primitives.TextBoxBase.IsReadOnlyProperty, true);

            template.VisualTree = factory;

            return template;
        }

        public static DataTemplate CreateSoundsButtonTemplate(AudioEditorViewModel viewModel)
        {
            var template = new DataTemplate();
            var factory = new FrameworkElementFactory(typeof(Button));

            // Handle button click event
            factory.AddHandler(System.Windows.Controls.Primitives.ButtonBase.ClickEvent, new RoutedEventHandler((sender, e) =>
            {
                var button = sender as Button;
                var dataGridRow = FindVisualParent<DataGridRow>(button);

                if (dataGridRow != null)
                {
                    var textBox = FindVisualChild<TextBox>(dataGridRow, "AudioFilesDisplay");

                    if (textBox != null)
                    {
                        var rowDataContext = dataGridRow.DataContext;

                        if (rowDataContext is Dictionary<string, object> dataGridRowContext)
                            AddAudioFiles(dataGridRowContext, textBox);
                    }
                }
            }));

            factory.SetValue(ContentControl.ContentProperty, "...");
            factory.SetValue(FrameworkElement.ToolTipProperty, "Browse wav files");

            template.VisualTree = factory;

            return template;
        }

        public static DataTemplate CreateEditableTextBoxTemplate(string columnHeader)
        {
            var template = new DataTemplate();
            var factory = new FrameworkElementFactory(typeof(TextBox));

            factory.SetBinding(TextBox.TextProperty, new Binding($"[{columnHeader}]")
            {
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Mode = BindingMode.TwoWay
            });

            factory.SetValue(TextBox.PaddingProperty, new Thickness(5, 2.5, 2.5, 5));

            template.VisualTree = factory;

            return template;
        }

        private static bool IsNavigatingWithArrowKeys(TextChangedEventArgs e)
        {
            return Keyboard.IsKeyDown(Key.Up) || Keyboard.IsKeyDown(Key.Down);
        }
    }
}
