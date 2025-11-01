using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using Editors.Audio.AudioEditor.Commands;
using Editors.Audio.AudioEditor.Events;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.Presentation.Shared.Table
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

            // Set to false to disable the built-in text search so filtering works correctly
            factory.SetValue(ItemsControl.IsTextSearchEnabledProperty, false); 

            // Setting the Text binding allows us to control it elsewhere, for example to show a specific value by default
            factory.SetBinding(ComboBox.TextProperty, new Binding($"[{stateGroupWithQualifierWithExtraUnderscores}]")
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });

            // Do not bind SelectedItem. Text is the single source of truth to avoid feedback loops that erase the first keystroke.
            factory.AddHandler(FrameworkElement.LoadedEvent, new RoutedEventHandler((sender, args) =>
            {
                if (sender is ComboBox comboBox)
                {
                    var view = CollectionViewSource.GetDefaultView(observableValues);
                    if (view != null)
                        view.Filter = _ => true;

                    // Ensure a default value of "Any"
                    if (string.IsNullOrEmpty(comboBox.Text) && states.Contains("Any"))
                        comboBox.Text = "Any";

                    if (comboBox.Template.FindName("PART_EditableTextBox", comboBox) is TextBox textBox)
                    {
                        // Ensure clicking into the box doesn’t select-all and swallow the first key
                        textBox.PreviewMouseLeftButtonDown += (s2, e2) =>
                        {
                            if (!textBox.IsKeyboardFocusWithin)
                            {
                                e2.Handled = true;
                                textBox.Focus();
                            }
                        };

                        // On focus, place caret at end and clear selection to avoid highlight flash
                        textBox.GotKeyboardFocus += (s2, e2) =>
                        {
                            textBox.SelectionStart = textBox.Text.Length;
                            textBox.SelectionLength = 0;
                        };

                        // When text changes, filter and keep caret behavior stable
                        textBox.TextChanged += (s, e) =>
                        {
                            eventHub.Publish(new EditorDataGridTextboxTextChangedEvent(textBox.Text));

                            // If the user edits the text and it no longer matches the selected item, clear selection
                            if (comboBox.SelectedItem != null &&
                                !string.Equals(comboBox.SelectedItem.ToString(), textBox.Text, StringComparison.Ordinal))
                            {
                                comboBox.SelectedItem = null;
                            }

                            var filterText = textBox.Text;

                            if (view != null)
                            {
                                view.Filter = item =>
                                {
                                    if (string.IsNullOrWhiteSpace(filterText))
                                        return true;
                                    var str = (string)item;
                                    return str.Contains(filterText, StringComparison.OrdinalIgnoreCase);
                                };
                                view.Refresh();
                            }

                            // Clear selection after text change to prevent a bug where the first character was being overridden by the second if the first is highlighted
                            _ = textBox.Dispatcher.BeginInvoke(() =>
                            {
                                textBox.SelectionStart = textBox.Text.Length;
                                textBox.SelectionLength = 0;
                            }, System.Windows.Threading.DispatcherPriority.Background);

                            // Open the drop-down to display the results if the text does not exactly match the selected item
                            if (comboBox.SelectedItem == null || !string.Equals(comboBox.SelectedItem?.ToString(), textBox.Text, StringComparison.Ordinal))
                            {
                                if (!comboBox.IsDropDownOpen)
                                    comboBox.IsDropDownOpen = true;
                            }
                        };


                        // When an item is picked from the list, copy it into Text so the TwoWay Text binding updates the VM
                        comboBox.SelectionChanged += (s, e) =>
                        {
                            if (comboBox.SelectedItem is string picked &&
                                !string.Equals(comboBox.Text, picked, StringComparison.Ordinal))
                            {
                                comboBox.Text = picked; // updates VM via the Text two-way binding
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
                                    // keep selection clear while using text as source of truth
                                    comboBox.SelectedItem = null; 
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

            factory.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler((sender, args) =>
            {
                var textBox = (TextBox)sender;
                eventHub.Publish(new EditorDataGridTextboxTextChangedEvent(textBox.Text));
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
                        var caretPosition = textBox.SelectionStart;
                        if (textBox.Text.StartsWith("Play"))
                        {
                            textBox.Text = string.Concat("Play_", textBox.Text.AsSpan("Play".Length));
                            textBox.SelectionStart = caretPosition + 1;
                        }
                        else
                        {
                            textBox.Text = "Play_" + textBox.Text;
                            textBox.SelectionStart = caretPosition + "Play_".Length;
                        }
                    }

                    eventHub.Publish(new EditorDataGridTextboxTextChangedEvent(textBox.Text));
                }
            }));

            //factory.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler((sender, e) =>
            //{
            //    if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.V)
            //    {
            //        if (sender is TextBox textBox)
            //        {
            //            var clipboardText = string.Empty;
            //            if (Clipboard.ContainsText())
            //                clipboardText = Clipboard.GetText(TextDataFormat.UnicodeText);

            //            _ = textBox.Dispatcher.BeginInvoke(new Action(() =>
            //            {
            //                eventHub.Publish(new EditorDataGridTextboxPastedEvent(clipboardText));
            //            }), System.Windows.Threading.DispatcherPriority.Background);
            //        }
            //    }
            //}), handledEventsToo: true);

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
