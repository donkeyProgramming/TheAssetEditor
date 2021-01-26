using FileTypes.PackFiles.Models;
using FileTypes.PackFiles.Services;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileTypesTests.PackFiles
{
    public class PackFileServiceTests
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

            Assert.IsTrue(result);
            Assert.AreEqual(1, packFileDb.PackFiles.Count);
            var fileCount = packFileDb.PackFiles[0].FileCount();
            Assert.AreEqual(4, fileCount);
        }

        [Test]
        public void LoadAllCaPacks()
        {
            PackFileService packFileService = new PackFileService(new PackFileDataBase());
            var result = packFileService.LoadAllCaFiles(@"Data");
            Assert.IsTrue(result);
        }
    }
}
