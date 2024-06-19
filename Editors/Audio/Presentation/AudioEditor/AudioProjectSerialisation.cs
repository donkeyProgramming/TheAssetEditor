using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Editors.Audio.Presentation.AudioEditor
{
    public class AudioProjectSerialisation
    {
        public static string ConvertToAudioProject(AudioProject audioProject)
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
