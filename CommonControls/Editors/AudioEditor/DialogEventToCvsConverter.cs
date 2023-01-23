using CommonControls.Common;
using CommonControls.FileTypes.Sound.WWise.Hirc.V136;
using CommonControls.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace CommonControls.Editors.AudioEditor
{
    public class DialogEventToCvsConverter
    {
        class OutputTest
        {
            public string EventName;
            public List<string> PrettyTable;
            public string PrettyKeys;
        }
        WWiseNameLookUpHelper _nameLookUpHelper;

        public DialogEventToCvsConverter(WWiseNameLookUpHelper nameLookUpHelper)
        {
            _nameLookUpHelper = nameLookUpHelper;
        }

        public string ConvertToCsv(CAkDialogueEvent_v136 dialogEvent, bool includeHeader = true)
        {
            if(dialogEvent == null)
                throw new Exception("Error Converting to CSV - DialogEvent not correct version");

            var numArgs = dialogEvent.ArgumentList.Arguments.Count() - 1;
            var root = dialogEvent.AkDecisionTree.Root.Children.First();
            if (numArgs == 0)
                throw new Exception("Error Converting to CSV - No arguments in DialogEvent");

            var table = new List<string>();
            foreach (var children in root.Children)
                GenerateRow(children, 0, numArgs, new Stack<string>(), table, _nameLookUpHelper);

            var keys = dialogEvent.ArgumentList.Arguments.Select(x => _nameLookUpHelper.GetName(x.ulGroup)).ToList();
            var prettyKeys = string.Join("|", keys);
            prettyKeys += "|WWiseChild";
            var prettyTable = table.Select(x => string.Join("|", x)).ToList();

            var wholeStr = new StringBuilder();
            if(includeHeader)
                wholeStr.AppendLine("sep=|");
            wholeStr.AppendLine(prettyKeys);
            foreach (var row in prettyTable)
                wholeStr.AppendLine(row);

            return wholeStr.ToString();
        }

        public void DumpAndOpen(CAkDialogueEvent_v136 dialogEvent)
        {
            var text = ConvertToCsv(dialogEvent);
            var name = _nameLookUpHelper.GetName(dialogEvent.Id);
            var folderPath = "c:\\temp\\wwiseDialogEvents";
            var filePath = $"{folderPath}\\{name}.csv";
            DirectoryHelper.EnsureCreated(folderPath);
            File.WriteAllText(filePath, text.ToString());
            DirectoryHelper.OpenFolderAndSelectFile(filePath);
        }

        static void GenerateRow(AkDecisionTree.Node currentNode, int currentArgrument, int numArguments, Stack<string> pushList, List<string> outputList, WWiseNameLookUpHelper lookUpHelper)
        {
            var currentNodeContent = lookUpHelper.GetName(currentNode.Key);
            pushList.Push(currentNodeContent);

            bool isDone = numArguments == currentArgrument;
            if (isDone)
            {
                var currentLine = pushList.ToArray().Reverse().ToList();
                currentLine.Add(currentNode.AudioNodeId.ToString());  // Add the wwise child node
                outputList.Add(string.Join("|", currentLine));
            }
            else
            {
                foreach (var child in currentNode.Children)
                    GenerateRow(child, currentArgrument + 1, numArguments, pushList, outputList, lookUpHelper);
            }

            pushList.Pop();
        }
    }
}
