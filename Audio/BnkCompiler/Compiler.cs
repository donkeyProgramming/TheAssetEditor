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
        public class CompileResult
        {
            public PackFile OutputBnkFile { get; set; }
            public PackFile OutputDatFile { get; set; }
            public PackFile NameList { get; set; }
        }

        private readonly PackFileService _pfs;
        private readonly HircChuckBuilder _hircBuilder;
        private readonly BnkHeaderBuilder _headerBuilder;

        public Compiler(PackFileService pfs, HircChuckBuilder hircBuilder, BnkHeaderBuilder headerBuilder)
        {
            _pfs = pfs;
            _hircBuilder = hircBuilder;
            _headerBuilder = headerBuilder;
        }

        public bool CompileAllProjects(out ErrorList outputList)
        {
            outputList = new ErrorList();

            if (_pfs.HasEditablePackFile() == false)
                return false;

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

            // Build the wwise object graph 
            var header = _headerBuilder.Generate(audioProject);
            var hircChunk = _hircBuilder.Generate(audioProject);

            var compileResult = new CompileResult()
            {
                OutputBnkFile = ConvertToPackFile(header, hircChunk, audioProject.ProjectSettings.BnkName),
                OutputDatFile = BuildDat(audioProject),
                NameList = null
            };

            if (audioProject.ProjectSettings.ExportResultToFile)
            {
                var bnkPath = Path.Combine(audioProject.ProjectSettings.OutputFilePath, $"{audioProject.ProjectSettings.BnkName}.bnk");
                File.WriteAllBytes(bnkPath, compileResult.OutputBnkFile.DataSource.ReadData());

                var datPath = Path.Combine(audioProject.ProjectSettings.OutputFilePath, $"{audioProject.ProjectSettings.BnkName}.dat");
                File.WriteAllBytes(datPath, compileResult.OutputDatFile.DataSource.ReadData());

                if (audioProject.ProjectSettings.ConvertResultToXml)
                    BnkToXmlConverter.Convert(audioProject.ProjectSettings.WWiserPath, bnkPath, true);
            }

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
