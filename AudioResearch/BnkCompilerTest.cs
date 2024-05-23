using SharedCore.PackFiles;

namespace AudioResearch
{
    public static class BnkCompilerTest
    {
        public static bool Run(string projectFilePath, PackFileService pfs, out string outputFile)
        {
            //outputFile = null;
            //var compiler = new Compiler(pfs);
            //var fileContent = File.ReadAllBytes(projectFilePath);
            //var packFile = new PackFile("project", new MemorySource(fileContent));
            //
            //var compileResultLog = new ErrorListViewModel.ErrorList();
            //var result = compiler.CompileProject(packFile, ref compileResultLog);
            //
            //if (compileResultLog.Errors.Count == 0 && result != null)
            //{
            //    var project = compiler.ProjectFile;
            //
            //    // Save to disk for easy debug
            //    var bnkFile = result.OutputBnkFile;
            //    var chunk = bnkFile.DataSource.ReadDataAsChunk();
            //    outputFile = $"Data\\{project.OutputFile}".ToLower().Trim();
            //    chunk.SaveToFile(outputFile);
            //
            //    return true;
            //}

            throw new System.Exception("Something went wrong");
        }
    }
}
