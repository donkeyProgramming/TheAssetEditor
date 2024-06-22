using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Editors.Audio.Presentation.AudioEditor.ViewModels;
using Editors.Audio.Storage;
using Shared.Core.PackFiles;


namespace Editors.Audio.Presentation.AudioEditor
{
    public class AudioEditorViewModelHelpers
    {
        private readonly PackFileService _packFileService;
        private readonly IAudioRepository _audioRepository;

        public static Dictionary<string, List<string>> DialogueEventsWithStateGroupsWithQualifiers { get; set; } = new Dictionary<string, List<string>>();

        public AudioEditorViewModelHelpers(PackFileService packFileService, IAudioRepository audioRepository)
        {
            _packFileService = packFileService;
            _audioRepository = audioRepository;
        }

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

        public static void ConfigureDataGrid(IAudioRepository audioRepository, string selectedEventName, ObservableCollection<Dictionary<string, string>> dataGridItems)
        {
            var dataGrid = GetDataGrid();

            // DataGrid settings:
            dataGrid.CanUserAddRows = false; // Setting this fucker to false ensures that data won't go missing from the last row when a new row is added. Wtf WPF.
            dataGrid.ItemsSource = dataGridItems;

            // Clear existing items.
            dataGrid.Columns.Clear();
            dataGridItems.Clear();


            var stateGroups = audioRepository.DialogueEventsWithStateGroups[selectedEventName];
            var stateGroupsWithQualifiers = DialogueEventsWithStateGroupsWithQualifiers[selectedEventName];

            foreach (var (stateGroup, stateGroupWithQualifier) in stateGroups.Zip(stateGroupsWithQualifiers))
            {
                var column = new DataGridTemplateColumn
                {
                    Header = AddExtraUnderScoresToStateGroup(stateGroupWithQualifier),
                    CellTemplate = CreateComboBoxTemplate(audioRepository, stateGroup, stateGroupWithQualifier) // Makes all cells use a ComboBox of the available States.
                };

                dataGrid.Columns.Add(column);
            }
        }

        public static DataTemplate CreateComboBoxTemplate(IAudioRepository audioRepository, string stateGroup, string stateGroupWithQualifier)
        {
            var template = new DataTemplate();
            var factory = new FrameworkElementFactory(typeof(ComboBox));
            var states = audioRepository.StateGroupsWithStates[stateGroup];

            var binding = new Binding($"[{stateGroupWithQualifier}]")
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

        public static void AddQualifiersToStateGroups(Dictionary<string, List<string>> dialogueEventsWithStateGroups)
        {
            DialogueEventsWithStateGroupsWithQualifiers = new Dictionary<string, List<string>>();

            foreach (var dialogueEvent in dialogueEventsWithStateGroups)
            {
                DialogueEventsWithStateGroupsWithQualifiers[dialogueEvent.Key] = new List<string>();
                var stateGroups = DialogueEventsWithStateGroupsWithQualifiers[dialogueEvent.Key];

                var voActorCount = 0;
                var voCultureCount = 0;

                foreach (var stateGroup in dialogueEvent.Value)
                {
                    if (stateGroup == "VO_Actor")
                    {
                        voActorCount++;

                        if (voActorCount > 1)
                            stateGroups.Add($"VO_Actor (Reference)");

                        else
                            stateGroups.Add("VO_Actor (Source)");
                    }

                    else if (stateGroup == "VO_Culture")
                    {
                        voCultureCount++;

                        if (voCultureCount > 1)
                            stateGroups.Add($"VO_Culture (Reference)");

                        else
                            stateGroups.Add("VO_Culture (Source)");
                    }

                    else
                        stateGroups.Add(stateGroup);
                }
            }
        }

        public static string AddExtraUnderScoresToStateGroup(string stateGroupWithQualifier)
        {
            return stateGroupWithQualifier.Replace("_", "__"); // Apparently WPF doesn't_like_underscores
        }

        public static void UpdateAudioProjectEventSubType(AudioEditorViewModel viewModel)
        {
            var audioProjectSettings = new AudioProjectSettings();
            viewModel.AudioProjectSubTypes.Clear();

            if (viewModel.SelectedAudioProjectEventType == "Non-VO")
                foreach (var item in audioProjectSettings.NonVO)
                    viewModel.AudioProjectSubTypes.Add(item);

            else if (viewModel.SelectedAudioProjectEventType == "Frontend VO")
                foreach (var item in audioProjectSettings.FrontendVO)
                    viewModel.AudioProjectSubTypes.Add(item);

            else if (viewModel.SelectedAudioProjectEventType == "Campaign VO")
                foreach (var item in audioProjectSettings.CampaignVO)
                    viewModel.AudioProjectSubTypes.Add(item);

            else if (viewModel.SelectedAudioProjectEventType == "Battle VO")
                foreach (var item in audioProjectSettings.BattleVO)
                    viewModel.AudioProjectSubTypes.Add(item);

            Debug.WriteLine($"AudioProjectSubTypes changed to: {string.Join(", ", viewModel.AudioProjectSubTypes)}");
        }

        // STILL NEED TO FINISH THIS
        public static void UpdateAudioProjectDialogueEvents(AudioEditorViewModel viewModel)
        {
            viewModel.AudioProjectDialogueEvents.Clear();

            if (viewModel.SelectedAudioProjectEventType == "Frontend VO" 
                && viewModel.SelectedAudioProjectEventSubtype == "Lord" 
                && (viewModel.SelectedDialogueEventsPreset == DialogueEventsPreset.All || viewModel.SelectedDialogueEventsPreset == DialogueEventsPreset.Essential))
            {
                AddDialogueEventToDisplayData(viewModel, AudioProjectSettings.FrontendVODialogueEventsAll);
            }


            if (viewModel.SelectedAudioProjectEventType == "Campaign VO" && viewModel.SelectedAudioProjectEventSubtype == "Lord")
            {
                if (viewModel.SelectedDialogueEventsPreset == DialogueEventsPreset.All)
                    AddDialogueEventToDisplayData(viewModel, AudioProjectSettings.CampaignVODialogueEventsAll);

                else
                {

                }
            }

            if (viewModel.SelectedAudioProjectEventType == "Campaign VO" && viewModel.SelectedAudioProjectEventSubtype == "Hero")
            {
                if (viewModel.SelectedDialogueEventsPreset == DialogueEventsPreset.All)
                    AddDialogueEventToDisplayData(viewModel, AudioProjectSettings.CampaignVODialogueEventsAll);

                else
                {

                }
            }
        }

        public static void AddDialogueEventToDisplayData(AudioEditorViewModel viewModel, List<string>  displayData)
        {
            foreach (var dialogueEvent in displayData)
                viewModel.AudioProjectDialogueEvents.Add(dialogueEvent);

            viewModel.AudioProjectDialogueEventsText.Value = string.Join(Environment.NewLine, viewModel.AudioProjectDialogueEvents);
            Debug.WriteLine($"AudioProjectDialogueEvents changed to: {viewModel.AudioProjectDialogueEventsText.Value}");
        }
    }
}
