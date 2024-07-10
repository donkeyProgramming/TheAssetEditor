using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Editors.Audio.Storage;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
            var audioProject = new JObject();

            var settings = new JObject
            {
                ["BnkName"] = "battle_vo_conversational__ovn_vo_actor_Albion_Dural_Durak",
                ["Language"] = "english(uk)"
            };

            audioProject["Settings"] = settings;

            var dialogueEvents = new JArray();

            foreach (var eventData in eventsData)
            {
                var dialogueEventName = eventData.Key;
                var eventDataItems = eventData.Value;
                var decisionTree = new JArray();

                foreach (var eventDataItem in eventDataItems)
                {
                    if (eventDataItem.ContainsKey("AudioFiles"))
                    {
                        var statePath = eventDataItem
                            .Where(kv => kv.Key != "AudioFiles" && kv.Key != "AudioFilesDisplay")
                            .Select(kv => kv.Value.ToString())
                            .ToList();

                        var decisionBranchItem = new JObject
                        {
                            ["StatePath"] = string.Join(".", statePath),
                            ["AudioFiles"] = new JArray(eventDataItem["AudioFiles"])
                        };

                        decisionTree.Add(decisionBranchItem);
                    }
                }

                var dialogueEvent = new JObject
                {
                    ["DialogueEvent"] = dialogueEventName,
                    ["DecisionTree"] = decisionTree
                };

                dialogueEvents.Add(dialogueEvent);
            }

            audioProject["DialogueEvents"] = dialogueEvents;

            return audioProject.ToString(Formatting.Indented);
        }

        public static Dictionary<string, List<Dictionary<string, object>>> ConvertAudioProjectToEventData(IAudioRepository audioRepository, string audioProjectJson)
        {
            var eventData = new Dictionary<string, List<Dictionary<string, object>>>();

            var audioProject = JObject.Parse(audioProjectJson);
            var settings = audioProject["Settings"].ToObject<ProjectSettings>();
            var dialogueEvents = audioProject["DialogueEvents"].ToObject<List<DialogueEventItems>>();

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

                    var fileNames = decisionTree.AudioFiles.Select(filePath => $"\"{Path.GetFileName(filePath)}\"");
                    var fileNamesString = string.Join(", ", fileNames);
                    
                    eventDataItem["AudioFilesDisplay"] = fileNamesString;
                    eventDataItem["AudioFiles"] = decisionTree.AudioFiles;
                    eventDataItems.Add(eventDataItem);
                }

                eventData[eventKey] = eventDataItems;
            }

            return eventData;
        }
    }
}
