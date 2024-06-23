using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
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

        public static void ProcessAudioProject(PackFileService packFileService, Dictionary<string, List<Dictionary<string, string>>> eventData)
        {
            // Create an instance of AudioProject
            var audioProject = new AudioProjectData();

            // Set BnkName and Language settings
            audioProject.Settings.BnkName = "battle_vo_conversational__ovn_vo_actor_Albion_Dural_Durak";
            audioProject.Settings.Language = "english(uk)";

            foreach (var eventName in eventData.Keys)
            {
                var eventItems = eventData[eventName];

                // If eventItems is empty, add an empty AudioProjectItem
                if (eventItems.Count == 0)
                    audioProject.AddAudioEditorItem(eventName, new Dictionary<string, string>());

                else
                {
                    foreach (var eventItem in eventItems)
                        audioProject.AddAudioEditorItem(eventName, eventItem);
                }
            }

            var audioProjectJson = ConvertToAudioProject(audioProject);
            Debug.WriteLine($"eventData: {audioProjectJson}");

            var pack = packFileService.GetEditablePack();
            var byteArray = Encoding.ASCII.GetBytes(audioProjectJson);
            packFileService.AddFileToPack(pack, "AudioProjects", new PackFile($"audio_project.json", new MemorySource(byteArray)));
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
    }
}
