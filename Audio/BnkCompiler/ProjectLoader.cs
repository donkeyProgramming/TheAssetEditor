using Audio.BnkCompiler.Validation;
using CommonControls.Common;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using SharpDX.MediaFoundation;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

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
            if(packfile == null)
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
                var projectFile = JsonSerializer.Deserialize<CompilerInputProject>(str);
                if (projectFile == null)
                    return Result<CompilerData>.FromError("BnkCompiler-Loader", "Unable to load file. Please validate the Json using an online validator.");

                // Validate the input file
                var validateInputResult = ValidateInputFile(projectFile);
                if (validateInputResult.Success == false)
                    return Result<CompilerData>.FromError(validateInputResult.ErrorList);

                // Convert and validate to compiler input
                var compilerData = ConvertSimpleInputToCompilerData(projectFile, settings);
                var validateProjectResult = ValidateProjectFile(compilerData);
                if (validateProjectResult.Success == false)
                    return Result<CompilerData>.FromError(validateProjectResult.ErrorList);

                return Result<CompilerData>.FromOk(compilerData);
            }
            catch (Exception e)
            {
                return Result<CompilerData>.FromError("BnkCompiler-Loader", $"{e.Message}");
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

            // Create a default mixer
            var mixer = new ActorMixer()
            {
                Name = "RootMixer",
                RootParentId = input.Settings.RootAudioMixer,
            };
            compilerData.ActorMixers.Add(mixer);
            compilerData.ProjectSettings.Version = input.Settings.Version;
            compilerData.ProjectSettings.BnkName = input.Settings.BnkName;

            foreach (var simpleEvent in input.Events)
            {
                var eventId = simpleEvent.Name; ;
                var actionId = $"{simpleEvent.Name}_action";
                var soundId = $"{simpleEvent.Name}_sound";

                var defaultEvent = new Event()
                {
                    Name = eventId,
                    Actions = new List<string>() { actionId }
                };

                var defaultAction = new Action()
                {
                    Name = actionId,
                    Type = "Play",
                    ChildId = soundId
                };

                var defaultSound = new GameSound()
                {
                    Name = soundId,
                    Path = simpleEvent.Sound,
                };

                mixer.Sounds.Add(defaultSound.Name);

                compilerData.Events.Add(defaultEvent);
                compilerData.Actions.Add(defaultAction);
                compilerData.GameSounds.Add(defaultSound);
            }

            compilerData.PreperForCompile(settings.UserOerrideIdForActions, settings.UseOverrideIdForMixers, settings.UseOverrideIdForSounds);
            return compilerData;
        }
    }
}
