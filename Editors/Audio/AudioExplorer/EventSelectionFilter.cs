using Editors.Audio.Storage;
using Shared.GameFormats.Wwise.Enums;
using Shared.Ui.Common;
using System.Collections.Generic;
using System.Linq;

namespace Editors.Audio.AudioExplorer
{
    public class EventSelectionFilter
    {
        private readonly IAudioRepository _repository;

        public FilterCollection<SelectedHircItem> EventList { get; set; }

        public EventSelectionFilter(IAudioRepository repository, bool showEvents, bool showDialogueEvents)
        {
            _repository = repository;

            EventList = new FilterCollection<SelectedHircItem>(new List<SelectedHircItem>())
            {
                SearchFilter = (value, rx) => rx.Match(value.DisplayName).Success
            };

            Refresh(showEvents, showDialogueEvents);
        }

        public void Refresh(bool showEvents, bool showDialogueEvents)
        {
            var typesToShow = new List<AkBkHircType>();
            if (showEvents)
                typesToShow.Add(AkBkHircType.Event);
            if (showDialogueEvents)
                typesToShow.Add(AkBkHircType.Dialogue_Event);

            var allEvents = _repository.HircLookupByID.SelectMany(x => x.Value)
                .Where(x => typesToShow.Contains(x.HircType))
                .ToList();

            var selectedableList = allEvents.Select(x => new SelectedHircItem() { HircItem = x, DisplayName = _repository.GetNameFromID(x.ID), ID = x.ID, PackFile = x.OwnerFilePath, IndexInFile = x.ByteIndexInFile }).OrderBy(x => x.DisplayName).ToList();
            EventList.Filter = "";
            EventList.UpdatePossibleValues(selectedableList);
        }
    }
}
