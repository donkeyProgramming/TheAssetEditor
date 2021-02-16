using FileTypes.PackFiles.Models;
using FileTypes.PackFiles.Services;
using FileTypesTests.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileTypesTests.PackFiles
{
    class PackFileServiceTests_Add
    {
        [Test]
        public void Basic()
        {
            PackFileService packFileService = new PackFileService(new PackFileDataBase());
            var loadedPackFile = packFileService.Load(@"Data\CaPackFile_01.pack");

            Assert.NotNull(loadedPackFile);
            var fileCount = loadedPackFile.FileList.Count;
            Assert.AreEqual(4, fileCount);
        }

        [Test]
        public void AddFileToRoot()
        {
            PackFileService packFileService = new PackFileService(new PackFileDataBase());
            var loadedPackFile = packFileService.Load(@"Data\CaPackFile_01.pack");
            packFileService.AddFileToPack(loadedPackFile, "", TestPackFileHelper.CreatePackFile("TestFile.txt"));

            var newFileCount = loadedPackFile.FileList.Count;
            Assert.AreEqual(5, newFileCount);

            var file = packFileService.FindFile("TestFile.txt");
            Assert.NotNull(file);
        }

        [Test]
        public void AddFileToFolder()
        {
            PackFileService packFileService = new PackFileService(new PackFileDataBase());
            var loadedPackFile = packFileService.Load(@"Data\CaPackFile_01.pack");
            packFileService.AddFileToPack(loadedPackFile, @"warmachines\materials", TestPackFileHelper.CreatePackFile("TestFile.txt"));

            var newFileCount = loadedPackFile.FileList.Count;
            Assert.AreEqual(5, newFileCount);

            var file = packFileService.FindFile(@"warmachines\materials\TestFile.txt");
            Assert.NotNull(file);
        }

        [Test]
        public void AddFolderToRoot()
        {
            var packFileDb = new PackFileDataBase();
            PackFileService packFileService = new PackFileService(packFileDb);
            var loadedPackFile = packFileService.Load(@"Data\CaPackFile_01.pack");

            packFileService.AddFolderContent(loadedPackFile, "", @"Data\FolderData");

            var newFileCount = loadedPackFile.FileList.Count;
            Assert.AreEqual(9, newFileCount);

            var file = packFileService.FindFile(@"SubFolder1\Subfolder_1_file1.txt");
            Assert.NotNull(file);
        }

        [Test]
        public void AddFolderToChild()
        {

            var packFileDb = new PackFileDataBase();
            PackFileService packFileService = new PackFileService(packFileDb);
            var loadedPackFile = packFileService.Load(@"Data\CaPackFile_01.pack");

            packFileService.AddFolderContent(loadedPackFile, @"warmachines\materials", @"Data\FolderData");

            var newFileCount = loadedPackFile.FileList.Count;
            Assert.AreEqual(9, newFileCount);

            var file = packFileService.FindFile(@"warmachines\materials\subFolder1\Subfolder_1_file1.txt");
            Assert.NotNull(file);
        }
    }
}
