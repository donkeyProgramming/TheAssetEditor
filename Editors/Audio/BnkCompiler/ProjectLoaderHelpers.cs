using Editors.Audio.BnkCompiler.ObjectConfiguration.Warhammer3;
using Editors.Audio.Storage;
using Editors.Audio.Utility;
using Microsoft.Xna.Framework.Media;
using Shared.Core.PackFiles;
using Shared.GameFormats.WWise.Hirc.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms.Design;
using System.Windows.Input;
using static Editors.Audio.BnkCompiler.ProjectLoader;

namespace Editors.Audio.BnkCompiler
{
    public class ProjectLoaderHelpers
    {
        public static uint UsableWwiseId { get; set; }

        public static void AddEvents(List<ActorMixer> mixers, CompilerInputProject input, CompilerData compilerData)
        {
            // Add mixers to compilerData first so they can be individually referenced later.
            AddEventMixers(mixers, input, compilerData);

            // Add events and their associated actions, containers, and sounds.
            foreach (var projectEvent in input.Events)
            {
                var eventName = projectEvent.Event;
                var eventId = WwiseHash.Compute(eventName);
                var mixerId = EventMixers[eventId];
                var soundsCount = projectEvent.Sounds.Count;
                var currentMixer = new ActorMixer();

                // Record all Events for adding to a dat file.
                RecordDatData(compilerData.EventsDat, eventName);

                foreach (var mixerItem in mixers)
                    if (mixerItem.Id == mixerId)
                    {
                        currentMixer = mixerItem;
                        break;
                    }

                if (soundsCount == 1)
                    AddSingleSoundEvent(compilerData, currentMixer, projectEvent);

                else if (soundsCount > 1)
                    AddMultipleSoundEvent(compilerData, currentMixer, projectEvent);
            }
        }

        public static void AddDialogueEvents(IAudioRepository audioRepository, List<ActorMixer> mixers, CompilerInputProject input, CompilerData compilerData)
        {
            var wwiseIdStart = input.Settings.WwiseStartId;

            if (wwiseIdStart == 0)
                UsableWwiseId = 1;
            else
                UsableWwiseId = wwiseIdStart;

            // Add mixers to compilerData first so they can be individually referenced later.
            AddDialogueEventMixers(audioRepository, mixers, input, compilerData);

            // Add Dialogue Events and their associated nodes, containers, and sounds.
            foreach (var hircDialogueEvent in input.DialogueEvents)
            {
                var eventId = WwiseHash.Compute(hircDialogueEvent.DialogueEvent);
                var mixerId = EventMixers[eventId];
                var currentMixer = new ActorMixer();
                var dialogueEvent = new DialogueEvent();

                uint key = 0; // Any value will initialise this property. In the case of the WH3 Dialogue Events the root node value always exists and is always 0.
                uint audioNodeId = 0; // If 0 this property is not initialised. The root node in WH3 Dialogue Events is always unused so the value is set to 0.
                ushort children_uIdx = 1; // If 0 this property is not initialised. This value is set to 1 to initialise the value. Its intended value is updated later.
                ushort children_uCount = 1; // If 0 this property is not initialised. This value is set to 1 to initialise the value. Its intended value is updated later.
                var rootNode = CreateNode(key, audioNodeId, children_uIdx, children_uCount, CompilerConstants.UWeight, CompilerConstants.UProbability);

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
                        AddSingleSoundDialogueEvent(audioRepository, compilerData, rootNode, currentMixer, branch);

                    else if (soundsCount > 1)
                        AddMultipleSoundDialogueEvent(audioRepository, compilerData, rootNode, currentMixer, branch);
                }

                //PrintNode(rootNode, 0); // print for testing
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
                var eventName = hircEvent.Event;
                var eventId = WwiseHash.Compute(eventName);
                var mixerName = $"{eventId}_mixer_{currentMixer}";
                var mixerId = WwiseHash.Compute(mixerName);
                var eventMixer = hircEvent.Mixer;
                var mixerParent = VanillaObjectIds.EventMixerIds[eventMixer.ToLower()];

                if (!EventMixers.ContainsKey(eventId))
                {
                    var mixer = CreateMixer(mixerId, mixerParent);
                    mixers.Add(mixer);
                    compilerData.ActorMixers.Add(mixer);
                    EventMixers.Add(eventId, mixerId);
                }
            }
        }

        public static void AddDialogueEventMixers(IAudioRepository audioRepository, List<ActorMixer> mixers, CompilerInputProject input, CompilerData compilerData)
        {
            foreach (var hircDialogueEvent in input.DialogueEvents)
            {
                var dialogueEventName = hircDialogueEvent.DialogueEvent;
                var dialogueEventId = WwiseHash.Compute(dialogueEventName);
                var mixerId = GetNextUsableWwiseId(UsableWwiseId);
                var dialogueEventBnk = audioRepository.GetOwnerFileFromDialogueEvent(dialogueEventId, true);
                var mixerParent = VanillaObjectIds.DialogueEventMixerIds[dialogueEventBnk];

                if (!DialogueEventMixers.ContainsKey(dialogueEventId))
                {
                    var mixer = CreateMixer(mixerId, mixerParent, dialogueEventName);
                    mixers.Add(mixer);
                    compilerData.ActorMixers.Add(mixer);
                    DialogueEventMixers.Add(dialogueEventId, mixerId);
                }
            }
        }

        public static void AddSingleSoundEvent(CompilerData compilerData, ActorMixer currentMixer, CompilerInputProject.ProjectEvent projectEvent)
        {
            var eventMixer = projectEvent.Mixer;
            var eventName = projectEvent.Event;
            var eventId = WwiseHash.Compute(eventName);
            var mixerId = currentMixer.Id;
            var soundName = $"{eventName}_sound";
            var soundId = WwiseHash.Compute(soundName);
            var actionName = $"{eventName}_action";
            var actionId = WwiseHash.Compute(actionName);

            var soundFilePath = projectEvent.Sounds[0];
            var hircSound = CreateSound(soundId, mixerId, eventMixer, soundFilePath);
            var hircAction = CreateAction(actionId, soundId, CompilerConstants.ActionType);
            var hircEvent = CreateEvent(eventId, actionId);

            currentMixer.Children.Add(soundId);
            compilerData.Sounds.Add(hircSound);
            compilerData.Actions.Add(hircAction);
            compilerData.Events.Add(hircEvent);
        }

        public static void AddMultipleSoundEvent(CompilerData compilerData, ActorMixer currentMixer, CompilerInputProject.ProjectEvent projectEvent)
        {
            var eventMixer = projectEvent.Mixer;
            var eventName = projectEvent.Event;
            var eventId = WwiseHash.Compute(eventName);
            var mixerId = currentMixer.Id;
            var containerName = $"{eventName}_random_container";
            var containerId = WwiseHash.Compute(containerName);

            var soundsCount = projectEvent.Sounds.Count;
            var currentSound = 0;
            var sounds = new List<uint>() { };

            foreach (var sound in projectEvent.Sounds)
            {
                currentSound++;

                var soundName = $"{eventName}_sound_{currentSound}";
                var soundId = WwiseHash.Compute(soundName);
                var actionName = $"{eventName}_action_{currentSound}";
                var actionId = WwiseHash.Compute(actionName);

                var hircSound = CreateSound(soundId, containerId, eventMixer, sound);

                // Once the final sound is reached add everything else
                if (currentSound == soundsCount)
                {
                    var hircAction = CreateAction(actionId, containerId, CompilerConstants.ActionType);
                    var hircRandomContainer = CreateRandomContainer(containerId, mixerId, sounds);
                    var hircEvent = CreateEvent(eventId, actionId);

                    currentMixer.Children.Add(containerId);
                    compilerData.Actions.Add(hircAction);
                    compilerData.RandomContainers.Add(hircRandomContainer);
                    compilerData.Events.Add(hircEvent);
                }

                compilerData.Sounds.Add(hircSound);
                sounds.Add(soundId);
            }
        }

        public static void AddSingleSoundDialogueEvent(IAudioRepository audioRepository, CompilerData compilerData, AkDecisionTree.Node rootNode, ActorMixer currentMixer, CompilerInputProject.ProjectDecisionTree branch)
        {
            var mixerId = currentMixer.Id;
            var containerId = GetNextUsableWwiseId(UsableWwiseId);
            var soundId = GetNextUsableWwiseId(UsableWwiseId);
            var dialogueEventName = currentMixer.DialogueEvent;
            var dialogueEventId = WwiseHash.Compute(dialogueEventName);
            var dialogueEventBnk = audioRepository.GetOwnerFileFromDialogueEvent(dialogueEventId, true);

            var soundFilePath = branch.Sounds[0];
            var hircSound = CreateSound(soundId, mixerId, dialogueEventBnk, soundFilePath, dialogueEventName);
            currentMixer.Children.Add(soundId);
            compilerData.Sounds.Add(hircSound);

            var statePath = branch.StatePath;
            var statePathArray = statePath.Split('.');
            ProcessStatePath(rootNode, statePathArray, compilerData, containerId);
        }

        public static void AddMultipleSoundDialogueEvent(IAudioRepository audioRepository, CompilerData compilerData, AkDecisionTree.Node rootNode, ActorMixer currentMixer, CompilerInputProject.ProjectDecisionTree branch)
        {
            var mixerId = currentMixer.Id;
            var containerId = GetNextUsableWwiseId(UsableWwiseId);

            var soundsCount = branch.Sounds.Count;
            var currentSoundIndex = 0;
            var sounds = new List<uint>() { };

            foreach (var sound in branch.Sounds)
            {
                currentSoundIndex++;

                var soundId = GetNextUsableWwiseId(UsableWwiseId);
                var dialogueEventName = currentMixer.DialogueEvent;
                var dialogueEventId = WwiseHash.Compute(dialogueEventName);
                var dialogueEventBnk = audioRepository.GetOwnerFileFromDialogueEvent(dialogueEventId, true);

                var hircSound = CreateSound(soundId, containerId, dialogueEventBnk, sound, dialogueEventName);

                compilerData.Sounds.Add(hircSound);
                sounds.Add(soundId);

                // Once the final sound is reached add everything else.
                if (currentSoundIndex == soundsCount - 1)
                {
                    var hircRandomContainer = CreateRandomContainer(containerId, mixerId, sounds);

                    compilerData.RandomContainers.Add(hircRandomContainer);
                    currentMixer.Children.Add(containerId);

                    var statePath = branch.StatePath;
                    var statePathArray = statePath.Split('.');
                    ProcessStatePath(rootNode, statePathArray, compilerData, containerId);
                }
            }
        }

        private static void ProcessStatePath(AkDecisionTree.Node parentNode, string[] statePathArray, CompilerData compilerData, uint containerId)
        {
            var currentStateIndex = 0;

            foreach (var state in statePathArray)
            {
                // Record all States for adding to a dat file.
                RecordDatData(compilerData.StatesDat, state);

                currentStateIndex++;
                var hashedState = state.Equals("Any", StringComparison.OrdinalIgnoreCase) ? 0 : WwiseHash.Compute(state); // The hashed value of the State that is used in this node.

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
                    var audioNodeId = currentStateIndex == statePathArray.Length ? containerId : 0; // If 0 this property is not initialised. If this is the last state in the path AudioNodeId is set to containerId, otherwise it's set to 0 which removes the property.
                    var children_uIdx = (ushort)(currentStateIndex == statePathArray.Length ? 0 : 1); // If 0 this property is not initialised. If this is the last state in the path Children_uIdx is set to 0 which removes the property otherwise it's set to 1 which means the property can be set later on.
                    var children_uCount = (ushort)(currentStateIndex == statePathArray.Length ? 0 : 1); // If 0 this property is not initialised. If this is the last state in the path Children_uCount is set to 0 which removes the property otherwise it's set to 1 which means the property can be set later on.
                    var newNode = CreateNode(hashedState, audioNodeId, children_uIdx, children_uCount, CompilerConstants.UWeight, CompilerConstants.UProbability);
                    parentNode.Children.Add(newNode);
                    parentNode = newNode;
                }
            }
        }

        private static ActorMixer CreateMixer(uint id, uint directParentId, string dialogueEvent = null)
        {
            return new ActorMixer()
            {
                Id = id,
                DirectParentId = directParentId,
                DialogueEvent = dialogueEvent
            };
        }


        private static Sound CreateSound(uint id, uint directParentId, string attenuationKeyPrefix, string filePath, string dialogueEvent = null)
        {
            return new Sound()
            {
                Id = id,
                DirectParentId = directParentId,
                Attenuation = GetAttenuationId(attenuationKeyPrefix),
                FilePath = filePath,
                DialogueEvent = dialogueEvent,
            };
        }

        private static Action CreateAction(uint id, uint childId, string type)
        {
            return new Action()
            {
                Id = id,
                ChildId = childId,
                Type = type
            };
        }

        private static Event CreateEvent(uint eventId, uint actionId)
        {
            return new Event()
            {
                Id = eventId,
                Actions = new List<uint>() { actionId }
            };
        }

        private static RandomContainer CreateRandomContainer(uint id, uint directParentId, List<uint> children)
        {
            return new RandomContainer()
            {
                Id = id,
                DirectParentId = directParentId,
                Children = children
            };
        }

        public static AkDecisionTree.Node CreateNode(uint key, uint audioNodeId, ushort children_uIdx, ushort children_uCount, ushort uWeight, ushort uProbability)
        {
            return new AkDecisionTree.Node(new AkDecisionTree.BinaryNode
            {
                Key = key,
                AudioNodeId = audioNodeId,
                Children_uIdx = children_uIdx,
                Children_uCount = children_uCount,
                uWeight = uWeight,
                uProbability = uProbability
            });
        }

        public static void RecordDatData(List<string> datData, string value)
        {
            if (!datData.Contains(value))
                datData.Add(value);
        }

        public static uint GetNextUsableWwiseId(uint wwiseStartId)
        {
            wwiseStartId++;

            // throw new NotImplementedException("Todo"); // validate the Id to check it's not already in use.

            return wwiseStartId;
        }

        public static uint GetAttenuationId(string attenuationKeyPrefix)
        {
            var attenuationKey = $"{attenuationKeyPrefix}_attenuation";

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
    }
}
