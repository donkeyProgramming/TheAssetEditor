using Audio.Utility;
using CommonControls.FileTypes.PackFiles.Models;
using CommunityToolkit.Diagnostics;
using System.IO;

namespace CommonControls.Editors.AudioEditor.BnkCompiler
{
    public class CompileResult
    {
        public AudioInputProject Project { get; set; }
        public PackFile OutputBnkFile { get; set; }
        public PackFile OutputDatFile { get; set; }
        public PackFile NameList { get; set; }
    }


    public class CompilerResultHandler
    {
        public string WWiserPath { get; set; } = "D:\\Research\\Audio\\WWiser\\wwiser.pyz";

        public void SaveToPackFile()
        { 
        
        }

        public void SaveToFile(CompileResult result, string outputDirectory, bool convertResultToXml)
        {
            var bnkPath = Path.Combine(outputDirectory, $"{result.Project.ProjectSettings.BnkName}.bnk");
            File.WriteAllBytes(bnkPath, result.OutputBnkFile.DataSource.ReadData());

            var datPath = Path.Combine(outputDirectory, $"{result.Project.ProjectSettings.BnkName}.dat");
            File.WriteAllBytes(datPath, result.OutputDatFile.DataSource.ReadData());

            if (convertResultToXml)
            {
                Guard.IsNotNullOrEmpty(WWiserPath);
                BnkToXmlConverter.Convert(WWiserPath, bnkPath, true);
            }
        }


        /*
         * 
         *  public bool CompileAllProjects(out ErrorList outputList)
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
         */



    }

}
