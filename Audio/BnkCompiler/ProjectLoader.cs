using Audio.BnkCompiler.Validation;
using Microsoft.Xna.Framework.Media;
using Newtonsoft.Json;
using SharedCore.ErrorHandling;
using SharedCore.PackFiles;
using SharedCore.PackFiles.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;


namespace Audio.BnkCompiler
{
    public class ProjectLoader
    {
        private readonly PackFileService _pfs;

        public ProjectLoader(PackFileService pfs)
        {
            _pfs = pfs;
        }

        public Result<CompilerData> LoadProject(string path, CompilerSettings settings)
        {
            var packfile = _pfs.FindFile(path);
            if (packfile == null)
                return Result<CompilerData>.FromError("BnkCompiler-Loader", $"Unable to find file '{path}'");
            return LoadProject(packfile, settings);
        }

        public Result<CompilerData> LoadProject(PackFile packfile, CompilerSettings settings)
        {
            try
            {
                // Load the file
                var bytes = packfile.DataSource.ReadData();
                var str = Encoding.UTF8.GetString(bytes);
                var projectFile = JsonConvert.DeserializeObject<CompilerInputProject>(str, new JsonSerializerSettings() { MissingMemberHandling = MissingMemberHandling.Error });
                if (projectFile == null)
                    return Result<CompilerData>.FromError("BnkCompiler-Loader", "Unable to load file. Please validate the Json using an online validator.");

                // Validate the input file
                var validateInputResult = ValidateInputFile(projectFile);
                if (validateInputResult.IsSuccess == false)
                    return Result<CompilerData>.FromError(validateInputResult.LogItems);

                // Convert and validate to compiler input
                var compilerData = ConvertSimpleInputToCompilerData(projectFile, settings);
                SaveCompilerDataToPackFile(compilerData, settings, packfile);
                var validateProjectResult = ValidateProjectFile(compilerData);
                if (validateProjectResult.IsSuccess == false)
                    return Result<CompilerData>.FromError(validateProjectResult.LogItems);

                return Result<CompilerData>.FromOk(compilerData);
            }
            catch (JsonSerializationException e)
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
                var filePath = _pfs.GetFullPath(packfile);
                var fileNameWithoutExtenetion = Path.GetFileNameWithoutExtension(filePath);
                var directory = Path.GetDirectoryName(filePath);

                var compilerDataAsStr = JsonConvert.SerializeObject(compilerData, Formatting.Indented);
                var outputName = $"{fileNameWithoutExtenetion}_generated.json";
                _pfs.AddFileToPack(_pfs.GetEditablePack(), directory, PackFile.CreateFromASCII(outputName, compilerDataAsStr));
            }
        }

        Result<bool> ValidateProjectFile(CompilerData projectFile)
        {
            var validator = new AudioProjectXmlValidator(_pfs, projectFile);
            var result = validator.Validate(projectFile);
            if (result.IsValid == false)
                return Result<bool>.FromError("BnkCompiler-Validation", result);

            // Validate for name collisions and uniqe ids
            return Result<bool>.FromOk(true);
        }

        Result<bool> ValidateInputFile(CompilerInputProject projectFile)
        {
            return Result<bool>.FromOk(true);
        }

        CompilerData ConvertSimpleInputToCompilerData(CompilerInputProject input, CompilerSettings settings)
        {

            var compilerData = new CompilerData();
            compilerData.ProjectSettings.Version = 1;
            compilerData.ProjectSettings.BnkName = input.Settings.BnkName;
            compilerData.ProjectSettings.Language = input.Settings.Language;

            var mixers = new List<ActorMixer>() { };
            var actions = new List<string>() { };
            var sounds = new List<string>() { };

            var currentItem = 0;
            var currentSound = 0;

            // Add mixers to compilerData first so they can be individually referenced later
            foreach (var item in input.Project)
            {
                var mixerId = $"{item.Event}_mixer";
                var mixerParent = item.ActorMixer;
                var isMixerAdded = false;

                foreach (var mixerItem in mixers)
                {
                    if (mixerItem.DirectParentId == mixerParent)
                    {
                        isMixerAdded = true;
                        break;
                    }
                }

                if (isMixerAdded == false)
                {
                    var mixer = new ActorMixer()
                    {
                        Name = mixerId,
                        DirectParentId = mixerParent
                    };

                    mixers.Add(mixer);
                    compilerData.ActorMixers.Add(mixer);
                }
            }

            // Add all other objects relating to individual mixers
            foreach (var item in input.Project)
            {
                currentItem = currentItem + 1;
                var eventId = item.Event;
                var mixerId = $"{item.Event}_mixer";
                var mixerParent = item.ActorMixer;
                var defaultSound = new GameSound();
                var currentMixer = new ActorMixer();

                foreach (var mixerItem in mixers)
                {
                    if (mixerItem.DirectParentId == mixerParent)
                    {
                        currentMixer = mixerItem;
                        break;
                    }
                }

                var soundsCount = item.Sounds.Count();

                if (item.SoundContainerType != null || (soundsCount > 1 && item.SoundContainerType == null))
                {
                    if (item.SoundContainerType == "Random" || (soundsCount > 1 && item.SoundContainerType == null)) // if there's a random container
                    {
                        foreach (var sound in item.Sounds)
                        {
                            currentSound = currentSound + 1;
                            var soundId = $"{eventId}_sound_{currentSound}";
                            var actionId = $"{eventId}_action_{currentSound}";
                            var containerId = $"{eventId}_random_container";

                            defaultSound = new GameSound()
                            {
                                Name = soundId,
                                Path = sound,
                                DirectParentID = containerId
                            };

                            compilerData.GameSounds.Add(defaultSound);
                            sounds.Add(soundId);

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
                                    Children = sounds
                                };

                                compilerData.RandomContainers.Add(defaultRandomContainer);
                                currentMixer.Children.Add(containerId);

                                var defaultEvent = new Event()
                                {
                                    Name = eventId,
                                    Actions = new List<string>() { actionId }
                                };

                                compilerData.Events.Add(defaultEvent);
                                sounds = new List<string>();
                                currentSound = 0;
                            }
                        }
                    }
                }
                else // if there's no container
                {
                    foreach (var sound in item.Sounds)
                    {
                        currentSound = currentSound + 1;
                        var soundId = $"{eventId}_sound_{currentSound}";
                        var actionId = $"{eventId}_action_{currentSound}";

                        defaultSound = new GameSound()
                        {
                            Name = soundId,
                            Path = sound,
                            DirectParentID = mixerId
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
                        actions.Add(actionId);

                        if (currentSound == soundsCount)
                        {
                            var defaultEvent = new Event()
                            {
                                Name = eventId,
                                Actions = actions
                            };

                            compilerData.Events.Add(defaultEvent);
                            actions = new List<string>();
                            currentSound = 0;
                        }
                    }
                }
            }

            compilerData.PreperForCompile(settings.UserOverrideIdForActions, settings.UseOverrideIdForMixers, settings.UseOverrideIdForSounds);
            return compilerData;
        }
    }
}
