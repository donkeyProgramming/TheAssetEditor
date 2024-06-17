using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Editors.Audio.Presentation.AudioExplorer;
using Editors.Audio.Storage;
using Newtonsoft.Json;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.ToolCreation;

namespace Editors.Audio.Presentation.AudioEditor
{
    public class AudioEditorViewModel : NotifyPropertyChangedImpl, IEditorViewModel
    {
        public NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("Audio Editor");

        private readonly PackFileService _packFileService;
        private readonly IAudioRepository _audioRepository;
        private string _selectedEventName;

        public ICommand AddEventCommand { get; set; }
        public ICommand AddStatePathCommand { get; set; }

        public EventSelectionFilter EventFilter { get; set; }

        public ObservableCollection<Dictionary<string, string>> DataGridItems { get; set; } = new ObservableCollection<Dictionary<string, string>>();

        public AudioEditorViewModel(PackFileService packFileService, IAudioRepository audioRepository)
        {
            _packFileService = packFileService;
            _audioRepository = audioRepository;

            AddEventCommand = new RelayCommand(AddEvent);
            AddStatePathCommand = new RelayCommand(AddStatePath);

            EventFilter = new EventSelectionFilter(_audioRepository, false, true);
            EventFilter.EventList.SelectedItemChanged += OnEventSelected;
        }

        public void Close()
        {
            EventFilter.EventList.SelectedItemChanged -= OnEventSelected;
        }

        public bool Save()
        {
            return true;
        }

        public PackFile MainFile { get; set; }
        public bool HasUnsavedChanges { get; set; } = false;

        private void OnEventSelected(SelectedHircItem newValue)
        {
            if (newValue == null)
                return;

            _selectedEventName = newValue.DisplayName.ToString();
            Debug.WriteLine($"selectedEventName: {_selectedEventName}");

            // Clear all previously recorded data. Do I want to do this? Maybe I need some way of storing the data for different objects and reinstating it.
            var dataGrid = AudioEditorHelpers.GetDataGrid();
            dataGrid.Columns.Clear();
            DataGridItems.Clear();
        }

        private void AddEvent()
        {
            if (string.IsNullOrEmpty(_selectedEventName))
                return;

            AudioEditorHelpers.ConfigureDataGrid(_audioRepository, _selectedEventName, DataGridItems);
        }

        private void AddStatePath()
        {
            if (string.IsNullOrEmpty(_selectedEventName))
                return;

            // Add a row
            var newRow = new Dictionary<string, string>();
            DataGridItems.Add(newRow);
        }
    }
}
