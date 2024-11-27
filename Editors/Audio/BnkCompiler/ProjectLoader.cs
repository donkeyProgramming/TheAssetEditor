using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Editors.Audio.BnkCompiler.ObjectConfiguration.Warhammer3;
using Editors.Audio.BnkCompiler.Validation;
using Editors.Audio.Storage;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using static Editors.Audio.BnkCompiler.ProjectLoaderHelpers;
using static Shared.Core.PackFiles.IPackFileService;

namespace Editors.Audio.BnkCompiler
{
    public class ProjectLoader
    {
        private readonly IPackFileService _packFileService;
        public static readonly Dictionary<uint, uint> EventMixers = new Dictionary<uint, uint>();
        public static readonly Dictionary<uint, uint> DialogueEventMixers = new Dictionary<uint, uint>();
        public static readonly IVanillaObjectIds VanillaObjectIds = new WwiseIdProvider();
        private readonly IAudioRepository _audioRepository;

        public ProjectLoader(IPackFileService packFileService, IAudioRepository audioRepository)
        {
            _packFileService = packFileService;
            _audioRepository = audioRepository;
        }

        public Result<CompilerData> LoadProject(string path, CompilerSettings settings)
        {
            // Find packfile
            var packfile = _packFileService.FindFile(path);
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
                var compilerData = ConvertInputToCompilerData(_audioRepository, projectFile, settings);

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
                var filePath = _packFileService.GetFullPath(packfile);
                var fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(filePath);
                var directory = System.IO.Path.GetDirectoryName(filePath);

                // Serialize compiler data
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                var compilerDataAsStr = JsonSerializer.Serialize(compilerData, options);
                var outputName = $"{fileNameWithoutExtension}_generated.json";

                // Add file to pack
                var fileEntry = new NewPackFileEntry(directory, PackFile.CreateFromASCII(outputName, compilerDataAsStr));
                _packFileService.AddFilesToPack(_packFileService.GetEditablePack(), [fileEntry]);
            }
        }

        private Result<bool> ValidateProjectFile(CompilerData projectFile)
        {
            var validator = new AudioProjectXmlValidator(_packFileService, projectFile);
            var result = validator.Validate(projectFile);
            if (result.IsValid == false)
                return Result<bool>.FromError("BnkCompiler-Validation", result);

            // Validate for name collisions and unique ids
            return Result<bool>.FromOk(true);
        }

        private Result<bool> ValidateInputFile(CompilerInputProject projectFile)
        {
            return Result<bool>.FromOk(true);
        }

        private static CompilerData ConvertInputToCompilerData(IAudioRepository audioRepository, CompilerInputProject input, CompilerSettings settings)
        {
            var compilerData = new CompilerData();
            compilerData.ProjectSettings.Version = 1;
            compilerData.ProjectSettings.BnkName = input.Settings.BnkName;
            compilerData.ProjectSettings.Language = input.Settings.Language;
            compilerData.ProjectSettings.WwiseStartId = input.Settings.WwiseStartId;

            var mixers = new List<ActorMixer>() { };

            // Add Events and related objects.
            if (input.Events != null)
                AddEvents(mixers, input, compilerData);

            // Add Dialogue Events and related objects.
            if (input.DialogueEvents != null)
                AddDialogueEvents(audioRepository, mixers, input, compilerData);

            compilerData.StoreWwiseObjects();

            return compilerData;
        }
    }
}
