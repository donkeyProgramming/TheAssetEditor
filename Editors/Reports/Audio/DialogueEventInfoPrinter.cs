using Editors.Audio.Storage;
using Editors.Audio.Utility;
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
        }

        public static void Generate(IAudioRepository audioRepository)
        {
            var instance = new DialogueEventInfoPrinter(audioRepository);
            instance.Create();
        }

        public void Create()
        {
            var printer = new DialogueEventInfoPrinter(_audioRepository);
            printer.PrintDialogueEventInfo();
        }

        public void PrintDialogueEventInfo()
        {
            var dialogueEvents = _audioRepository.GetHircItemsByType<ICAkDialogueEvent>();
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
