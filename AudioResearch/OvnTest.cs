using Audio.FileFormats.WWise.Hirc.V136;
using Audio.Storage;
using Audio.Utility;
using CommonControls.Common;
using CommonControls.Editors.AudioEditor.BnkCompiler;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AudioResearch
{
    internal class OvnTest
    {
        public static void Compile()
        {
            using var application = new SimpleApplication();

            var pfs = application.GetService<PackFileService>();
            //pfs.LoadAllCaFiles(GameTypeEnum.Warhammer3);
            pfs.CreateNewPackFileContainer("SoundOutput", PackFileCAType.MOD, true);
            PackFileUtil.LoadFilesFromDisk(pfs, new[]
            {
                new PackFileUtil.FileRef( packFilePath: @"audioprojects", systemPath:@"Data\OvnExample\Project.json")
            });

            // Load all wems
            var wemReferences = Directory.GetFiles(@"D:\Research\Audio\Working pack\audio_ovn\wwise\english(uk)")
                .Where(x => Path.GetExtension(x) == ".wem")
                .Select(x => new PackFileUtil.FileRef(packFilePath: @"audio\wwise", systemPath: x))
                .ToList();
            PackFileUtil.LoadFilesFromDisk(pfs, wemReferences);

            var compiler = application.GetService<Compiler>();
            var compileResult = compiler.CompileProject(@"audioprojects\Project.json", out var errorList);
        }

        public static void GenerateProjectFromBnk()
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

            var projectExporter = new AudioProjectExporter();
            projectExporter.CreateFromRepository(audioRepo, "OvnProject.json");
        }

    }
}
