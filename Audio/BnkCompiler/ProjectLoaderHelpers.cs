using Audio.BnkCompiler.ObjectConfiguration.Warhammer3;
using Audio.Utility;
using Shared.GameFormats.WWise.Hirc.Shared;
using SharpDX.XAudio2;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using static Audio.BnkCompiler.ProjectLoader;

namespace Audio.BnkCompiler
{
    public class ProjectLoaderHelpers
    {
        public static void AddEvents(List<ActorMixer> mixers, CompilerInputProject input, CompilerData compilerData)
        {
            // Add mixers to compilerData first so they can be individually referenced later
            AddEventMixers(mixers, input, compilerData);

            // Add events and their associated actions, containers, and sounds
            foreach (var hircEvent in input.Events)
            {
                var eventId = hircEvent.Event;
                var mixerId = EventToMixers[eventId];
                var soundsCount = hircEvent.Sounds.Count;
                var currentMixer = new ActorMixer();

                foreach (var mixerItem in mixers)
                    if (mixerItem.Name == mixerId)
                    {
                        currentMixer = mixerItem;
                        break;
                    }

                if (soundsCount == 1)
                    AddSingleSoundEvent(compilerData, currentMixer, hircEvent);

                else if (soundsCount > 1)
                    AddMultipleSoundEvent(compilerData, currentMixer, hircEvent);
            }
        }

        public static void AddDialogueEvents(List<ActorMixer> mixers, CompilerInputProject input, CompilerData compilerData)
        {
            // Add mixers to compilerData first so they can be individually referenced later
            AddDialogueEventMixers(mixers, input, compilerData);

            // Add dialogue_events and their associated nodes, containers, and sounds
            foreach (var hircDialogueEvent in input.DialogueEvents)
            {
                var eventId = hircDialogueEvent.DialogueEvent;
                var mixerId = EventToMixers[eventId];
                var currentMixer = new ActorMixer();
                var dialogueEvent = new DialogueEvent();

                // The following properties and values is how the rood node should be set up in WH3 Dialogue Events.
                var rootNode = new AkDecisionTree.Node(new AkDecisionTree.BinaryNode
                {
                    Key = 0, // If value is 0 it will be read and written as 0 as some keys can equal 0. In the case of the WH3 Dialogue Events the root node value always exists and is always 0.
                    AudioNodeId = 0, // Set to 0 if if this property is unused, set to 1 if it's used.
                    Children_uIdx = 1, // Set to 0 if this property is unused, set to 1 if it's used. This value is updated later.
                    Children_uCount = 1, // Set to 0 if this property is unused, set to 1 if it's used. This value is updated later.
                    uWeight = 50, // Always 50 in WH3 Dialogue Events
                    uProbability = 100 // Always 100 in WH3 Dialogue Events
                });

                dialogueEvent.Name = hircDialogueEvent.DialogueEvent;
                dialogueEvent.RootNode = rootNode;

                foreach (var mixerItem in mixers)
                    if (mixerItem.Name == mixerId)
                    {
                        currentMixer = mixerItem;
                        break;
                    }

                foreach (var branch in hircDialogueEvent.DecisionTree)
                {
                    var soundsCount = branch.Sounds.Count;

                    if (soundsCount == 1)
                        AddSingleSoundDialogueEvent(compilerData, rootNode, currentMixer, branch);

                    else if (soundsCount > 1)
                        AddMultipleSoundDialogueEvent(compilerData, rootNode, currentMixer, branch);
                }

                //Console.WriteLine($"======================= PRINTING CUSTOM DECISION TREE GRAPH PRE PROCESSING =======================");
                //PrintNode(rootNode, 0);

                var rootNodeDescendants = ProjectLoaderHelpers.CountNodeDescendants(rootNode);
                dialogueEvent.NodesCount = (uint)(rootNodeDescendants + 1);
                compilerData.DialogueEvents.Add(dialogueEvent);
            }
        }

        public static void AddEventMixers(List<ActorMixer> mixers, CompilerInputProject input, CompilerData compilerData)
        {
            var currentMixer = 0;

            foreach (var hircEvent in input.Events)
            {
                currentMixer++;
                var eventId = hircEvent.Event;
                var eventMixer = hircEvent.Mixer;
                var mixerId = $"{eventId}_mixer_{currentMixer}";
                var mixerParent = VanillaObjectIds.EventMixerIds[eventMixer.ToLower()];

                if (!EventToMixers.ContainsKey(eventId))
                {
                    EventToMixers.Add(eventId, mixerId);

                    var mixer = new ActorMixer()
                    {
                        Name = mixerId,
                        DirectParentId = mixerParent
                    };

                    mixers.Add(mixer);
                    compilerData.ActorMixers.Add(mixer);
                }
            }
        }

        public static void AddDialogueEventMixers(List<ActorMixer> mixers, CompilerInputProject input, CompilerData compilerData)
        {
            foreach (var hircDialogueEvent in input.DialogueEvents)
            {
                var eventId = hircDialogueEvent.DialogueEvent;
                var mixerId = $"{GenerateRandomNumber()}_mixer_{GenerateRandomNumber()}";
                var dialogueEventBnk = DialogueEventData.GetBnkFromDialogueEvent(eventId);
                var mixerParent = VanillaObjectIds.DialogueEventMixerIds[dialogueEventBnk];

                if (!EventToMixers.ContainsKey(eventId))
                {
                    EventToMixers.Add(eventId, mixerId);

                    var mixer = new ActorMixer()
                    {
                        Name = mixerId,
                        DirectParentId = mixerParent,
                        DialogueEvent = eventId
                    };

                    mixers.Add(mixer);
                    compilerData.ActorMixers.Add(mixer);
                }
            }
        }

        public static void AddSingleSoundEvent(CompilerData compilerData, ActorMixer currentMixer, CompilerInputProject.ProjectEvent hircEvent)
        {
            var eventMixer = hircEvent.Mixer;
            var eventId = hircEvent.Event;
            var mixerId = currentMixer.Name;

            foreach (var sound in hircEvent.Sounds)
            {
                var soundId = $"{eventId}_sound";
                var actionId = $"{eventId}_action";

                var defaultSound = new GameSound()
                {
                    Name = soundId,
                    Path = sound,
                    DirectParentId = mixerId,
                    Attenuation = ProjectLoaderHelpers.GetAttenuationId(eventMixer)
                };

                currentMixer.Children.Add(soundId);
                compilerData.GameSounds.Add(defaultSound);

                var defaultAction = new Action()
                {
                    Name = actionId,
                    Type = "Play",
                    ChildId = soundId
                };

                compilerData.Actions.Add(defaultAction);

                var defaultEvent = new Event()
                {
                    Name = eventId,
                    Actions = new List<string>() { actionId }
                };

                compilerData.Events.Add(defaultEvent);
            }
        }

        public static void AddMultipleSoundEvent(CompilerData compilerData, ActorMixer currentMixer, CompilerInputProject.ProjectEvent hircEvent)
        {
            var eventMixer = hircEvent.Mixer;
            var eventId = hircEvent.Event;
            var mixerId = currentMixer.Name;

            var containerId = $"{GenerateRandomNumber()}_random_container_{GenerateRandomNumber()}";

            var soundsCount = hircEvent.Sounds.Count;
            var currentSound = 0;

            var sounds = new List<string>() { };

            foreach (var sound in hircEvent.Sounds)
            {
                currentSound++;
                var soundId = $"{eventId}_sound_{currentSound}";
                var actionId = $"{eventId}_action_{currentSound}";

                var defaultSound = new GameSound()
                {
                    Name = soundId,
                    Path = sound,
                    DirectParentId = containerId,
                    Attenuation = GetAttenuationId(eventMixer)
                };

                compilerData.GameSounds.Add(defaultSound);
                sounds.Add(soundId);

                // once the final sound is reached add everything else
                if (currentSound == soundsCount)
                {
                    var defaultAction = new Action()
                    {
                        Name = actionId,
                        Type = "Play",
                        ChildId = containerId
                    };

                    compilerData.Actions.Add(defaultAction);

                    var defaultRandomContainer = new RandomContainer()
                    {
                        Name = containerId,
                        Children = sounds,
                        DirectParentId = mixerId
                    };

                    compilerData.RandomContainers.Add(defaultRandomContainer);
                    currentMixer.Children.Add(containerId);

                    var defaultEvent = new Event()
                    {
                        Name = eventId,
                        Actions = new List<string>() { actionId }
                    };

                    compilerData.Events.Add(defaultEvent);
                }
            }
        }

        public static void AddSingleSoundDialogueEvent(CompilerData compilerData, AkDecisionTree.Node rootNode, ActorMixer currentMixer, CompilerInputProject.ProjectDecisionTree branch)
        {
            var mixerId = currentMixer.Name;
            var containerId = $"{GenerateRandomNumber()}_random_container_{GenerateRandomNumber()}";
            var hashedContainer = WWiseHash.Compute(containerId);

            var statePath = branch.StatePath;
            var currentSoundIndex = 0;
            var currentStateIndex = 0;

            foreach (var sound in branch.Sounds)
            {
                currentSoundIndex++;
                var soundId = $"{GenerateRandomNumber()}_sound_{GenerateRandomNumber()}";

                var dialogueEvent = currentMixer.DialogueEvent;
                var dialogueEventBnk = DialogueEventData.GetBnkFromDialogueEvent(dialogueEvent);

                var defaultSound = new GameSound()
                {
                    Name = soundId,
                    Path = sound,
                    DirectParentId = mixerId,
                    DialogueEvent = currentMixer.DialogueEvent,
                    Attenuation = GetAttenuationId(dialogueEventBnk)
                };

                compilerData.GameSounds.Add(defaultSound);
                currentMixer.Children.Add(soundId);

                var statePathArray = statePath.Split('.');
                var parentNode = rootNode;

                foreach (var state in statePathArray)
                {
                    // record all states for adding to a dat file
                    if (!compilerData.DatStates.Contains(state))
                        compilerData.DatStates.Add(state);

                    currentStateIndex++;
                    var hashedState = state.Equals("Any", StringComparison.OrdinalIgnoreCase) ? 0 : WWiseHash.Compute(state);

                    var parentExists = false;
                    AkDecisionTree.Node existingParentNode = null;

                    if (currentStateIndex == 1)
                    {
                        // Check if the parent node already exists
                        foreach (var childNode in parentNode.Children)
                        {
                            if (childNode.Key == hashedState)
                            {
                                parentExists = true;
                                existingParentNode = childNode;
                                break;
                            }
                        }
                    }

                    // If the parent node exists, update parentNode
                    if (parentExists)
                        parentNode = existingParentNode;

                    else
                    {
                        // Create a new parent node and add it to the current parentNode's children
                        var newNode = new AkDecisionTree.Node(new AkDecisionTree.BinaryNode
                        {
                            Key = hashedState,
                            AudioNodeId = (currentStateIndex == statePathArray.Length) ? hashedContainer : 0,
                            Children_uIdx = (ushort)((currentStateIndex == statePathArray.Length) ? 0 : 1),
                            Children_uCount = (ushort)((currentStateIndex == statePathArray.Length) ? 0 : 1),
                            uWeight = 50,
                            uProbability = 100
                        });

                        parentNode.Children.Add(newNode);
                        parentNode = newNode;
                    }
                }
            }
        }

        public static void AddMultipleSoundDialogueEvent(CompilerData compilerData, AkDecisionTree.Node rootNode, ActorMixer currentMixer, CompilerInputProject.ProjectDecisionTree branch)
        {
            var mixerId = currentMixer.Name;
            var containerId = $"{GenerateRandomNumber()}_random_container_{GenerateRandomNumber()}";
            var hashedContainer = WWiseHash.Compute(containerId);

            var soundsCount = branch.Sounds.Count;
            var statePath = branch.StatePath;
            var currentSoundIndex = 0;
            var currentStateIndex = 0;

            var sounds = new List<string>() { };

            foreach (var sound in branch.Sounds)
            {
                currentSoundIndex++;
                var soundId = $"{GenerateRandomNumber()}_sound_{GenerateRandomNumber()}";

                var dialogueEvent = currentMixer.DialogueEvent;
                var dialogueEventBnk = DialogueEventData.GetBnkFromDialogueEvent(dialogueEvent);

                var defaultSound = new GameSound()
                {
                    Name = soundId,
                    Path = sound,
                    DirectParentId = containerId,
                    DialogueEvent = currentMixer.DialogueEvent,
                    Attenuation = GetAttenuationId(dialogueEventBnk)
                };

                compilerData.GameSounds.Add(defaultSound);
                sounds.Add(soundId);

                // once the final sound is reached add everything else
                if (currentSoundIndex == soundsCount - 1)
                {
                    var defaultRandomContainer = new RandomContainer()
                    {
                        Name = containerId,
                        Children = sounds,
                        DirectParentId = mixerId
                    };

                    compilerData.RandomContainers.Add(defaultRandomContainer);
                    currentMixer.Children.Add(containerId);

                    var statePathArray = statePath.Split('.');
                    var parentNode = rootNode;

                    foreach (var state in statePathArray)
                    {
                        // record all states for adding to a dat file
                        if (!compilerData.DatStates.Contains(state))
                            compilerData.DatStates.Add(state);

                        currentStateIndex++;
                        var hashedState = state.Equals("Any", StringComparison.OrdinalIgnoreCase) ? 0 : WWiseHash.Compute(state);

                        var parentExists = false;
                        AkDecisionTree.Node existingParentNode = null;

                        if (currentStateIndex == 1)
                        {
                            // Check if the parent node already exists
                            foreach (var childNode in parentNode.Children)
                            {
                                if (childNode.Key == hashedState)
                                {
                                    parentExists = true;
                                    existingParentNode = childNode;
                                    break;
                                }
                            }
                        }

                        // If the parent node exists, update parentNode
                        if (parentExists)
                            parentNode = existingParentNode;

                        else
                        {
                            // Create a new parent node and add it to the current parentNode's children
                            var newNode = new AkDecisionTree.Node(new AkDecisionTree.BinaryNode
                            {
                                Key = hashedState,
                                AudioNodeId = (currentStateIndex == statePathArray.Length) ? hashedContainer : 0,
                                Children_uIdx = (ushort)((currentStateIndex == statePathArray.Length) ? 0 : 1),
                                Children_uCount = (ushort)((currentStateIndex == statePathArray.Length) ? 0 : 1),
                                uWeight = 50,
                                uProbability = 100
                            });

                            parentNode.Children.Add(newNode);
                            parentNode = newNode;
                        }
                    }
                }
            }
        }

        public static uint GetAttenuationId(string eventMixer)
        {
            var attenuationKey = $"{eventMixer}_attenuation";

            if (VanillaObjectIds.AttenuationIds.ContainsKey(attenuationKey))
                return VanillaObjectIds.AttenuationIds[attenuationKey];

            else
                return 0;
        }

        public static int CountNodeDescendants(AkDecisionTree.Node node)
        {
            var count = node.Children.Count;

            foreach (var child in node.Children)
                count += CountNodeDescendants(child);

            return count;
        }

        public static void PrintNode(AkDecisionTree.Node node, int depth)
        {
            if (node == null)
                return;

            var indentation = new string(' ', depth * 4);
            Console.WriteLine($"{indentation}Key: {node.Key}");
            Console.WriteLine($"{indentation}AudioNodeId: {node.AudioNodeId}");
            Console.WriteLine($"{indentation}Children_uIdx: {node.Children_uIdx}");
            Console.WriteLine($"{indentation}Children_uCount: {node.Children_uCount}");
            Console.WriteLine($"{indentation}uWeight: {node.uWeight}");
            Console.WriteLine($"{indentation}uProbability: {node.uProbability}");

            foreach (var childNode in node.Children)
                PrintNode(childNode, depth + 1);
        }

        public static int GenerateRandomNumber()
        {
            var rand = new Random();
            var min = 10000000;
            var max = 99999999;

            return rand.Next(min, max + 1);
        }
    }
}
