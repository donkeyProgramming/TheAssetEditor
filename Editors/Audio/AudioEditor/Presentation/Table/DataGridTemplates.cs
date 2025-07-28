using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using Shared.Core.Events;
using Editors.Audio.AudioEditor.Events;
using Editors.Audio.AudioEditor.UICommands;

namespace Editors.Audio.AudioEditor.Presentation.Table
{
    public class DataGridTemplates
    {
        public static DataGridTemplateColumn CreateColumnTemplate(string columnName, double columnWidth, bool isReadOnly = false, bool useAbsoluteWidth = false)
        {
            var width = useAbsoluteWidth ? DataGridLengthUnitType.Pixel : DataGridLengthUnitType.Star;
            var column = new DataGridTemplateColumn
            {
                Header = columnName,
                Width = new DataGridLength(columnWidth, width),
                IsReadOnly = isReadOnly
            };
            return column;
        }

        // TODO: Add ctrl + v and ctrl + c shortcuts for the combo boxes.
        public static DataTemplate CreateStatesComboBoxTemplate(IEventHub eventHub, string stateGroupWithQualifierWithExtraUnderscores, List<string> states)
        {
            var template = new DataTemplate();
            var factory = new FrameworkElementFactory(typeof(ComboBox));

            factory.SetValue(ScrollViewer.CanContentScrollProperty, true);
            factory.SetValue(VirtualizingStackPanel.IsVirtualizingProperty, true);
            factory.SetValue(VirtualizingStackPanel.VirtualizationModeProperty, VirtualizationMode.Recycling);

            var comboBoxStyle = Application.Current.TryFindResource(typeof(ComboBox)) as Style;
            factory.SetValue(FrameworkElement.StyleProperty, comboBoxStyle);

            var itemsPanelFactory = new FrameworkElementFactory(typeof(VirtualizingStackPanel));
            factory.SetValue(ItemsControl.ItemsPanelProperty, new ItemsPanelTemplate(itemsPanelFactory));

            var observableValues = new ObservableCollection<string>(states);
            factory.SetValue(ItemsControl.ItemsSourceProperty, observableValues);

            factory.SetValue(ComboBox.IsEditableProperty, true);
            factory.SetValue(ItemsControl.IsTextSearchEnabledProperty, false); // Changed to false to disable the built-in text search so filtering works correctly

            // Setting the SelectedItem and Text bindings allows us to determine control them elsewhere, for example to show a specific value by default
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

            // Add SelectionChanged handler to set a flag that will suppress refiltering when an item is selected
            var suppressTextChanged = false;
            factory.AddHandler(Selector.SelectionChangedEvent, new SelectionChangedEventHandler((sender, e) =>
            {
                suppressTextChanged = true;
            }));

            factory.AddHandler(FrameworkElement.LoadedEvent, new RoutedEventHandler((sender, args) =>
            {
                if (sender is ComboBox comboBox)
                {
                    if (comboBox.Template.FindName("PART_EditableTextBox", comboBox) is TextBox textBox)
                    {
                        // When text changes, filter asynchronously
                        textBox.TextChanged += async (s, e) =>
                        {
                            eventHub.Publish(new EditorDataGridTextboxTextChangedEvent());

                            if (suppressTextChanged)
                            {
                                // Clear selection after an item is selected so text isn't highlighted
                                _ = textBox.Dispatcher.BeginInvoke(() =>
                                {
                                    textBox.SelectionStart = textBox.Text.Length;
                                    textBox.SelectionLength = 0;
                                }, System.Windows.Threading.DispatcherPriority.Background);
                                suppressTextChanged = false;
                                return;
                            }

                            var filterText = textBox.Text;
                            var filteredItems = await Task.Run(() =>
                            {
                                if (string.IsNullOrWhiteSpace(filterText))
                                    return observableValues.ToList();
                                else
                                    return observableValues.Where(item =>
                                        item.Contains(filterText, StringComparison.OrdinalIgnoreCase)).ToList();
                            });

                            comboBox.ItemsSource = filteredItems;

                            // Clear selection after text change to prevent a bug where the first character was being overridden by the second if the first is highlighted
                            _ = textBox.Dispatcher.BeginInvoke(() =>
                            {
                                textBox.SelectionStart = textBox.Text.Length;
                                textBox.SelectionLength = 0;
                            }, System.Windows.Threading.DispatcherPriority.Background);

                            // Open the drop-down to display the results if the text does not exactly match the selected item
                            if (comboBox.SelectedItem == null ||
                                !string.Equals(comboBox.SelectedItem.ToString(), textBox.Text, StringComparison.Ordinal))
                            {
                                if (!comboBox.IsDropDownOpen)
                                    comboBox.IsDropDownOpen = true;
                            }
                        };

                        // When the user leaves the TextBox, validate the text
                        textBox.LostFocus += (s, e) =>
                        {
                            var match = observableValues.FirstOrDefault(item => string.Equals(item, textBox.Text, StringComparison.OrdinalIgnoreCase));
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

        public static DataTemplate CreateEditableTextBoxTemplate(IEventHub eventHub, string columnHeader)
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
                eventHub.Publish(new EditorDataGridTextboxTextChangedEvent());
            }));

            template.VisualTree = factory;
            return template;
        }

        public static DataTemplate CreateEditableEventTextBoxTemplate(IEventHub eventHub, string columnHeader)
        {
            var template = new DataTemplate();
            var factory = new FrameworkElementFactory(typeof(TextBox));

            factory.SetBinding(TextBox.TextProperty, new Binding($"[{columnHeader}]")
            {
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Mode = BindingMode.TwoWay
            });

            factory.SetValue(Control.PaddingProperty, new Thickness(5, 2.5, 5, 2.5));

            factory.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler((sender, e) =>
            {
                if (sender is TextBox textBox)
                {
                    if (!textBox.Text.StartsWith("Play_"))
                    {
                        var caretPos = textBox.SelectionStart;
                        if (textBox.Text.StartsWith("Play"))
                        {
                            textBox.Text = string.Concat("Play_", textBox.Text.AsSpan("Play".Length));
                            textBox.SelectionStart = caretPos + 1;
                        }
                        else
                        {
                            textBox.Text = "Play_" + textBox.Text;
                            textBox.SelectionStart = caretPos + "Play_".Length;
                        }
                    }

                    eventHub.Publish(new EditorDataGridTextboxTextChangedEvent());
                }
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

        public static DataTemplate CreateFileSelectButtonCellTemplate(IUiCommandFactory uiCommandFactory)
        {
            var template = new DataTemplate();
            var factory = new FrameworkElementFactory(typeof(Button));

            factory.SetValue(ContentControl.ContentProperty, ". . .");

            factory.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler((sender, args) =>
            {
                uiCommandFactory.Create<OpenMovieFileSelectionWindowCommand>().Execute();
            }));

            template.VisualTree = factory;
            return template;
        }
    }
}
