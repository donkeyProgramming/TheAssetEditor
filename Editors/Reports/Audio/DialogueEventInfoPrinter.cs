using Editors.Audio.Storage;
using Editors.Audio.Utility;
using Shared.Core.Events;
using Shared.Core.Misc;
using Shared.GameFormats.Wwise.Hirc;

namespace Editors.Reports.Audio
{
    public class GenerateDialogueEventInfoReportCommand(DialogueEventInfoPrinter generator) : IUiCommand
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
            // Get all the dialogue event info.
            var printer = new DialogueEventInfoPrinter(_audioRepository);
            printer.PrintDialogEventInfos();
        }

        public void PrintDialogEventInfos()
        {
            // Retrieve all HircItem instances from the repository.
            var allHircItems = _audioRepository.GetAllOfType<HircItem>();

            // Filter those that are ICADialogEvent.
            var dialogEvents = allHircItems.OfType<ICADialogEvent>();

            foreach (var dialogEvent in dialogEvents)
                PrintDialogEventInfo(dialogEvent);
        }

        private void PrintDialogEventInfo(ICADialogEvent dialogueEvent)
        {
            // Assuming HircItem is the base type with an Id.
            if (dialogueEvent is not HircItem hircItem)
                throw new InvalidCastException("dialogEvent is not a HircItem.");

            var helper = new DecisionPathHelper(_audioRepository);
            var paths = helper.GetDecisionPaths(dialogueEvent);

            // Splitting the string by '.' and enclosing each part in quotes.
            var splitPaths = paths.Header.GetAsString().Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries)
                              .Select(part => $"\"{part}\"")
                              .ToArray();

            // Joining the quoted strings with a comma and a space, and enclosing the result in brackets.
            var formattedPaths = "[" + string.Join(", ", splitPaths) + "]";

            // Format the information with quotes around the dialog event and the modified path string.
            var info = $"\"{_audioRepository.GetNameFromHash(hircItem.Id)}\" : {formattedPaths}";

            Console.WriteLine(info);
            var filePath = $"{DirectoryHelper.ReportsDirectory}\\dialogue_event_info.txt";
            File.AppendAllText(filePath, info + Environment.NewLine);
        }
    }
}
