using Audio.FileFormats.WWise;
using Audio.Storage;
using Shared.Ui.Common;
using System.Collections.Generic;
using System.Linq;

namespace Audio.AudioEditor
{
    public class EventSelectionFilter
    {
        private readonly IAudioRepository _repository;

        public FilterCollection<SelectedHircItem> EventList { get; set; }

        public EventSelectionFilter(IAudioRepository repository)
        {
            _repository = repository;

            EventList = new FilterCollection<SelectedHircItem>(new List<SelectedHircItem>())
            {
                SearchFilter = (value, rx) => { return rx.Match(value.DisplayName).Success; }
            };

            Refresh(true, true);
        }

        public void Refresh(bool showEvents, bool showDialogEvents)
        {
            var typesToShow = new List<HircType>();
            if (showEvents)
                typesToShow.Add(HircType.Event);
            if (showDialogEvents)
                typesToShow.Add(HircType.Dialogue_Event);

            var allEvents = _repository.HircObjects.SelectMany(x => x.Value)
                .Where(x => typesToShow.Contains(x.Type))
                .ToList();

            var selectedableList = allEvents.Select(x => new SelectedHircItem() { HircItem = x, DisplayName = _repository.GetNameFromHash(x.Id), Id = x.Id, PackFile = x.OwnerFile, IndexInFile = x.ByteIndexInFile }).ToList();
            EventList.Filter = "";
            EventList.UpdatePossibleValues(selectedableList);
        }
    }
}
