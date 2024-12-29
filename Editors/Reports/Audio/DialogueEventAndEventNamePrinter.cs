using Editors.Audio.Storage;
using Editors.Audio.Utility;
using Octokit;
using Shared.Core.Events;
using Shared.Core.Misc;
using Shared.GameFormats.Wwise.Hirc;

namespace Editors.Reports.Audio
{
    public class GenerateDialogueEventAndEventNamePrinterReportCommand(DialogueEventAndEventNamePrinter generator) : IUiCommand
    {
        public void Execute() => generator.Create();
    }

    public class DialogueEventAndEventNamePrinter
    {
        private readonly IAudioRepository _audioRepository;

        public DialogueEventAndEventNamePrinter(IAudioRepository audioRepository)
        {
            _audioRepository = audioRepository;
        }

        public static void Generate(IAudioRepository audioRepository)
        {
            var instance = new DialogueEventAndEventNamePrinter(audioRepository);
            instance.Create();
        }

        public void Create()
        {
            var printer = new DialogueEventAndEventNamePrinter(_audioRepository);
            printer.PrintInfo();
        }

        public void PrintInfo()
        {
            var allHircItems = _audioRepository.GetAllOfType<HircItem>();
            foreach (var item in allHircItems)
            {
                if (item is ICAkDialogueEvent or ICAkEvent)
                    ProcessItem(item);
            }
        }

        private void ProcessItem(HircItem item)
        {
            var itemName = _audioRepository.GetNameFromHash(item.Id);
            Console.WriteLine(itemName);

            var filePath = $"{DirectoryHelper.ReportsDirectory}\\dialogue_event_and_event_names.txt";
            File.AppendAllText(filePath, itemName + Environment.NewLine);
        }
    }
}
