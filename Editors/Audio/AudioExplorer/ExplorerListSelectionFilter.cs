using System.IO;
using Editors.Audio.Shared.Storage;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc;
using Shared.Ui.Common;

namespace Editors.Audio.AudioExplorer
{
    public class ExplorerListSelectionFilter
    {
        private readonly IAudioRepository _audioRepository;

        public FilterCollection<ExplorerListItem> ExplorerList { get; set; }

        public ExplorerListSelectionFilter(IAudioRepository audioRepository, bool searchByActionEvent, bool searchByDialogueEvent, bool searchByHircId, bool searchByVOActor)
        {
            _audioRepository = audioRepository;

            ExplorerList = new FilterCollection<ExplorerListItem>([])
            {
                // Plain substring match anywhere in the name. Display names contain "(bank.bnk)", which
                // as a regex would stop matching the item the user just picked, and a half-typed "(" is not a valid regex.
                IsPlainTextFilter = true,
                SearchFilter = (value, _) => MatchesFilter(value, ExplorerList.Filter)
            };

            Refresh(searchByActionEvent, searchByDialogueEvent, searchByHircId, searchByVOActor);
        }

        private static bool MatchesFilter(ExplorerListItem item, string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return true;

            filter = filter.Trim();
            return (item.DisplayName?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false)
                || item.Id.ToString().Contains(filter, StringComparison.Ordinal);
        }

        public void Refresh(bool searchByActionEvent, bool searchByDialogueEvent, bool searchByHircId, bool searchByVOActor)
        {
            var selectedList = new List<ExplorerListItem>();

            if (searchByHircId)
            {
                var hircs = _audioRepository.HircsById.SelectMany(x => x.Value).ToList();

                selectedList = hircs
                    .Select(hirc => new ExplorerListItem
                    {
                        HircItem = hirc,
                        DisplayName = hirc.Id.ToString(),
                        Id = hirc.Id,
                        PackFile = hirc.BnkFilePath,
                        IndexInFile = hirc.ByteIndexInFile
                    })
                    .OrderBy(x => x.Id)
                    .ToList();
            }
            else if (searchByActionEvent || searchByDialogueEvent)
            {
                var hircs = new List<HircItem>();
                if (searchByActionEvent)
                    hircs = _audioRepository.GetHircs(AkBkHircType.Event);
                else if (searchByDialogueEvent)
                    hircs = _audioRepository.GetHircs(AkBkHircType.Dialogue_Event);

                selectedList = hircs
                    .Select(hirc => new ExplorerListItem
                    {
                        HircItem = hirc,
                        DisplayName = $"{_audioRepository.GetNameFromId(hirc.Id)} ({Path.GetFileName(hirc.BnkFilePath)})",
                        Id = hirc.Id,
                        PackFile = hirc.BnkFilePath,
                        IndexInFile = hirc.ByteIndexInFile
                    })
                    .OrderBy(hirc => hirc.DisplayName)
                    .ToList();
            }
            else if (searchByVOActor)
            {
                var states = _audioRepository.StatesByStateGroup["VO_Actor"];
                selectedList = states
                    .Select(state => new ExplorerListItem
                    {
                        DisplayName = state,
                    })
                    .OrderBy(x => x.DisplayName)
                    .ToList();
            }
            else
            {
                ExplorerList.UpdatePossibleValues([]);
                return;
            }

            ExplorerList.Filter = "";
            ExplorerList.UpdatePossibleValues(selectedList);
        }
    }
}
