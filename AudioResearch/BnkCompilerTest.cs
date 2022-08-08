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
                var editPack = pfs.GetEditablePack();
                var bnkPackFile = editPack.FileList.First().Value;

                // Save to disk
                var chunk = bnkPackFile.DataSource.ReadDataAsChunk();
                var outputName = projectFilePath.Replace(".bnk.xml", ".bnk");
                chunk.SaveToFile($"Data\\{outputName}");

                return true;
            }

            return false;
        }
    }
}
