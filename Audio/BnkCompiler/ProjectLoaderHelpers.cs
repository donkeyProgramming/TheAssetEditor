using Audio.BnkCompiler.ObjectConfiguration.Warhammer3;
using Audio.Utility;
using Shared.Core.PackFiles;
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
        public static uint UsableWwiseId { get; set; }

        public static void AddEvents(List<ActorMixer> mixers, CompilerInputProject input, CompilerData compilerData)
        {
            // Add mixers to compilerData first so they can be individually referenced later.
            AddEventMixers(mixers, input, compilerData);

            // Add events and their associated actions, containers, and sounds.
            foreach (var hircEvent in input.Events)
            {
                var eventId = WwiseHash.Compute(hircEvent.Event);
                var mixerId = EventMixers[eventId];
                var soundsCount = hircEvent.Sounds.Count;
                var currentMixer = new ActorMixer();

                foreach (var mixerItem in mixers)
                    if (mixerItem.Id == mixerId)
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
            UsableWwiseId = input.Settings.WwiseStartId;

            // Add mixers to compilerData first so they can be individually referenced later.
            AddDialogueEventMixers(mixers, input, compilerData);

            // Add Dialogue Events and their associated nodes, containers, and sounds.
            foreach (var hircDialogueEvent in input.DialogueEvents)
            {
                var eventId = WwiseHash.Compute(hircDialogueEvent.DialogueEvent);
                var mixerId = EventMixers[eventId];
                var currentMixer = new ActorMixer();
                var dialogueEvent = new DialogueEvent();

                // The following properties and values is how the rood node should be set up in WH3 Dialogue Events.
                var rootNode = new AkDecisionTree.Node(new AkDecisionTree.BinaryNode
                {
                    Key = 0, // If value is 0 it will be read and written as 0 as some keys can equal 0. In the case of the WH3 Dialogue Events the root node value always exists and is always 0.
                    AudioNodeId = 0, // Set to 0 if if this property is unused, set to 1 if it's used.
                    Children_uIdx = 1, // Set to 0 if this property is unused, set to 1 if it's used. This value is updated later.
                    Children_uCount = 1, // Set to 0 if this property is unused, set to 1 if it's used. This value is updated later.
                    uWeight = 50, // Always 50 in WH3 Dialogue Events.
                    uProbability = 100 // Always 100 in WH3 Dialogue Events.
                });

                dialogueEvent.Name = hircDialogueEvent.DialogueEvent;
                dialogueEvent.RootNode = rootNode;

                foreach (var mixerItem in mixers)
                    if (mixerItem.Id == mixerId)
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

                //PrintNode(rootNode, 0);

                var rootNodeDescendants = CountNodeDescendants(rootNode);
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

                var eventId = WwiseHash.Compute(hircEvent.Event);

                var mixerName = $"{eventId}_mixer_{currentMixer}";
                var mixerId = WwiseHash.Compute(mixerName);

                var eventMixer = hircEvent.Mixer;
                var mixerParent = VanillaObjectIds.EventMixerIds[eventMixer.ToLower()];

                if (!EventMixers.ContainsKey(eventId))
                {
                    EventMixers.Add(eventId, mixerId);

                    var mixer = new ActorMixer()
                    {
                        Name = mixerName,
                        Id = mixerId,
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
                var eventName = hircDialogueEvent.DialogueEvent;
                var eventId = WwiseHash.Compute(eventName);
                var mixerId = GetNextUsableWwiseId(UsableWwiseId);

                var dialogueEventBnk = DialogueEventData.GetBnkFromDialogueEvent(eventName);
                var mixerParent = VanillaObjectIds.DialogueEventMixerIds[dialogueEventBnk];

                if (!DialogueEventMixers.ContainsKey(eventId))
                {
                    DialogueEventMixers.Add(eventId, mixerId);

                    var mixer = new ActorMixer()
                    {
                        Id = mixerId,
                        DirectParentId = mixerParent,
                        DialogueEvent = eventName
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
            var mixerId = currentMixer.Id;

            foreach (var sound in hircEvent.Sounds)
            {
                var soundName = $"{eventId}_sound";
                var soundId = WwiseHash.Compute(soundName);

                var actionName = $"{eventId}_action";
                var actionId = WwiseHash.Compute(actionName);

                currentMixer.Children.Add(soundId);

                var defaultSound = new GameSound()
                {
                    Name = soundName,
                    Id = soundId,
                    Path = sound,
                    DirectParentId = mixerId,
                    Attenuation = GetAttenuationId(eventMixer)
                };

                compilerData.GameSounds.Add(defaultSound);

                var defaultAction = new Action()
                {
                    Name = actionName,
                    Id = actionId,
                    Type = "Play",
                    ChildId = soundId
                };

                compilerData.Actions.Add(defaultAction);

                var defaultEvent = new Event()
                {
                    Name = eventId,
                    Actions = new List<uint>() { actionId }
                };

                compilerData.Events.Add(defaultEvent);
            }
        }

        public static void AddMultipleSoundEvent(CompilerData compilerData, ActorMixer currentMixer, CompilerInputProject.ProjectEvent hircEvent)
        {
            var eventMixer = hircEvent.Mixer;
            var eventId = hircEvent.Event;
            var mixerId = currentMixer.Name;
            var containerId = GetNextUsableWwiseId(UsableWwiseId);

            var soundsCount = hircEvent.Sounds.Count;
            var currentSound = 0;
            var sounds = new List<uint>() { };

            foreach (var sound in hircEvent.Sounds)
            {
                currentSound++;

                var soundName = $"{eventId}_sound_{currentSound}";
                var soundId = WwiseHash.Compute(soundName);

                var actionName = $"{eventId}_action_{currentSound}";
                var actionId = WwiseHash.Compute(actionName);

                var defaultSound = new GameSound()
                {
                    Name = soundName,
                    Id = soundId,
                    Path = sound,
                    DirectParentId = containerId,
                    Attenuation = GetAttenuationId(eventMixer)
                };

                // Once the final sound is reached add everything else
                if (currentSound == soundsCount)
                {
                    currentMixer.Children.Add(containerId);

                    var defaultAction = new Action()
                    {
                        Name = actionName,
                        Id = actionId,
                        Type = "Play",
                        ChildId = containerId
                    };

                    var defaultRandomContainer = new RandomContainer()
                    {
                        Id = containerId,
                        Children = sounds,
                        DirectParentId = mixerId
                    };

                    var defaultEvent = new Event()
                    {
                        Name = eventId,
                        Actions = new List<uint>() { actionId }
                    };

                    compilerData.Actions.Add(defaultAction);
                    compilerData.RandomContainers.Add(defaultRandomContainer);
                    compilerData.Events.Add(defaultEvent);
                }

                compilerData.GameSounds.Add(defaultSound);
                sounds.Add(soundId);
            }
        }

        public static void AddSingleSoundDialogueEvent(CompilerData compilerData, AkDecisionTree.Node rootNode, ActorMixer currentMixer, CompilerInputProject.ProjectDecisionTree branch)
        {
            var mixerId = currentMixer.Id;
            var containerId = GetNextUsableWwiseId(UsableWwiseId);
            var statePath = branch.StatePath;

            var currentSoundIndex = 0;
            var currentStateIndex = 0;

            foreach (var sound in branch.Sounds)
            {
                currentSoundIndex++;

                var soundId = GetNextUsableWwiseId(UsableWwiseId);
                var dialogueEvent = currentMixer.DialogueEvent;
                var dialogueEventBnk = DialogueEventData.GetBnkFromDialogueEvent(dialogueEvent);

                var defaultSound = new GameSound()
                {
                    Id = soundId,
                    Path = sound,
                    DirectParentId = mixerId,
                    DialogueEvent = currentMixer.DialogueEvent,
                    Attenuation = GetAttenuationId(dialogueEventBnk)
                };

                currentMixer.Children.Add(soundId);
                compilerData.GameSounds.Add(defaultSound);

                var statePathArray = statePath.Split('.');

                ProcessStatePath(rootNode, statePathArray, compilerData, currentStateIndex, containerId);
            }
        }

        public static void AddMultipleSoundDialogueEvent(CompilerData compilerData, AkDecisionTree.Node rootNode, ActorMixer currentMixer, CompilerInputProject.ProjectDecisionTree branch)
        {
            var mixerId = currentMixer.Name;
            var containerId = GetNextUsableWwiseId(UsableWwiseId);

            var soundsCount = branch.Sounds.Count;
            var statePath = branch.StatePath;
            var currentSoundIndex = 0;
            var currentStateIndex = 0;

            var sounds = new List<uint>() { };

            foreach (var sound in branch.Sounds)
            {
                currentSoundIndex++;
                var soundId = GetNextUsableWwiseId(UsableWwiseId);

                var dialogueEvent = currentMixer.DialogueEvent;
                var dialogueEventBnk = DialogueEventData.GetBnkFromDialogueEvent(dialogueEvent);

                var defaultSound = new GameSound()
                {
                    Id = soundId,
                    Path = sound,
                    DirectParentId = containerId,
                    DialogueEvent = currentMixer.DialogueEvent,
                    Attenuation = GetAttenuationId(dialogueEventBnk)
                };

                compilerData.GameSounds.Add(defaultSound);
                sounds.Add(soundId);

                // Once the final sound is reached add everything else.
                if (currentSoundIndex == soundsCount - 1)
                {
                    var defaultRandomContainer = new RandomContainer()
                    {
                        Id = containerId,
                        Children = sounds,
                        DirectParentId = mixerId
                    };

                    compilerData.RandomContainers.Add(defaultRandomContainer);
                    currentMixer.Children.Add(containerId);

                    var statePathArray = statePath.Split('.');

                    ProcessStatePath(rootNode, statePathArray, compilerData, currentStateIndex, containerId);
                }
            }
        }

        private static void ProcessStatePath(AkDecisionTree.Node parentNode, string[] statePathArray, CompilerData compilerData, int currentStateIndex, uint containerId)
        {
            foreach (var state in statePathArray)
            {
                // Record all states for adding to a dat file.
                if (!compilerData.StatesDat.Contains(state))
                    compilerData.StatesDat.Add(state);

                currentStateIndex++;
                var hashedState = state.Equals("Any", StringComparison.OrdinalIgnoreCase) ? 0 : WwiseHash.Compute(state);

                var parentExists = false;
                AkDecisionTree.Node existingParentNode = null;

                if (currentStateIndex == 1)
                {
                    // Check if the parent node already exists.
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

                // If the parent node exists, update parentNode.
                if (parentExists)
                    parentNode = existingParentNode;

                else
                {
                    // Create a new parent node and add it to the current parentNode's children.
                    var newNode = new AkDecisionTree.Node(new AkDecisionTree.BinaryNode
                    {
                        Key = hashedState,
                        AudioNodeId = (currentStateIndex == statePathArray.Length) ? containerId : 0,
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
            Console.WriteLine($"======================= PRINTING CUSTOM DECISION TREE GRAPH =======================");
            Console.WriteLine($"{indentation}Key: {node.Key}");
            Console.WriteLine($"{indentation}AudioNodeId: {node.AudioNodeId}");
            Console.WriteLine($"{indentation}Children_uIdx: {node.Children_uIdx}");
            Console.WriteLine($"{indentation}Children_uCount: {node.Children_uCount}");
            Console.WriteLine($"{indentation}uWeight: {node.uWeight}");
            Console.WriteLine($"{indentation}uProbability: {node.uProbability}");

            foreach (var childNode in node.Children)
                PrintNode(childNode, depth + 1);
        }

        public static uint GetNextUsableWwiseId(uint wwiseStartId)
        {
            wwiseStartId++;
            // validate the Id to check it's not already in use

            return wwiseStartId;
        }
    }
}
