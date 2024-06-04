using Audio.BnkCompiler.Validation;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using Audio.BnkCompiler.ObjectConfiguration.Warhammer3;
using Shared.GameFormats.WWise.Hirc.Shared;
using static Audio.BnkCompiler.ProjectLoaderHelpers;

namespace Audio.BnkCompiler
{
    public class ProjectLoader
    {
        private readonly PackFileService _pfs;
        public static readonly Dictionary<string, string> EventToMixers = new Dictionary<string, string>();
        public static readonly IVanillaObjectIds VanillaObjectIds = new IdProvider();

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
        private Result<bool> ValidateProjectFile(CompilerData projectFile)
        {
            var validator = new AudioProjectXmlValidator(_pfs, projectFile);
            var result = validator.Validate(projectFile);
            if (result.IsValid == false)
                return Result<bool>.FromError("BnkCompiler-Validation", result);

            // Validate for name collisions and unique ids
            return Result<bool>.FromOk(true);
        }

        // Validate the input file
        private Result<bool> ValidateInputFile(CompilerInputProject projectFile)
        {
            return Result<bool>.FromOk(true);
        }

        private static CompilerData ConvertInputToCompilerData(CompilerInputProject input, CompilerSettings settings)
        {
            var compilerData = new CompilerData();
            compilerData.ProjectSettings.Version = 1;
            compilerData.ProjectSettings.BnkName = input.Settings.BnkName;
            compilerData.ProjectSettings.Language = input.Settings.Language;

            var mixers = new List<ActorMixer>() { };

            if (input.Events != null)
                AddEvents(mixers, input, compilerData);

            if (input.DialogueEvents != null)
                AddDialogueEvents(mixers, input, compilerData);

            compilerData.PreperForCompile(settings.UserOverrideIdForActions, settings.UseOverrideIdForMixers, settings.UseOverrideIdForSounds);
            
            return compilerData;
        }
    }
}
