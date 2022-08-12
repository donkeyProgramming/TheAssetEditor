using CommonControls.Editors.AudioEditor.BnkCompiler;
using CommonControls.Services;
using System.IO;
using System.Linq;

namespace AudioResearch
{
    public static class BnkCompilerTest
    {
        public static bool Run(string projectFilePath, PackFileService pfs)
        {
            var compiler = new Compiler(pfs);
            var fileContent = File.ReadAllText(projectFilePath);
            var result = compiler.Compile(fileContent, out var errorList);

            if (errorList.Errors.Count == 0 && result)
            {
                var project = compiler.ProjectFile;

                // Save to disk for easy debug
                var chunk = compiler.OutputBnkFile.DataSource.ReadDataAsChunk();
                chunk.SaveToFile($"Data\\{project.OutputFile}".ToLower().Trim());

                return true;
            }

            throw new System.Exception("Something went wrong");
        }
    }
}
