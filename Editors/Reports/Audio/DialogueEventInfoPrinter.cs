using Editors.Audio.GameInformation.Warhammer3;
using Editors.Audio.Storage;
using Shared.Core.Events;
using Shared.Core.Misc;
using Shared.GameFormats.Wwise.Hirc;

namespace Editors.Reports.Audio
{
    public class GenerateDialogueEventInfoPrinterReportCommand(DialogueEventInfoPrinter generator) : IUiCommand
    {
        public void Execute() => generator.Create();
    }

    public class DialogueEventInfoPrinter
    {
        private readonly IAudioRepository _audioRepository;

        public DialogueEventInfoPrinter(IAudioRepository audioRepository)
        {
            _audioRepository = audioRepository;
            var languages = Wh3LanguageInformation.GetAllLanguages();
            _audioRepository.Load(languages);
        }

        public void Create()
        {
            var printer = new DialogueEventInfoPrinter(_audioRepository);
            printer.PrintDialogueEventInfo();
            _audioRepository.Clear();
        }

        public void PrintDialogueEventInfo()
        {
            var dialogueEvents = _audioRepository.GetHircsByType<ICAkDialogueEvent>();
            foreach (var dialogueEvent in dialogueEvents)
                PrintDialogueEventInfo(dialogueEvent);
        }

        private void PrintDialogueEventInfo(ICAkDialogueEvent dialogueEvent)
        {
            var hircItem = dialogueEvent as HircItem;

            var stateGroups = dialogueEvent.Arguments
                .Select(argument => _audioRepository.GetNameFromId(argument.GroupId))
                .ToList();

            var stateGroupsJoined = string.Join(", ", stateGroups);

            var info = $"{_audioRepository.GetNameFromId(hircItem.Id)} : {stateGroupsJoined}";
            Console.WriteLine(info);

            var filePath = $"{DirectoryHelper.ReportsDirectory}\\dialogue_event_info.txt";
            File.AppendAllText(filePath, info + Environment.NewLine);
        }
    }
}
