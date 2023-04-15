using Audio.BnkCompiler.ObjectGeneration;
using Audio.BnkCompiler.Validation;
using Audio.FileFormats.Dat;
using Audio.FileFormats.WWise;
using Audio.FileFormats.WWise.Bkhd;
using Audio.FileFormats.WWise.Hirc;
using CommonControls.BaseDialogs.ErrorListDialog;
using CommonControls.Common;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
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

        public bool ExportResultToFile { get; set; }
        public bool ConvertResultToXml { get; set; }
        public bool ThrowOnCompileError { get; set; }

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

                    //foreach(var audioFile in compileResult.AudioFiles)
                    //    SaveHelper.SavePackFile(_pfs, "wwise\\audio", audioFile, true);

                   
                }

                outputList.Ok("Compiler", $"Finished {_pfs.GetFullPath(file)}. Overall result:{compileResult != null}");
            }
            return true;
        }

        public CompileResult CompileProject(string path, out ErrorList errorList)
        {
            var pf = _pfs.FindFile(path);
            if (pf == null)
                throw new Exception();

            return CompileProject(pf, out errorList);
        }

        public CompileResult CompileProject(PackFile packfile, out ErrorList errorList)
        {
            errorList = new ErrorList();

            var projectFile = LoadProjectFile(packfile, ref errorList);

            // Validate
            if (ValidateProject(projectFile, out errorList) == false)
                return null;

            // Build the wwise object graph 
            var header = _headerBuilder.Generate(projectFile);
            var hircChunk = _hircBuilder.Generate(projectFile);

            var bnkFile = ConvertToPackFile(header, hircChunk, projectFile.OutputFile);
            var datFile = BuildDat(projectFile);

            return new CompileResult()
            {
                OutputBnkFile = bnkFile,
                OutputDatFile = datFile,
                NameList = null
            };
        }

        bool ValidateProject(AudioProjectXml projectFile, out ErrorList errorList)
        {
            errorList = new ErrorList();

            var validator = new AudioProjectXmlValidator(_pfs, projectFile);
            var result = validator.Validate(projectFile);
            if (result.IsValid == false)
            {
                foreach (var error in result.Errors)
                    errorList.Error("BnkCompiler", error.ErrorMessage);

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


        PackFile BuildDat(AudioProjectXml projectFile)
        {
            var outputName = "event_data__" + Path.GetFileNameWithoutExtension(projectFile.OutputFile) + ".dat";
            var datFile = new SoundDatFile();

            foreach (var wwiseEvent in projectFile.Events)
                datFile.Event0.Add(new SoundDatFile.EventWithValue() { EventName = wwiseEvent.Id, Value = 400 });

            var bytes = DatFileParser.GetAsByteArray(datFile);
            var packFile = new PackFile(outputName, new MemorySource(bytes));
            var reparsedSanityFile = DatFileParser.Parse(packFile, false);
            return packFile;
        }


        AudioProjectXml LoadProjectFile(PackFile packfile, ref ErrorList errorList)
        {
            try
            {
                var bytes = packfile.DataSource.ReadData();
                var str = Encoding.UTF8.GetString(bytes);

                using var stream = GenerateStreamFromString(str);
                XmlSerializer serializer = new XmlSerializer(typeof(AudioProjectXml));
                var result = serializer.Deserialize(stream);
                var typedResult = result as AudioProjectXml;
                if (typedResult == null)
                    throw new Exception($"Error loading project, typed result is null, actual: '{result}'");
                return typedResult;
            }
            catch (Exception e)
            {
                errorList.Error("Unable to serialize project file", $"{e.Message} Please validate the XML at https://www.w3schools.com/xml/xml_validator.asp");
                return null;
            }
        }

        public static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}
