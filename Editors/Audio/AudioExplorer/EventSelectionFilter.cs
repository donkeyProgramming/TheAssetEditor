using System.Collections.Generic;
using System.IO;
using System.Linq;
using Editors.Audio.Storage;
using Shared.GameFormats.Wwise.Enums;
using Shared.Ui.Common;

namespace Editors.Audio.AudioExplorer
{
    public class EventSelectionFilter
    {
        private readonly IAudioRepository _audioRepository;

        public FilterCollection<SelectedHircItem> EventList { get; set; }

        public EventSelectionFilter(IAudioRepository audioRepository, bool showEvents, bool showDialogueEvents)
        {
            _audioRepository = audioRepository;

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

            var allEvents = _audioRepository.HircsById.SelectMany(x => x.Value)
                .Where(x => typesToShow.Contains(x.HircType))
                .ToList();

            var selectedList = allEvents
                .Select(x => new SelectedHircItem() 
                    { 
                        HircItem = x, 
                        DisplayName = $"{_audioRepository.GetNameFromId(x.Id)} ({Path.GetFileName(x.BnkFilePath)})", 
                        Id = x.Id, 
                        PackFile = x.BnkFilePath, 
                        IndexInFile = x.ByteIndexInFile 
                    })
                .OrderBy(x => x.DisplayName)
                .ToList();

            EventList.Filter = "";
            EventList.UpdatePossibleValues(selectedList);
        }
    }
}
