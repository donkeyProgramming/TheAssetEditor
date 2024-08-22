using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using static Editors.Audio.AudioEditor.AudioEditorHelpers;
using static Editors.Audio.AudioEditor.AudioProject;

namespace Editors.Audio.AudioEditor
{
    public class VOProjectData
    {
        public class VOProject
        {
            public Settings Settings { get; set; }
            public List<Event> Events { get; set; } = [];
            public List<DialogueEvent> DialogueEvents { get; set; } = [];
        }

        public class Settings
        {
            public string BnkName { get; set; }
            public string Language { get; set; }
            public string StatesProjectFilePath { get; set; }
        }

        public class Event
        {
            public string Name { get; set; }
            public List<string> AudioFiles { get; set; } = [];
            public string AudioFilesDisplay { get; set; }
        }

        public class DialogueEvent
        {
            public string Name { get; set; }
            public List<DecisionNode> DecisionTree { get; set; } = [];
        }

        public class DecisionNode
        {
            public StatePath StatePath { get; set; }
            public List<string> AudioFiles { get; set; } = [];
        }

        public class StatePath
        {
            public List<StatePathNode> Nodes { get; set; } = [];
        }

        public class StatePathNode
        {
            public string StateGroup { get; set; }
            public string State { get; set; }
        }

        public static void ConvertDataGridDataToVOProject(ObservableCollection<Dictionary<string, object>> dataGridData, string audioProjectEvent)
        {
            if (dataGridData.Count() == 0 || audioProjectEvent == null || audioProjectEvent == "")
                return;

            var voProject = AudioProjectInstance.VOProject;
            var dialogueEvent = voProject.DialogueEvents.FirstOrDefault(dialogueEvent => dialogueEvent.Name == audioProjectEvent); // Find the corresponding DialogueEvent in AudioProject
            var decisionTree = dialogueEvent.DecisionTree;
            decisionTree.Clear();

            foreach (var dataGridItem in dataGridData)
            {
                // Validation to ensure that the State Groups are in the correct order.
                var orderedStateGroupsAndStates = ValidateStateGroupsOrder(dataGridItem, audioProjectEvent);

                var decisionNode = new DecisionNode
                {
                    AudioFiles = dataGridItem.ContainsKey("AudioFiles") ? dataGridItem["AudioFiles"] as List<string> : new List<string>(),
                };

                var statePath = new StatePath();

                foreach (var kvp in orderedStateGroupsAndStates)
                {
                    var stateGroup = kvp.Key;
                    var state = kvp.Value;

                    var statePathNode = new StatePathNode
                    {
                        StateGroup = stateGroup,
                        State = state
                    };

                    statePath.Nodes.Add(statePathNode);
                }

                decisionNode.StatePath = statePath;

                dialogueEvent.DecisionTree.Add(decisionNode);
            }
        }

        public static void ConvertVOProjectToDataGridData(ObservableCollection<Dictionary<string, object>> dataGridData, VOProject voProject, string selectedAudioProjectEvent)
        {
            var dialogueEvent = voProject.DialogueEvents.FirstOrDefault(dialogueEvent => dialogueEvent.Name == selectedAudioProjectEvent); // Find the corresponding DialogueEvent in AudioProject

            foreach (var decisionNode in dialogueEvent.DecisionTree)
            {
                var filePaths = decisionNode.AudioFiles;
                var fileNames = filePaths.Select(Path.GetFileName);
                var fileNamesString = string.Join(", ", fileNames);

                var dataGridRow = new Dictionary<string, object>();
                dataGridRow["AudioFiles"] = filePaths;
                dataGridRow["AudioFilesDisplay"] = fileNamesString;

                foreach (var node in decisionNode.StatePath.Nodes)
                {
                    var stateGroup = node.StateGroup;
                    var state = node.State;

                    if (DialogueEventsWithStateGroupsWithQualifiers.TryGetValue(selectedAudioProjectEvent, out var stateGroupsWithQualifiers))
                    {
                        foreach (var kvp in stateGroupsWithQualifiers)
                        {
                            var stateGroupWithQualifierKey = kvp.Key;
                            var stateGroupValue = kvp.Value;

                            if (stateGroup == stateGroupValue)
                            {
                                dataGridRow[AddExtraUnderscoresToString(stateGroupWithQualifierKey)] = state;
                                break;
                            }
                        }
                    }
                }

                dataGridData.Add(dataGridRow);
            }
        }
    }
}
