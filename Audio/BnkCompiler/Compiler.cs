using Audio.BnkCompiler.ObjectGeneration;
using Audio.BnkCompiler.Validation;
using Audio.FileFormats.Dat;
using Audio.FileFormats.WWise;
using Audio.FileFormats.WWise.Bkhd;
using Audio.FileFormats.WWise.Hirc;
using Audio.Utility;
using CommonControls.Common;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using CommunityToolkit.Diagnostics;
using SharpDX.MediaFoundation;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using static CommonControls.BaseDialogs.ErrorListDialog.ErrorListViewModel;

namespace CommonControls.Editors.AudioEditor.BnkCompiler
{
    public class Compiler
    {

        private readonly PackFileService _pfs;
        private readonly HichBuilder _hircBuilder;
        private readonly BnkHeaderBuilder _headerBuilder;

        public bool UserOerrideIdForActions { get; set; } = false;
        public bool UseOverrideIdForMixers { get; set; } = false;
        public bool UseOverrideIdForSounds { get; set; } = false;

        public Compiler(PackFileService pfs, HichBuilder hircBuilder, BnkHeaderBuilder headerBuilder)
        {
            _pfs = pfs;
            _hircBuilder = hircBuilder;
            _headerBuilder = headerBuilder;
        }

        public bool CompileAllProjects(out ErrorList outputList)
        {
            outputList = new ErrorList();

            if (_pfs.HasEditablePackFile() == false)
            {
                outputList.Error("Compiler", "No Editable pack is set");
                return false;
            }

            var allProjectFiles = _pfs.FindAllWithExtention(".xml").Where(x => x.Name.Contains("bnk.xml"));
            outputList.Ok("Compiler", $"{allProjectFiles.Count()} projects found to compile.");

            foreach (var file in allProjectFiles)
            {
                outputList.Ok("Compiler", $"Starting {_pfs.GetFullPath(file)}");
                var compileResult = CompileProject(file, out var instanceErrorList);
                if (compileResult == null)
                    outputList.AddAllErrors(instanceErrorList);

                if (compileResult != null)
                {
                    SaveHelper.SavePackFile(_pfs, "wwise\\audio", compileResult.OutputBnkFile, true);
                    SaveHelper.SavePackFile(_pfs, "wwise\\audio", compileResult.OutputDatFile, true);
                }

                outputList.Ok("Compiler", $"Finished {_pfs.GetFullPath(file)}. Overall result:{compileResult != null}");
            }
            return true;
        }

        public CompileResult CompileProject(string path, out ErrorList errorList)
        {
            var projectPackFile = _pfs.FindFile(path);
            Guard.IsNotNull(projectPackFile);

            return CompileProject(projectPackFile, out errorList);
        }

        public CompileResult CompileProject(PackFile packfile, out ErrorList errorList)
        {
            errorList = new ErrorList();

            var audioProject = LoadProjectFile(packfile, ref errorList);

            // Validate
            if (ValidateProject(audioProject, out errorList) == false)
                return null;

            audioProject.ComputeAllWriteIds(UserOerrideIdForActions, UseOverrideIdForMixers, UseOverrideIdForSounds);

            //Ensure all write ids are not causing conflicts
            //var allWriteIds = audioProject.

            // Build the wwise object graph 
            var header = _headerBuilder.Generate(audioProject);
            var hircChunk = _hircBuilder.Generate(audioProject);


            var allIds = hircChunk.Hircs.Select(x => x.Id).ToList();
            var originalCount = allIds.Count();
            var uniqueCount = allIds.Distinct().Count();
            Guard.IsEqualTo(originalCount, uniqueCount);

            var compileResult = new CompileResult()
            {
                Project = audioProject,
                OutputBnkFile = ConvertToPackFile(header, hircChunk, audioProject.ProjectSettings.BnkName),
                OutputDatFile = BuildDat(audioProject),
                NameList = null,
            };

            return compileResult;
        }

        bool ValidateProject(AudioInputProject projectFile, out ErrorList errorList)
        {
            errorList = new ErrorList();

            var validator = new AudioProjectXmlValidator(_pfs, projectFile);
            var result = validator.Validate(projectFile);
            if (result.IsValid == false)
            {
                foreach (var error in result.Errors)
                    errorList.Error("BnkCompiler", $"{error.PropertyName} - {error.ErrorMessage}");

                return false;
            }
            return true;
        }

        PackFile ConvertToPackFile(BkhdHeader header, HircChunk hircChunk, string outputFile)
        {
            var headerBytes = BkhdParser.GetAsByteArray(header);
            var hircBytes = new HircParser().GetAsBytes(hircChunk);

            // Write
            using var memStream = new MemoryStream();
            memStream.Write(headerBytes);
            memStream.Write(hircBytes);
            var bytes = memStream.ToArray();

            // Convert to output and parse for sanity
            var bnkPackFile = new PackFile(outputFile, new MemorySource(bytes));
            var parser = new Bnkparser();
            var result = parser.Parse(bnkPackFile, "test\\fakefilename.bnk");

            return bnkPackFile;
        }

        PackFile BuildDat(AudioInputProject projectFile)
        {
            var outputName = $"event_data__{projectFile.ProjectSettings.BnkName}.dat";
            var datFile = new SoundDatFile();

            foreach (var wwiseEvent in projectFile.Events)
                datFile.Event0.Add(new SoundDatFile.EventWithValue() { EventName = wwiseEvent.Id, Value = 400 });

            var bytes = DatFileParser.GetAsByteArray(datFile);
            var packFile = new PackFile(outputName, new MemorySource(bytes));
            var reparsedSanityFile = DatFileParser.Parse(packFile, false);
            return packFile;
        }


        AudioInputProject LoadProjectFile(PackFile packfile, ref ErrorList errorList)
        {
            try
            {
                // Dont allow other attributes then waht is known
                var bytes = packfile.DataSource.ReadData();
                var str = Encoding.UTF8.GetString(bytes);
                var myDeserializedClass = JsonSerializer.Deserialize<AudioInputProject>(str);
                if(myDeserializedClass == null) 
                {
                    errorList.Error("Unable to load project file", "Please validate the Json using an online validator.");
                    return null;
                }
                return myDeserializedClass;
            }
            catch (Exception e)
            {
                errorList.Error("Unable to load project file", $"{e.Message} Please validate the Json using an online validator.");
                return null;
            }
        }


    }
}
