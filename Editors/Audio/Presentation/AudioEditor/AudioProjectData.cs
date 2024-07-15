using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Editors.Audio.Storage;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;

namespace Editors.Audio.Presentation.AudioEditor
{
    public class AudioProjectData
    {
        public ProjectSettings Settings { get; set; } = new ProjectSettings();
        public List<DialogueEventItems> DialogueEvents { get; set; } = new List<DialogueEventItems>();

        public class ProjectSettings
        {
            public string BnkName { get; set; }
            public string Language { get; set; }
        }

        public class DialogueEventItems
        {
            public string DialogueEvent { get; set; }
            public List<DecisionTreeItems> DecisionTree { get; set; } = new List<DecisionTreeItems>();
        }

        public class DecisionTreeItems
        {
            public string StatePath { get; set; }
            public List<string> AudioFiles { get; set; } = new List<string>();
        }

        public static void AddAudioProjectToPackFile(PackFileService packFileService, Dictionary<string, List<Dictionary<string, object>>> eventsData)
        {
            var audioProjectJson = ConvertEventDataToAudioProject(eventsData);
            Debug.WriteLine($"audioProjectJson: {audioProjectJson}");

            var pack = packFileService.GetEditablePack();
            var byteArray = Encoding.ASCII.GetBytes(audioProjectJson);
            packFileService.AddFileToPack(pack, "AudioProjects", new PackFile($"audio_project.json", new MemorySource(byteArray)));
        }

        public static string ConvertEventDataToAudioProject(Dictionary<string, List<Dictionary<string, object>>> eventsData)
        {
            var audioProject = new Dictionary<string, object>();

            var settings = new Dictionary<string, object>
            {
                ["BnkName"] = "battle_vo_conversational__ovn_vo_actor_Albion_Dural_Durak",
                ["Language"] = "english(uk)"
            };

            audioProject["Settings"] = settings;

            var dialogueEvents = new List<object>();

            foreach (var eventData in eventsData)
            {
                var dialogueEventName = eventData.Key;
                var eventDataItems = eventData.Value;
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

            // Serialize the dictionary to JSON
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            return JsonSerializer.Serialize(audioProject, options);
        }

        public static Dictionary<string, List<Dictionary<string, object>>> ConvertAudioProjectToEventData(IAudioRepository audioRepository, string audioProjectJson)
        {
            var eventData = new Dictionary<string, List<Dictionary<string, object>>>();

            // Deserialize the JSON string into a dynamic object using System.Text.Json
            using (var audioProject = JsonDocument.Parse(audioProjectJson))
            {
                var root = audioProject.RootElement;

                // Deserialize Settings
                var settingsElement = root.GetProperty("Settings");
                var settings = new ProjectSettings
                {
                    BnkName = settingsElement.GetProperty("BnkName").GetString(),
                    Language = settingsElement.GetProperty("Language").GetString()
                };

                // Deserialize DialogueEvents
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
                        var decisionTreeItem = new DecisionTreeItems
                        {
                            StatePath = decisionTreeItemElement.GetProperty("StatePath").GetString(),
                            AudioFiles = decisionTreeItemElement.GetProperty("AudioFiles").EnumerateArray()
                                .Select(file => file.GetString())
                                .ToList()
                        };

                        dialogueEvent.DecisionTree.Add(decisionTreeItem);
                    }

                    dialogueEvents.Add(dialogueEvent);
                }

                // Convert DialogueEvents to eventData dictionary
                foreach (var dialogueEvent in dialogueEvents)
                {
                    var eventKey = dialogueEvent.DialogueEvent;
                    var eventDataItems = new List<Dictionary<string, object>>();

                    foreach (var decisionTree in dialogueEvent.DecisionTree)
                    {
                        var eventDataItem = new Dictionary<string, object>();

                        var states = decisionTree.StatePath.Split('.');
                        var stateGroups = AudioEditorViewModelHelpers.DialogueEventsWithStateGroupsWithQualifiers.GetValueOrDefault(dialogueEvent.DialogueEvent, new List<string>());

                        for (var i = 0; i < states.Length && i < stateGroups.Count; i++)
                        {
                            var stateGroup = AudioEditorViewModelHelpers.AddExtraUnderScoresToStateGroup(stateGroups[i]);
                            var state = states[i];
                            eventDataItem[stateGroup] = state;
                        }

                        var fileNames = decisionTree.AudioFiles.Select(filePath => Path.GetFileName(filePath));
                        var fileNamesString = string.Join(", ", fileNames);

                        eventDataItem["AudioFilesDisplay"] = fileNamesString;
                        eventDataItem["AudioFiles"] = decisionTree.AudioFiles;
                        eventDataItems.Add(eventDataItem);
                    }

                    eventData[eventKey] = eventDataItems;
                }
            }

            return eventData;
        }
    }
}
