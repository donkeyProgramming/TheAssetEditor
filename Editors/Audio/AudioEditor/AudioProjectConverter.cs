using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Editors.Audio.Storage;
using static Editors.Audio.AudioEditor.AudioEditorHelpers;

namespace Editors.Audio.AudioEditor
{
    public class AudioProjectConverter
    {
        public class ProjectSettings
        {
            public string AudioProjectFileName { get; set; }
            public string CustomStatesFilePath { get; set; }
            public string Language { get; set; }
        }

        public class DialogueEventItems
        {
            public string DialogueEvent { get; set; }
            public List<DecisionTreeItems> DecisionTree { get; set; } = [];
        }

        public class DecisionTreeItems
        {
            public string StatePath { get; set; }
            public List<string> AudioFiles { get; set; } = [];
        }

        public static string ConvertToAudioProjectJson(Dictionary<string, List<Dictionary<string, object>>> audioProjectData)
        {
            var audioProject = new Dictionary<string, object>();

            var settings = audioProjectData["Settings"][0];
            audioProject["Settings"] = settings;

            var dialogueEvents = new List<object>();

            foreach (var audioProjectItem in audioProjectData)
            {
                var dialogueEventName = audioProjectItem.Key;

                // Skip the "Settings" key as it's not a dialogue event
                if (dialogueEventName == "Settings")
                    continue;

                var eventDataItems = audioProjectItem.Value;
                var decisionTree = new List<object>();

                foreach (var eventDataItem in eventDataItems)
                {
                    if (eventDataItem.ContainsKey("AudioFiles"))
                    {
                        var statePath = eventDataItem
                            .Where(kv => kv.Key != "AudioFiles" && kv.Key != "AudioFilesDisplay")
                            .Select(kv => kv.Value.ToString())
                            .ToList();

                        var decisionBranchItem = new Dictionary<string, object>
                        {
                            ["StatePath"] = string.Join(".", statePath),
                            ["AudioFiles"] = eventDataItem["AudioFiles"]
                        };

                        decisionTree.Add(decisionBranchItem);
                    }
                }

                var dialogueEvent = new Dictionary<string, object>
                {
                    ["DialogueEvent"] = dialogueEventName,
                    ["DecisionTree"] = decisionTree
                };

                dialogueEvents.Add(dialogueEvent);
            }

            audioProject["DialogueEvents"] = dialogueEvents;

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            return JsonSerializer.Serialize(audioProject, options);
        }

        public static Dictionary<string, List<Dictionary<string, object>>> ConvertFromAudioProjectJson(IAudioRepository audioRepository, string audioProjectJson)
        {
            var audioProjectData = new Dictionary<string, List<Dictionary<string, object>>>();

            using (var audioProject = JsonDocument.Parse(audioProjectJson))
            {
                var root = audioProject.RootElement;

                var settingsElement = root.GetProperty("Settings");

                var settings = new Dictionary<string, object>
                {
                    { "AudioProjectFileName", settingsElement.GetProperty("AudioProjectFileName").GetString() },
                    { "Language", settingsElement.GetProperty("Language").GetString() },
                    { "CustomStatesFilePath", settingsElement.GetProperty("CustomStatesFilePath").GetString() }
                };

                audioProjectData["Settings"] = new List<Dictionary<string, object>> { settings };

                var dialogueEventsElement = root.GetProperty("DialogueEvents");
                var dialogueEvents = new List<DialogueEventItems>();

                foreach (var dialogueEventElement in dialogueEventsElement.EnumerateArray())
                {
                    var dialogueEvent = new DialogueEventItems
                    {
                        DialogueEvent = dialogueEventElement.GetProperty("DialogueEvent").GetString(),
                        DecisionTree = new List<DecisionTreeItems>()
                    };

                    var decisionTreeElement = dialogueEventElement.GetProperty("DecisionTree");

                    foreach (var decisionTreeItemElement in decisionTreeElement.EnumerateArray())
                    {
                        var audioFilesElement = decisionTreeItemElement.GetProperty("AudioFiles");

                        var decisionTreeItem = new DecisionTreeItems
                        {
                            StatePath = decisionTreeItemElement.GetProperty("StatePath").GetString(),
                            AudioFiles = audioFilesElement.ValueKind == JsonValueKind.Array
                                ? audioFilesElement.EnumerateArray().Select(file => file.GetString()).ToList()
                                : [audioFilesElement.GetString()] // Handle case where no files are provided.
                        };

                        dialogueEvent.DecisionTree.Add(decisionTreeItem);
                    }

                    dialogueEvents.Add(dialogueEvent);
                }

                foreach (var dialogueEvent in dialogueEvents)
                {
                    var eventKey = dialogueEvent.DialogueEvent;
                    var eventDataItems = new List<Dictionary<string, object>>();

                    foreach (var decisionTree in dialogueEvent.DecisionTree)
                    {
                        var eventDataItem = new Dictionary<string, object>();

                        var states = decisionTree.StatePath.Split('.');
                        var stateGroups = DialogueEventsWithStateGroupsWithQualifiers.GetValueOrDefault(dialogueEvent.DialogueEvent, new List<string>());

                        for (var i = 0; i < states.Length && i < stateGroups.Count; i++)
                        {
                            var stateGroup = AddExtraUnderScoresToString(stateGroups[i]);
                            var state = states[i];
                            eventDataItem[stateGroup] = state;
                        }

                        var fileNames = decisionTree.AudioFiles.Select(filePath => Path.GetFileName(filePath));
                        var fileNamesString = string.Join(", ", fileNames);

                        eventDataItem["AudioFilesDisplay"] = fileNamesString;
                        eventDataItem["AudioFiles"] = decisionTree.AudioFiles;
                        eventDataItems.Add(eventDataItem);
                    }

                    audioProjectData[eventKey] = eventDataItems;
                }
            }

            return audioProjectData;
        }
    }
}
