using Audio.FileFormats.WWise;
using Audio.FileFormats.WWise.Hirc.V136;
using CommonControls.Common;
using CommonControls.Editors.AudioEditor;
using CommonControls.Services;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AudioResearch
{
    /*public class DialogEventsAsTables
    {
        class OutputTest
        {
            public string EventName;
            public List<string> PrettyTable;
            public string PrettyKeys;
        }

        public void ExportAllDialogEventsAsTable(string outputFilePrefix = "")
        {
            var pfs = ResearchUtil.GetPackFileService();
            ExportAsTable(pfs, outputFilePrefix);
        }


        public void ExportSystemFileAsTable(string systemPath, string outputFilePrefix = "")
        {
            var pfs = PackFileUtil.CreatePackFileServiceFromSystemFile(systemPath);
            ExportAsTable(pfs, outputFilePrefix);
        }

        void ExportAsTable(PackFileService pfs, string outputFilePrefix = "")
        {
            var gamePfs = ResearchUtil.GetPackFileService();

            WwiseDataLoader builder = new WwiseDataLoader();
            var bnkList = builder.LoadBnkFiles(pfs);
            var globalDb = builder.BuildMasterSoundDatabase(bnkList);
            var nameHelper = builder.BuildNameHelper(gamePfs);

            var dialogs = globalDb.HircList.SelectMany(x => x.Value).Where(x => x.Type == HircType.Dialogue_Event).Cast<CAkDialogueEvent_v136>().ToList();

            var whereRootNotZero = dialogs.Where(x => x.AkDecisionTree.Root.Key != 0).ToList();
            var whereFirstNotZero = dialogs.Where(x => x.AkDecisionTree.Root.Children.First().Key != 0).ToList();
            var counts = dialogs.Select(x => x.ArgumentList.Arguments.Count()).Distinct().ToList();

            List<OutputTest> test = new List<OutputTest>();

            foreach (var dialog in dialogs)
            {
                var numArgs = dialog.ArgumentList.Arguments.Count() - 1;
                var root = dialog.AkDecisionTree.Root.Children.First();

                if (numArgs != 0)
                {
                    var table = new List<string>();
                    foreach (var children in root.Children)
                        GenerateRow(children, 0, numArgs, new Stack<string>(), table, nameHelper);

                    var keys = dialog.ArgumentList.Arguments.Select(x => nameHelper.GetName(x.ulGroupId)).ToList();
                    test.Add(new OutputTest()
                    {
                        EventName = nameHelper.GetName(dialog.Id),
                        PrettyKeys = string.Join("|", keys),
                        PrettyTable = table.Select(x => string.Join("|", x)).ToList()
                    });

                    var last = test.Last();

                    var wholeStr = new StringBuilder();
                    wholeStr.AppendLine("sep=|");
                    wholeStr.AppendLine(last.PrettyKeys);
                    foreach (var row in last.PrettyTable)
                        wholeStr.AppendLine(row);
                    DirectoryHelper.EnsureCreated("c:\\temp\\wwiseDialogEvents");
                    File.WriteAllText($"c:\\temp\\wwiseDialogEvents\\{outputFilePrefix}{last.EventName}.csv", wholeStr.ToString());
                }
            }
        }

        static void GenerateRow(AkDecisionTree.Node currentNode, int currentArgrument, int numArguments, Stack<string> pushList, List<string> outputList, WWiseNameLookUpHelper lookUpHelper)
        {
            var currentNodeContent = lookUpHelper.GetName(currentNode.Key);
            pushList.Push(currentNodeContent);

            bool isDone = numArguments == currentArgrument;
            if (isDone)
            {
                outputList.Add(string.Join("|", pushList.ToArray().Reverse()));
            }
            else
            {
                foreach (var child in currentNode.Children)
                    GenerateRow(child, currentArgrument + 1, numArguments, pushList, outputList, lookUpHelper);
            }

            pushList.Pop();
        }
    }*/
}
