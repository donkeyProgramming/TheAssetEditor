using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Editors.Audio.AudioEditor.ViewModels;
using Editors.Audio.Storage;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using static Shared.Core.PackFiles.IPackFileService;

namespace Editors.Audio.AudioEditor
{
    public class AudioProjectData
    {
        readonly static ILogger _logger = Logging.Create<AudioEditorViewModel>();

        public ProjectSettings Settings { get; set; } = new ProjectSettings();
        public List<DialogueEventItems> DialogueEvents { get; set; } = [];

        public class ProjectSettings
        {
            public string BnkName { get; set; }
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

        public static void AddAudioProjectToPackFile(IPackFileService packFileService, Dictionary<string, List<Dictionary<string, object>>> eventsData, string audioProjectName)
        {
            var audioProjectJson = ConvertEventsDataToAudioProject(eventsData);
            var editablePack = packFileService.GetEditablePack();
            var byteArray = Encoding.ASCII.GetBytes(audioProjectJson);

            var fileEntry = new NewPackFileEntry("AudioProjects", new PackFile($"{audioProjectName}.json", new MemorySource(byteArray)));
            packFileService.AddFilesToPack(editablePack, [fileEntry]);

            _logger.Here().Information($"Saved Audio Project file: {audioProjectName}");
        }

        public static string ConvertEventsDataToAudioProject(Dictionary<string, List<Dictionary<string, object>>> eventsData)
        {
            var audioProject = new Dictionary<string, object>();

            var settings = new Dictionary<string, object>
            {
                ["BnkName"] = "battle_vo_conversational__ovn_vo_actor_Albion_Dural_Durak", // PLACEHOLDER NAME, NEED TO FINISH
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

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            return JsonSerializer.Serialize(audioProject, options);
        }

        public static Dictionary<string, List<Dictionary<string, object>>> ConvertAudioProjectToEventsData(IAudioRepository audioRepository, string audioProjectJson)
        {
            var eventsData = new Dictionary<string, List<Dictionary<string, object>>>();

            using (var audioProject = JsonDocument.Parse(audioProjectJson))
            {
                var root = audioProject.RootElement;

                var settingsElement = root.GetProperty("Settings");
                var settings = new ProjectSettings
                {
                    BnkName = settingsElement.GetProperty("BnkName").GetString(),
                    Language = settingsElement.GetProperty("Language").GetString()
                };

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
                            var stateGroup = AudioEditorViewModelHelpers.AddExtraUnderScoresToString(stateGroups[i]);
                            var state = states[i];
                            eventDataItem[stateGroup] = state;
                        }

                        var fileNames = decisionTree.AudioFiles.Select(filePath => System.IO.Path.GetFileName(filePath));
                        var fileNamesString = string.Join(", ", fileNames);

                        eventDataItem["AudioFilesDisplay"] = fileNamesString;
                        eventDataItem["AudioFiles"] = decisionTree.AudioFiles;
                        eventDataItems.Add(eventDataItem);
                    }

                    eventsData[eventKey] = eventDataItems;
                }
            }

            return eventsData;
        }
    }
}
