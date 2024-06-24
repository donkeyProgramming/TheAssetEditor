using System.Collections.Generic;
using System.Diagnostics;
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
        public Dictionary<string, List<AudioProjectItem>> AudioProjectItems { get; set; } = new Dictionary<string, List<AudioProjectItem>>();

        public class ProjectSettings
        {
            public string BnkName { get; set; }
            public string Language { get; set; }
        }

        public class AudioProjectItem
        {
            public Dictionary<string, string> StatePath { get; set; }
            public List<string> Sounds { get; set; }

            public AudioProjectItem()
            {
                StatePath = new Dictionary<string, string>();
                Sounds = new List<string>();
            }
        }

        public static string ProcessAudioProject(PackFileService packFileService, Dictionary<string, List<Dictionary<string, string>>> eventData)
        {
            var audioProjectData = new AudioProjectData();

            audioProjectData.Settings.BnkName = "battle_vo_conversational__ovn_vo_actor_Albion_Dural_Durak";
            audioProjectData.Settings.Language = "english(uk)";

            foreach (var eventName in eventData.Keys)
            {
                var eventItems = eventData[eventName];

                if (eventItems.Count == 0)
                    audioProjectData.AddAudioEditorItem(eventName, new Dictionary<string, string>());

                else
                {
                    foreach (var eventItem in eventItems)
                        audioProjectData.AddAudioEditorItem(eventName, eventItem);
                }
            }

            var audioProjectJson = ConvertToAudioProject(audioProjectData);
            Debug.WriteLine($"eventData: {audioProjectJson}");

            var pack = packFileService.GetEditablePack();
            var byteArray = Encoding.ASCII.GetBytes(audioProjectJson);
            packFileService.AddFileToPack(pack, "AudioProjects", new PackFile($"audio_project.json", new MemorySource(byteArray)));

            return audioProjectJson;
        }

        public void AddAudioEditorItem(string eventName, Dictionary<string, string> statePath)
        {
            if (!AudioProjectItems.ContainsKey(eventName))
                AudioProjectItems[eventName] = new List<AudioProjectItem>();

            var audioEditorItem = new AudioProjectItem
            {
                StatePath = statePath,
                Sounds = new List<string>()
            };

            AudioProjectItems[eventName].Add(audioEditorItem);
        }

        public static string ConvertToAudioProject(AudioProjectData audioProject)
        {
            var root = new JObject();

            var settings = new JObject
            {
                ["BnkName"] = audioProject.Settings.BnkName,
                ["Language"] = audioProject.Settings.Language
            };

            root["Settings"] = settings;

            var dialogueEvents = new JArray();

            foreach (var kvp in audioProject.AudioProjectItems)
            {
                var eventName = kvp.Key;
                var itemsList = kvp.Value;

                var decisionTree = new JArray();

                foreach (var item in itemsList)
                {
                    var statePathString = string.Join(".", item.StatePath.Values);

                    var decisionTreeItem = new JObject
                    {
                        ["StatePath"] = statePathString,
                        ["Sounds"] = new JArray(item.Sounds)
                    };

                    decisionTree.Add(decisionTreeItem);
                }

                var dialogueEvent = new JObject
                {
                    ["DialogueEvent"] = eventName,
                    ["DecisionTree"] = decisionTree
                };

                dialogueEvents.Add(dialogueEvent);
            }

            root["DialogueEvents"] = dialogueEvents;

            var serializedAudioProject = root.ToString(Formatting.Indented);

            return serializedAudioProject;
        }

        public static Dictionary<string, List<Dictionary<string, string>>> ConvertFromAudioProject(IAudioRepository audioRepository, string audioProjectJson)
        {
            var eventData = new Dictionary<string, List<Dictionary<string, string>>>();
            var root = JObject.Parse(audioProjectJson);
            var dialogueEvents = root["DialogueEvents"] as JArray;

            AudioEditorViewModelHelpers.AddQualifiersToStateGroups(audioRepository.DialogueEventsWithStateGroups);
            var dialogueEventsWithStateGroupsWithQualifiers = AudioEditorViewModelHelpers.DialogueEventsWithStateGroupsWithQualifiers;

            foreach (var item in dialogueEvents)
            {
                var dialogueEvent = item["DialogueEvent"].ToString();
                var decisionTree = item["DecisionTree"] as JArray;

                var eventDataItems = new List<Dictionary<string, string>>();

                foreach (var decisionTreeItem in decisionTree)
                {

                    var statePathString = decisionTreeItem["StatePath"].ToString();
                    var statePath = new Dictionary<string, string>();

                    if (!string.IsNullOrEmpty(statePathString) && dialogueEventsWithStateGroupsWithQualifiers.ContainsKey(dialogueEvent))
                    {
                        var statePathSegments = statePathString.Split('.');
                        var stateGroupKeys = dialogueEventsWithStateGroupsWithQualifiers[dialogueEvent];

                        for (var i = 0; i < statePathSegments.Length && i < stateGroupKeys.Count; i++)
                        {
                            statePath[stateGroupKeys[i]] = statePathSegments[i];
                        }
                    }

                    eventDataItems.Add(statePath);
                }

                eventData[dialogueEvent] = eventDataItems;
            }

            return eventData;
        }
    }
}
