using Audio.BnkCompiler.Validation;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using Audio.Utility;
using Audio.BnkCompiler.ObjectConfiguration.Warhammer3;
using Audio.FileFormats.WWise.Hirc.Shared;

namespace Audio.BnkCompiler
{
    public class ProjectLoader
    {
        private readonly PackFileService _pfs;
        private static readonly IVanillaObjectIds _VanillaObjectIds = new IdProvider();
        private static readonly Dictionary<string, string> s_eventToMixers = new Dictionary<string, string>();

        public ProjectLoader(PackFileService pfs)
        {
            _pfs = pfs;
        }

        public Result<CompilerData> LoadProject(string path, CompilerSettings settings)
        {
            // Find packfile
            var packfile = _pfs.FindFile(path);
            if (packfile == null)
                return Result<CompilerData>.FromError("BnkCompiler-Loader", $"Unable to find file '{path}'");

            // Load project
            return LoadProject(packfile, settings);
        }

        public Result<CompilerData> LoadProject(PackFile packfile, CompilerSettings settings)
        {
            try
            {
                // Configure JsonSerializerOptions
                var options = new JsonSerializerOptions
                {
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };

                // Load the file
                var bytes = packfile.DataSource.ReadData();
                var str = Encoding.UTF8.GetString(bytes);
                var projectFile = JsonSerializer.Deserialize<CompilerInputProject>(str, options);

                if (projectFile == null)
                    return Result<CompilerData>.FromError("BnkCompiler-Loader", "Unable to load file. Please validate the Json using an online validator.");

                // Validate the input file
                var validateInputResult = ValidateInputFile(projectFile);
                if (validateInputResult.IsSuccess == false)
                    return Result<CompilerData>.FromError(validateInputResult.LogItems);

                // Convert and validate to compiler input
                var compilerData = ConvertInputToCompilerData(projectFile, settings);
                SaveCompilerDataToPackFile(compilerData, settings, packfile);
                var validateProjectResult = ValidateProjectFile(compilerData);
                if (validateProjectResult.IsSuccess == false)
                    return Result<CompilerData>.FromError(validateProjectResult.LogItems);

                return Result<CompilerData>.FromOk(compilerData);
            }
            catch (JsonException e)
            {
                return Result<CompilerData>.FromError("BnkCompiler-Loader", $"{e.Message}");
            }
            catch (Exception e)
            {
                return Result<CompilerData>.FromError("BnkCompiler-Loader", $"{e.Message}");
            }
        }

        private void SaveCompilerDataToPackFile(CompilerData compilerData, CompilerSettings settings, PackFile packfile)
        {
            if (settings.SaveGeneratedCompilerInput)
            {
                // Get file path and directory
                var filePath = _pfs.GetFullPath(packfile);
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
                var directory = Path.GetDirectoryName(filePath);

                // Serialize compiler data
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                var compilerDataAsStr = JsonSerializer.Serialize(compilerData, options);
                var outputName = $"{fileNameWithoutExtension}_generated.json";

                // Add file to pack
                _pfs.AddFileToPack(_pfs.GetEditablePack(), directory, PackFile.CreateFromASCII(outputName, compilerDataAsStr));
            }
        }

        // Validate the project file
        Result<bool> ValidateProjectFile(CompilerData projectFile)
        {
            var validator = new AudioProjectXmlValidator(_pfs, projectFile);
            var result = validator.Validate(projectFile);
            if (result.IsValid == false)
                return Result<bool>.FromError("BnkCompiler-Validation", result);

            // Validate for name collisions and unique ids
            return Result<bool>.FromOk(true);
        }

        // Validate the input file
        Result<bool> ValidateInputFile(CompilerInputProject projectFile)
        {
            return Result<bool>.FromOk(true);
        }

        private static void AddEventMixersToCompilerData(List<ActorMixer> mixers, CompilerInputProject input, CompilerData compilerData)
        {
            // Add mixers for normal events
            foreach (var hircEvent in input.Events)
            {
                var eventId = hircEvent.Event;
                var eventMixer = hircEvent.Mixer;
                var mixerId = $"{ProjectLoaderHelpers.GenerateRandomNumber()}_mixer_{ProjectLoaderHelpers.GenerateRandomNumber()}";
                var mixerParent = _VanillaObjectIds.EventMixerIds[eventMixer.ToLower()];

                if (!s_eventToMixers.ContainsKey(eventId))
                {
                    s_eventToMixers.Add(eventId, mixerId);

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

        private static void AddDialogueEventMixersToCompilerData(List<ActorMixer> mixers, CompilerInputProject input, CompilerData compilerData)
        {
            // Add mixers for dialogue events
            foreach (var hircDialogueEvent in input.DialogueEvents)
            {
                var eventId = hircDialogueEvent.DialogueEvent;
                var mixerId = $"{ProjectLoaderHelpers.GenerateRandomNumber()}_mixer_{ProjectLoaderHelpers.GenerateRandomNumber()}";
                var dialogueEventBnk = DialogueEventData.GetBnkFromDialogueEvent(eventId);
                var mixerParent = _VanillaObjectIds.DialogueEventMixerIds[dialogueEventBnk];

                if (!s_eventToMixers.ContainsKey(eventId))
                {
                    s_eventToMixers.Add(eventId, mixerId);

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

        private static void AddSingleSoundEvent(CompilerData compilerData, ActorMixer currentMixer, CompilerInputProject.ProjectEvent hircEvent)
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

        private static void AddMultipleSoundEvent(CompilerData compilerData, ActorMixer currentMixer, CompilerInputProject.ProjectEvent hircEvent)
        {
            var eventMixer = hircEvent.Mixer;
            var eventId = hircEvent.Event;
            var mixerId = currentMixer.Name;

            var containerId = $"{ProjectLoaderHelpers.GenerateRandomNumber()}_random_container_{ProjectLoaderHelpers.GenerateRandomNumber()}";

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
                    Attenuation = ProjectLoaderHelpers.GetAttenuationId(eventMixer)
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

        private static void AddSingleSoundDialogueEvent(CompilerData compilerData, AkDecisionTree.Node rootNode, ActorMixer currentMixer, CompilerInputProject.ProjectDecisionTree branch)
        {
            var mixerId = currentMixer.Name;
            var containerId = $"{ProjectLoaderHelpers.GenerateRandomNumber()}_random_container_{ProjectLoaderHelpers.GenerateRandomNumber()}";
            var hashedContainer = WWiseHash.Compute(containerId);

            var statePath = branch.StatePath;
            var currentSoundIndex = 0;
            var currentStateIndex = 0;

            foreach (var sound in branch.Sounds)
            {
                currentSoundIndex++;
                var soundId = $"{ProjectLoaderHelpers.GenerateRandomNumber()}_sound_{ProjectLoaderHelpers.GenerateRandomNumber()}";

                var dialogueEvent = currentMixer.DialogueEvent;
                var dialogueEventBnk = DialogueEventData.GetBnkFromDialogueEvent(dialogueEvent);

                var defaultSound = new GameSound()
                {
                    Name = soundId,
                    Path = sound,
                    DirectParentId = mixerId,
                    DialogueEvent = currentMixer.DialogueEvent,
                    Attenuation = ProjectLoaderHelpers.GetAttenuationId(dialogueEventBnk)
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

        private static void AddMultipleSoundDialogueEvent(CompilerData compilerData, AkDecisionTree.Node rootNode, ActorMixer currentMixer, CompilerInputProject.ProjectDecisionTree branch)
        {
            var mixerId = currentMixer.Name;
            var containerId = $"{ProjectLoaderHelpers.GenerateRandomNumber()}_random_container_{ProjectLoaderHelpers.GenerateRandomNumber()}";
            var hashedContainer = WWiseHash.Compute(containerId);

            var soundsCount = branch.Sounds.Count;
            var statePath = branch.StatePath;
            var currentSoundIndex = 0;
            var currentStateIndex = 0;

            var sounds = new List<string>() { };

            foreach (var sound in branch.Sounds)
            {
                currentSoundIndex++;
                var soundId = $"{ProjectLoaderHelpers.GenerateRandomNumber()}_sound_{ProjectLoaderHelpers.GenerateRandomNumber()}";

                var dialogueEvent = currentMixer.DialogueEvent;
                var dialogueEventBnk = DialogueEventData.GetBnkFromDialogueEvent(dialogueEvent);

                var defaultSound = new GameSound()
                {
                    Name = soundId,
                    Path = sound,
                    DirectParentId = containerId,
                    DialogueEvent = currentMixer.DialogueEvent,
                    Attenuation = ProjectLoaderHelpers.GetAttenuationId(dialogueEventBnk)
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

        private static CompilerData ConvertInputToCompilerData(CompilerInputProject input, CompilerSettings settings)
        {
            var compilerData = new CompilerData();
            compilerData.ProjectSettings.Version = 1;
            compilerData.ProjectSettings.BnkName = input.Settings.BnkName;
            compilerData.ProjectSettings.Language = input.Settings.Language;

            var mixers = new List<ActorMixer>() { };

            if (input.Events != null)
            {
                // Add mixers to compilerData first so they can be individually referenced later
                AddEventMixersToCompilerData(mixers, input, compilerData);

                // Add events and their associated actions, containers, and sounds
                foreach (var hircEvent in input.Events)
                {
                    var eventId = hircEvent.Event;
                    var mixerId = s_eventToMixers[eventId];
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

            if (input.DialogueEvents != null)
            {
                // Add mixers to compilerData first so they can be individually referenced later
                AddDialogueEventMixersToCompilerData(mixers, input, compilerData);

                // Add dialogue_events and their associated nodes, containers, and sounds
                foreach (var hircDialogueEvent in input.DialogueEvents)
                {
                    var eventId = hircDialogueEvent.DialogueEvent;
                    var mixerId = s_eventToMixers[eventId];
                    var currentMixer = new ActorMixer();
                    var dialogueEvent = new DialogueEvent();

                    var rootNode = new AkDecisionTree.Node(new AkDecisionTree.BinaryNode
                    {
                        Key = 0, // If value is 0 it will be read and written as 0 as some keys can equal 0.
                        AudioNodeId = 0, // Set to 0 if no value, otherwise value will be set by the script.
                        Children_uIdx = 1, // Set to 0 if no value, set to 1 if value.
                        Children_uCount = 1, // Set to 0 if no value, set to 1 if value.
                        uWeight = 50, // Always 50
                        uProbability = 100 // Always 100
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

            compilerData.PreperForCompile(settings.UserOverrideIdForActions, settings.UseOverrideIdForMixers, settings.UseOverrideIdForSounds);
            return compilerData;
        }
    }
}
