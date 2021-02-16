using FileTypes.PackFiles.Models;
using FileTypes.PackFiles.Services;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FileTypesTests.PackFiles
{
    public class PackFileServiceTests_Load
    {
        [SetUp]
        public void Setup()
        {
            
        }

        [Test]
        public void LoadCaPackFile()
        {
            var packFileDb = new PackFileDataBase();
            PackFileService packFileService = new PackFileService(packFileDb);
            var result = packFileService.Load(@"Data\CaPackFile_01.pack");

            Assert.NotNull(result);
            Assert.AreEqual(1, packFileDb.PackFiles.Count);
            var fileCount = packFileDb.PackFiles[0].FileList.Count;
            Assert.AreEqual(4, fileCount);
        }

        [Test]
        public void LoadAllCaPacks()
        {
            PackFileService packFileService = new PackFileService(new PackFileDataBase());
            var result = packFileService.LoadAllCaFiles(@"Data");
            Assert.IsTrue(result);
        }

        [Test]
        public void UnloadPackFile()
        {
            PackFileService packFileService = new PackFileService(new PackFileDataBase());
            var pack0 = packFileService.Load(@"Data\CaPackFile_01.pack");
            var pack1 = packFileService.Load(@"Data\boot.pack");

            Assert.NotNull(pack0);
            Assert.NotNull(pack1);

            Assert.AreEqual(2, packFileService.Database.PackFiles.Count);
            packFileService.UnloadPackContainer(pack0);
            Assert.AreEqual(1, packFileService.Database.PackFiles.Count);
        }

        [Test]
        public void SaveNewPackFile()
        {
            PackFileService packFileService = new PackFileService(new PackFileDataBase());
           // packFileService.NewPackFile("Test", );
        }


        [Test]
        public void LoadBootPack()
        {
            PackFileService packFileService = new PackFileService(new PackFileDataBase());
            var pack0 = packFileService.Load(@"Data\boot.pack");


            var file = packFileService.FindFile(@"fx\cardcaps.txt");
            PackFile concretFile = file as PackFile;
            var s = Encoding.UTF8.GetString(concretFile.DataSource.ReadData());


            Assert.NotNull(pack0);
        }


        [Test]
        public void SavePackFile()
        {
            //var origialBytes = File.ReadAllBytes(@"Data\warStuff.pack");
            //
            //PackFileService packFileService = new PackFileService(new PackFileDataBase());
            //var packFile = packFileService.NewPackFile("CustomPackFile", PackFileCAType.MOD);
            //packFile.AddFile
            //
            //
            //var pack0 = packFileService.Load(@"Data\warStuff.pack");
            //
            //var saveResult = pack0.SaveToByteArray();
        }
    }
}
