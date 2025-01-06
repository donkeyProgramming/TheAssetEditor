using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Editors.Audio.AudioEditor.AudioProject;
using Editors.Audio.AudioEditor.ViewModels;
using Editors.Audio.Storage;
using static Editors.Audio.AudioEditor.AudioEditorHelpers;
using static Editors.Audio.AudioEditor.AudioProject.AudioProjectManagerHelpers;
using static Editors.Audio.AudioEditor.ButtonEnablement;
using static Editors.Audio.AudioEditor.Converters.TooltipConverter;

namespace Editors.Audio.AudioEditor.DataGrids
{
    public class CellTemplates
    {
        public static DataTemplate CreateStatesComboBoxTemplate(AudioEditorViewModel audioEditorViewModel, List<string> states, string stateGroupWithQualifierWithExtraUnderscores, IAudioProjectService audioProjectService, IAudioRepository audioRepository)
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

            var binding = new Binding($"[{stateGroupWithQualifierWithExtraUnderscores}]")
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            factory.SetBinding(System.Windows.Controls.Primitives.Selector.SelectedItemProperty, binding);

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

        public static DataTemplate CreateSoundsTextBoxTemplate()
        {
            var template = new DataTemplate();
            var factory = new FrameworkElementFactory(typeof(TextBox));

            var binding = new Binding("[AudioFilesDisplay]")
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            factory.SetBinding(TextBox.TextProperty, binding);

            var tooltipBinding = new Binding("[AudioFiles]")
            {
                Mode = BindingMode.TwoWay,
                Converter = new ConvertTooltipCollectionToString()
            };
            factory.SetBinding(FrameworkElement.ToolTipProperty, tooltipBinding);

            factory.SetValue(FrameworkElement.NameProperty, "AudioFilesDisplay");
            factory.SetValue(System.Windows.Controls.Primitives.TextBoxBase.IsReadOnlyProperty, true);
            template.VisualTree = factory;
            return template;
        }

        public static DataTemplate CreateSoundsButtonTemplate(AudioEditorViewModel audioEditorViewModel, IAudioProjectService audioProjectService, IAudioRepository audioRepository)
        {
            var template = new DataTemplate();
            var factory = new FrameworkElementFactory(typeof(Button));

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
                            AddAudioFilesToAudioProjectEditorSingleRowDataGrid(dataGridRowContext, textBox);
                            SetIsAddRowButtonEnabled(audioEditorViewModel, audioProjectService, audioRepository);
                        }
                    }
                }
            }));

            factory.SetValue(ContentControl.ContentProperty, "...");
            factory.SetValue(FrameworkElement.ToolTipProperty, "Browse wav files");
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

            factory.SetValue(Control.PaddingProperty, new Thickness(5, 2.5, 2.5, 5));

            factory.AddHandler(System.Windows.Controls.Primitives.TextBoxBase.TextChangedEvent, new TextChangedEventHandler((s, e) =>
            {
                SetIsAddRowButtonEnabled(audioEditorViewModel, audioProjectService, audioRepository);

            }));

            template.VisualTree = factory;
            return template;
        }
    }
}
