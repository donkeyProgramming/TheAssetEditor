﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Editors.Audio.AudioEditor.ViewModels;
using Editors.Audio.Storage;
using Editors.Audio.Utility;
using static Editors.Audio.AudioEditor.AudioEditorHelpers;
using static Editors.Audio.AudioEditor.DataGridHelpers;

namespace Editors.Audio.AudioEditor
{
    public class DialogueEventDataGrid
    {
        public DialogueEventDataGrid()
        {
        }

        public static void ConfigureDataGrid(AudioEditorViewModel viewModel, IAudioRepository audioRepository, bool showCustomStatesOnly)
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

            var stateGroups = audioRepository.DialogueEventsWithStateGroups[selectedAudioProjectEvent];
            var stateGroupsWithQualifiers = AudioProject.DialogueEventsWithStateGroupsWithQualifiers[selectedAudioProjectEvent];
            var stateGroupsWithCustomStates = AudioProject.AudioProjectInstance.StateGroupsWithCustomStates;

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

            foreach (var kvp in stateGroupsWithQualifiers)
            {
                var stateGroupWithQualifier = kvp.Key;
                var stateGroup = kvp.Value;

                var states = new List<string>();
                var customStates = new List<string>();

                var vanillaStates = audioRepository.StateGroupsWithStates[stateGroup];

                if (stateGroupsWithCustomStates.Count() > 0)
                {
                    if (stateGroup == "VO_Actor" || stateGroup == "VO_Culture" || stateGroup == "VO_Battle_Selection" || stateGroup == "VO_Battle_Special_Ability" || stateGroup == "VO_Faction_Leader")
                        customStates = stateGroupsWithCustomStates[stateGroup];
                }

                if (showCustomStatesOnly && (stateGroup == "VO_Actor" || stateGroup == "VO_Culture" || stateGroup == "VO_Battle_Selection" || stateGroup == "VO_Battle_Special_Ability" || stateGroup == "VO_Faction_Leader"))
                {
                    states.Add("Any"); // Still needs an Any State in addition to custom States.
                    states.AddRange(customStates);
                }

                else
                {
                    if (stateGroup == "VO_Actor" || stateGroup == "VO_Culture" || stateGroup == "VO_Battle_Selection" || stateGroup == "VO_Battle_Special_Ability" || stateGroup == "VO_Faction_Leader")
                        states.AddRange(customStates);

                    states.AddRange(vanillaStates);
                }

                // Column for State Group:
                var column = new DataGridTemplateColumn
                {
                    Header = AddExtraUnderscoresToString(stateGroupWithQualifier),
                    CellTemplate = CreateStatesComboBoxTemplate(states, stateGroupWithQualifier, showCustomStatesOnly),
                    Width = new DataGridLength(columnWidth, DataGridLengthUnitType.Star),
                };

                dataGrid.Columns.Add(column);
            }

            // Column for Audio files TextBox:
            var soundsTextBoxColumn = new DataGridTemplateColumn
            {
                Header = "Audio Files",
                CellTemplate = CreateSoundsTextBoxTemplate(),
                Width = new DataGridLength(columnWidth, DataGridLengthUnitType.Star),
            };

            dataGrid.Columns.Add(soundsTextBoxColumn);

            // Column for Audio files '...' button:
            var soundsButtonColumn = new DataGridTemplateColumn
            {
                CellTemplate = CreateSoundsButtonTemplate(audioRepository),
                Width = 30.0,
                CanUserResize = false
            };

            dataGrid.Columns.Add(soundsButtonColumn);

            // Column for Play button:
            var playButtonColumn = new DataGridTemplateColumn
            {
                CellTemplate = CreatePlaySoundsButtonTemplate(viewModel),
                Width = new DataGridLength(columnWidth, DataGridLengthUnitType.Star),
                CanUserResize = false
            };

            dataGrid.Columns.Add(playButtonColumn);
        }

        public static DataTemplate CreateStatesComboBoxTemplate(List<string> states, string stateGroupWithQualifier, bool showCustomStatesOnly)
        {
            var template = new DataTemplate();
            var factory = new FrameworkElementFactory(typeof(ComboBox));

            var binding = new Binding($"[{AddExtraUnderscoresToString(stateGroupWithQualifier)}]")
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };

            // ComboBox settings.
            factory.SetBinding(System.Windows.Controls.Primitives.Selector.SelectedItemProperty, binding);
            factory.SetValue(ItemsControl.IsTextSearchEnabledProperty, true);
            factory.SetValue(ComboBox.IsEditableProperty, true);
            factory.SetValue(ItemsControl.ItemsSourceProperty, states);

            // Loaded event for initializing items and setting up TextChanged event.
            factory.AddHandler(FrameworkElement.LoadedEvent, new RoutedEventHandler((sender, args) =>
            {
                if (sender is ComboBox comboBox)
                {
                    comboBox.ItemsSource = states;

                    if (comboBox.Template.FindName("PART_EditableTextBox", comboBox) is TextBox textBox)
                    {
                        textBox.TextChanged += (s, e) =>
                        {
                            var filterText = textBox.Text;
                            var filteredItems = states.Where(item => item.Contains(filterText, StringComparison.OrdinalIgnoreCase)).ToList();

                            comboBox.ItemsSource = filteredItems;
                            comboBox.IsDropDownOpen = true; // Keep the drop-down open to show filtered results.
                        };

                        // Handle LostFocus event to ensure final text is genuinely a State and warm the user if not.
                        textBox.LostFocus += (s, e) =>
                        {
                            var finalText = textBox.Text;

                            if (!string.IsNullOrWhiteSpace(finalText) && !states.Contains(finalText))
                            {
                                MessageBox.Show("Invalid State. Select a State from the list.", "Invalid State", MessageBoxButton.OK, MessageBoxImage.Warning);
                                textBox.Text = string.Empty;
                                comboBox.SelectedItem = null;
                            }
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
            factory.SetBinding(FrameworkElement.ToolTipProperty, tooltipBinding);

            factory.SetValue(System.Windows.Controls.Primitives.TextBoxBase.IsReadOnlyProperty, true);

            template.VisualTree = factory;

            return template;
        }

        public static DataTemplate CreateSoundsButtonTemplate(IAudioRepository audioRepository)
        {
            var template = new DataTemplate();
            var factory = new FrameworkElementFactory(typeof(Button));
            factory.SetValue(ContentControl.ContentProperty, "...");
            factory.SetValue(FrameworkElement.ToolTipProperty, "Browse wav files");

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
                        {
                            // Pass the entire ItemsSource (dataGridData) and the specific row context to AddAudioFiles
                            AudioEditorViewModel.AddAudioFiles(dataGridRowContext, textBox);
                        }
                    }
                }
            }));

            template.VisualTree = factory;

            return template;
        }

        public static DataTemplate CreatePlaySoundsButtonTemplate(AudioEditorViewModel viewModel)
        {
            var template = new DataTemplate();
            var factory = new FrameworkElementFactory(typeof(Button));
            factory.SetValue(ContentControl.ContentProperty, "Play Audio");
            factory.SetValue(FrameworkElement.ToolTipProperty, "Play an audio file at random to simulate the Dialogue Event being triggered in game.");

            // Handle button click event
            factory.AddHandler(System.Windows.Controls.Primitives.ButtonBase.ClickEvent, new RoutedEventHandler((sender, e) =>
            {
                var button = sender as Button;
                var dataGridRow = FindVisualParent<DataGridRow>(button);

                if (dataGridRow != null)
                {
                    var rowDataContext = dataGridRow.DataContext;

                    if (rowDataContext is Dictionary<string, object> dataGridRowContext)
                        SoundPlayer.PlaySound(dataGridRowContext);
                }
            }));

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