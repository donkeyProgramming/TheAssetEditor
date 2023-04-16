using CommonControls.Common;
using CommonControls.Editors.AudioEditor.BnkCompiler;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;

namespace AudioResearch
{
    class Program
    {
        static void Main(string[] args)
        {
            using var application = new SimpleApplication();

            var pfs = application.GetService<PackFileService>();
            //pfs.LoadAllCaFiles(GameTypeEnum.Warhammer3);
            pfs.CreateNewPackFileContainer("SoundOutput", PackFileCAType.MOD, true);
            PackFileUtil.LoadFilesFromDisk(pfs, new[] 
            { 
                new PackFileUtil.FileRef( packFilePath: @"audio\wwise", systemPath:@"Data\CustomSoundCompile\790209750.wem"),
                new PackFileUtil.FileRef( packFilePath: @"audioprojects", systemPath:@"Data\CustomSoundCompile\Project.json")
            } );

            var compiler = application.GetService<Compiler>();
            var compileResult = compiler.CompileProject(@"audioprojects\Project.json", out var errorList);
        }
    }
}
