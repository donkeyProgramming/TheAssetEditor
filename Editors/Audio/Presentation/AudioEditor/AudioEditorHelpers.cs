using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Editors.Audio.Storage;
using Newtonsoft.Json;


namespace Editors.Audio.Presentation.AudioEditor
{
    public static class AudioEditorHelpers
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
                {
                    return typedChild;
                }
                else
                {
                    var foundChild = FindVisualChild<T>(child, name);
                    if (foundChild != null)
                        return foundChild;
                }
            }
            return null;
        }

        public static void ConfigureDataGrid(IAudioRepository audioRepository, string selectedEventName, ObservableCollection<Dictionary<string, object>> dataGridItems)
        {
            var dataGrid = GetDataGrid();
            dataGrid.Columns.Clear();
            dataGridItems.Clear();

            var stateGroups = audioRepository.DialogueEventsWithStateGroups[selectedEventName];

            foreach (var stateGroup in stateGroups)
            {
                var column = new DataGridTemplateColumn
                {
                    Header = AddExtraUnderScoresToStateGroup(stateGroup),
                    CellTemplate = CreateComboBoxTemplate(audioRepository, stateGroup)
                };

                dataGrid.Columns.Add(column);
            }
        }

        public static DataTemplate CreateComboBoxTemplate(IAudioRepository audioRepository, string stateGroup)
        {
            var template = new DataTemplate();
            var factory = new FrameworkElementFactory(typeof(ComboBox));

            var binding = new Binding($"[{stateGroup}]")
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };

            factory.SetBinding(ComboBox.ItemsSourceProperty, new Binding { Source = audioRepository.StateGroupsWithStates[stateGroup] });
            factory.SetBinding(ComboBox.SelectedItemProperty, binding);

            template.VisualTree = factory;
            return template;
        }

        public static string AddExtraUnderScoresToStateGroup(string stateGroup)
        {
            return stateGroup.Replace("_", "__");
        }

        public static string SerializeDataGrid(ObservableCollection<Dictionary<string, string>> dataGridItems)
        {
            return JsonConvert.SerializeObject(dataGridItems.ToList(), Formatting.Indented);
        }
    }
}

