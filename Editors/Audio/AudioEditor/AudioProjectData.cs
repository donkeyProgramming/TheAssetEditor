using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Editors.Audio.AudioEditor
{
    public class AudioProjectData
    {
        public class AudioProject
        {
            public Settings Settings { get; set; }
            public List<Event> Events { get; set; }
            public List<DialogueEvent> DialogueEvents { get; set; }
        }

        public class Settings
        {
            public string AudioProjectName { get; set; }
            public string BnkName { get; set; }
            public string Language { get; set; }
            public string CustomStatesFilePath { get; set; }
        }

        public class Event
        {
            public string EventName { get; set; } 
            public List<string> AudioFiles { get; set; } = [];
            public string AudioFilesDisplay { get; set; }
        }

        public class DialogueEvent
        {
            public string DialogueEventName { get; set; }
            public List<StatePath> DecisionTree { get; set; } = new List<StatePath>();
        }

        public class StatePath
        {
            public string Path { get; set; }
            public List<string> AudioFiles { get; set; } = [];
        }

        /*
        public static string ConvertToVOAudioProject(AudioProject audioProjectData)
        {
            var audioProject = new Dictionary<string, object>();

            var settings = audioProjectData.Settings;
            audioProject[nameof(Settings)] = new Dictionary<string, object>
            {
                [nameof(Settings.AudioProjectName)] = settings.AudioProjectName,
                [nameof(Settings.Language)] = settings.Language,
                [nameof(Settings.CustomStatesFilePath)] = settings.CustomStatesFilePath
            };

            var dialogueEvents = new List<object>();

            foreach (var dialogueEventItem in audioProjectData.DialogueEvents)
            {
                var decisionTree = new List<object>();

                foreach (var decisionTreeItem in dialogueEventItem.DecisionTree)
                {
                    var decisionBranchItem = new Dictionary<string, object>
                    {
                        [nameof(DecisionBranch.StatePath)] = decisionTreeItem.StatePath,
                        [nameof(DecisionBranch.AudioFiles)] = decisionTreeItem.AudioFiles,
                        [nameof(DecisionBranch.AudioFilesDisplay)] = decisionTreeItem.AudioFilesDisplay
                    };

                    decisionTree.Add(decisionBranchItem);
                }

                var dialogueEvent = new Dictionary<string, object>
                {
                    [nameof(DialogueEvent.DialogueEventName)] = dialogueEventItem.DialogueEventName,
                    [nameof(DialogueEvent.DecisionTree)] = decisionTree
                };

                dialogueEvents.Add(dialogueEvent);
            }

            audioProject[nameof(DialogueEvents)] = dialogueEvents;

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            return JsonSerializer.Serialize(audioProject, options);
        }


        public static AudioProject ConvertFromVOAudioProject(string audioProjectSerialised)
        {
            var audioProject = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(audioProjectSerialised);

            var settingsElement = audioProject[nameof(Settings)];
            var settings = new Settings
            {
                AudioProjectName = settingsElement.GetProperty(nameof(Settings.AudioProjectName)).GetString(),
                Language = settingsElement.GetProperty(nameof(Settings.Language)).GetString(),
                CustomStatesFilePath = settingsElement.GetProperty(nameof(Settings.CustomStatesFilePath)).GetString()
            };

            var dialogueEventsElement = audioProject[nameof(DialogueEvents)].EnumerateArray();
            var dialogueEvents = new List<DialogueEvent>();

            foreach (var dialogueEventElement in dialogueEventsElement)
            {
                var decisionTreeElement = dialogueEventElement.GetProperty(nameof(DialogueEvent.DecisionTree)).EnumerateArray();
                var decisionTree = new List<DecisionBranch>();

                foreach (var decisionTreeItemElement in decisionTreeElement)
                {
                    var decisionTreeItem = new DecisionBranch
                    {
                        StatePath = decisionTreeItemElement.GetProperty(nameof(DecisionBranch.StatePath)).GetString(),
                        AudioFiles = decisionTreeItemElement.GetProperty(nameof(DecisionBranch.AudioFiles)).EnumerateArray().Select(x => x.GetString()).ToList(),
                        AudioFilesDisplay = decisionTreeItemElement.GetProperty(nameof(DecisionBranch.AudioFilesDisplay)).GetString()
                    };

                    decisionTree.Add(decisionTreeItem);
                }

                var dialogueEventItem = new DialogueEvent
                {
                    DialogueEventName = dialogueEventElement.GetProperty(nameof(DialogueEvent.DialogueEventName)).GetString(),
                    DecisionTree = decisionTree
                };

                dialogueEvents.Add(dialogueEventItem);
            }

            return new AudioProject
            {
                Settings = settings,
                DialogueEvents = dialogueEvents
            };
        }
        */
    }
}
