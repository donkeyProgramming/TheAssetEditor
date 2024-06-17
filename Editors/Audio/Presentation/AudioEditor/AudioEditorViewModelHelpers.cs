using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Editors.Audio.Storage;
using Newtonsoft.Json;


namespace Editors.Audio.Presentation.AudioEditor
{
    public class AudioEditorViewModelHelpers
    {
        public static DataGrid GetDataGrid()
        {
            var mainWindow = Application.Current.MainWindow;
            var dataGrid = FindVisualChild<DataGrid>(mainWindow, "AudioEditorDataGrid");
            return dataGrid;
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
            return stateGroup.Replace("_", "__"); // Apparently WPF doesn't_like_underscores
        }

        public static string SerializeDataGrid(ObservableCollection<Dictionary<string, string>> dataGridItems)
        {
            var rows = new List<Dictionary<string, string>>();

            foreach (var item in dataGridItems)
                rows.Add(item);

            return JsonConvert.SerializeObject(rows, Formatting.Indented);
        }
    }
}
