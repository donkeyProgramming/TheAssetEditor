using AssetEditor;
using Audio.BnkCompiler;
using Audio.Storage;
using Audio.Utility;
using CommonControls.Common;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using System.IO;
using System.Linq;

namespace AudioResearch
{

    /*
    * Custom sound in animMeta
    * Custom sound for button/ui
    * Custom sound triggered by script
    * Custom sound for movie
    * Custom sound for diplomacy line
     */

    internal class OvnTest
    {
        public static void Compile(string systemPath, bool useSoundIdFromBnk = true, bool useMixerIdFromBnk = true, bool useActionIdFromBnk = true)
        {
            using var application = new SimpleApplication();

            var pfs = application.GetService<PackFileService>();
            // pfs.LoadAllCaFiles(GameTypeEnum.Warhammer3);
            pfs.CreateNewPackFileContainer("SoundOutput", PackFileCAType.MOD, true);
            PackFileUtil.LoadFilesFromDisk(pfs, new[]
            {
                new PackFileUtil.FileRef( packFilePath: @"audioprojects", systemPath: systemPath)
            });

            // Load all wems
            var wemReferences = Directory.GetFiles(@"D:\Research\Audio\Working pack\audio_ovn\wwise\english(uk)")
                .Where(x => Path.GetExtension(x) == ".wem")
                .Select(x => new PackFileUtil.FileRef(packFilePath: @"audio\wwise", systemPath: x))
                .ToList();
            PackFileUtil.LoadFilesFromDisk(pfs, wemReferences);

            var compiler = application.GetService<CompilerService>();
            var compilerSettings = new CompilerSettings()
            {
                UserOverrideIdForActions = useActionIdFromBnk,
                UseOverrideIdForSounds = useSoundIdFromBnk,
                UseOverrideIdForMixers = useMixerIdFromBnk,
                ConvertResultToXml = true,
                FileExportPath = "D:\\Research\\Audio\\CustomBnks",
            };

            var result = compiler.Compile(@"audioprojects\ProjectSimple.json", compilerSettings);
        }

        public static void GenerateProjectFromBnk(string path)
        {
            using var application = new SimpleApplication();

            var pfs = application.GetService<PackFileService>();
            // pfs.LoadAllCaFiles(GameTypeEnum.Warhammer3);
            pfs.CreateNewPackFileContainer("SoundOutput", PackFileCAType.MOD, true);
            PackFileUtil.LoadFilesFromDisk(pfs, new[]
            {
                new PackFileUtil.FileRef( packFilePath: @"audio\wwise", systemPath:@"D:\Research\Audio\Working pack\audio_ovn\wwise\english(uk)\campaign_diplomacy__ovn.bnk"),
                new PackFileUtil.FileRef( packFilePath: @"audio\wwise", systemPath:@"D:\Research\Audio\Working pack\audio_ovn\wwise\event_data__ovn.dat"),
            });


            var audioRepo = application.GetService<IAudioRepository>();

            var hircs = audioRepo.HircObjects.Select(x => x.Value.First());
            var ids = hircs.Select(x => $"{x.Id}-{x.Type}").ToList();

            var projectExporter = new AudioProjectExporterSimple();
            //projectExporter.CreateFromRepositoryToFile(audioRepo, "campaign_diplomacy__ovn", path);
        }
    }
}
