using Audio.FileFormats;
using Audio.FileFormats.Dat;
using Audio.FileFormats.WWise;
using Audio.FileFormats.WWise.Bkhd;
using Audio.FileFormats.WWise.Hirc;
using CommonControls.BaseDialogs.ErrorListDialog;
using CommonControls.Common;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace CommonControls.Editors.AudioEditor.BnkCompiler
{

    public class Compiler
    {
        public class CompileResult
        {
            public PackFile OutputBnkFile { get; set; }
            public PackFile OutputDatFile { get; set; }
            public PackFile NameList { get; set; }
            public List<PackFile> AudioFiles { get; set; } = new List<PackFile>();
        }

        private readonly PackFileService _pfs;

        public AudioProjectXml ProjectFile { get; private set; }

        public Compiler(PackFileService pfs)
        {
            _pfs = pfs;
        }

        public bool CompileAllProjects(out ErrorListViewModel.ErrorList outputList)
        {
            outputList = new ErrorListViewModel.ErrorList();

            if (_pfs.HasEditablePackFile() == false)
                return false;

            var allProjectFiles = _pfs.FindAllWithExtention(".xml").Where(x => x.Name.Contains("bnk.xml"));
            outputList.Ok("Compiler", $"{allProjectFiles.Count()} projects found to compile.");

            foreach (var file in allProjectFiles)
            {
                outputList.Ok("Compiler", $"Starting {_pfs.GetFullPath(file)}");
                var compileResult = CompileProject(file, ref outputList);
                if (compileResult != null)
                {
                    SaveHelper.SavePackFile(_pfs, "wwise\\audio", compileResult.OutputBnkFile, true);
                    SaveHelper.SavePackFile(_pfs, "wwise\\audio", compileResult.OutputDatFile, true);

                    //foreach(var audioFile in compileResult.AudioFiles)
                    //    SaveHelper.SavePackFile(_pfs, "wwise\\audio", audioFile, true);
                }

                outputList.Ok("Compiler", $"Finished {_pfs.GetFullPath(file)}. Overall result:{true}");
            }
            return true;
        }

        public CompileResult CompileProject(string path, out ErrorListViewModel.ErrorList errorList)
        {
            var pf = _pfs.FindFile(path);
            if (pf == null)
                throw new Exception();

            var errorList = new ErrorListViewModel.ErrorList();
            return CompileProject(pf, ref errorList);
        }

        public CompileResult CompileProject(PackFile packfile, ref ErrorListViewModel.ErrorList errorList)
        {
            ProjectFile = LoadFile(packfile, ref errorList);
            if (ProjectFile == null)
                return null;

            var validationResult = AudioProjectValidator.Validate(ProjectFile, _pfs, ref errorList);
            if (validationResult == false)
                return null;

            var generator = new WWiseObjectGenerator(_pfs, ProjectFile);
            var wwiseProject = generator.Generate();

            var bnkFile = ConvertToPackFile(wwiseProject, ProjectFile.OutputFile);
            var datFile = BuildDat(ProjectFile);

            return new CompileResult()
            {
                OutputBnkFile = bnkFile,
                OutputDatFile = datFile,
                NameList = null
            };
        }

        PackFile ConvertToPackFile(WWiseObjectGenerator.WWiseProject wWiseProject, string outputFile)
        {
            var headerBytes = BkhdParser.GetAsByteArray(wWiseProject.Header);
            var hircBytes = new HircParser().GetAsBytes(wWiseProject.HircItems);

            // Write
            using var memStream = new MemoryStream();
            memStream.Write(headerBytes);
            memStream.Write(hircBytes);
            var bytes = memStream.ToArray();

            // Convert to output and parse for sanity
            var bnkPackFile = new PackFile(outputFile, new MemorySource(bytes));
            var parser = new Bnkparser();
            var result = parser.Parse(bnkPackFile, "test\\TestFile.bnk");

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
       

        AudioProjectXml LoadFile(PackFile packfile, ref ErrorListViewModel.ErrorList errorList)
        {
            try
            {
                var bytes = packfile.DataSource.ReadData();
                var str = Encoding.UTF8.GetString(bytes);

                using var stream = GenerateStreamFromString(str);
                XmlSerializer serializer = new XmlSerializer(typeof(AudioProjectXml));
                var result = serializer.Deserialize(stream);
                return result as AudioProjectXml;
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
