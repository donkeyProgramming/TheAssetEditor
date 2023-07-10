using NUnit.Framework;

namespace FileTypesTests.PackFiles
{
    class PackFileServiceTests_Save
    {
        [Test]
        public void Basic()
        {
            // PackFileService packFileService = new PackFileService(new PackFileDataBase(), null);
            // var loadedPackFile = packFileService.Load(@"Data\CaPackFile_01.pack");
            //
            // Assert.NotNull(loadedPackFile);
            // var fileCount = loadedPackFile.FileList.Count;
            // Assert.AreEqual(4, fileCount);
            //
            // using (MemoryStream ms = new MemoryStream())
            // {
            //     using (BinaryWriter writer = new BinaryWriter(ms))
            //         packFileService.Save(loadedPackFile, writer);
            //
            //     var newBytes = ms.ToArray();
            //     var orgData = File.ReadAllBytes(@"Data\CaPackFile_01.pack");
            //     for (int i = 0; i < newBytes.Length; i++)
            //         Assert.AreEqual(orgData[i], newBytes[i], $"Difference at index {i}");
            // }
        }

        [Test]
        public void CreateModPackFile()
        {
            //PackFileService packFileService = new PackFileService(new PackFileDataBase(), null);
            //var packContainer = packFileService.CreateNewPackFileContainer("MyTestPackFile", PackFileCAType.MOD);
            //
            //
            //var packFile = new PackFile("ExampleFile.txt", new FileSystemSource(@"Data\FolderData\SubFolder0\Subfolder_0_file0.txt"));
            //packFileService.AddFileToPack(packContainer, @"data\content\files", packFile);
            //
            ////var packFileContent = Encoding.UTF8.GetString(packFile.DataSource.ReadData());
            //
            //using (MemoryStream ms0 = new MemoryStream())
            //{
            //    using (BinaryWriter writer = new BinaryWriter(ms0))
            //        packFileService.Save(packContainer, writer);
            //
            //    // Load it again
            //    var orgData = ms0.ToArray();
            //    using (MemoryStream ms1 = new MemoryStream(orgData))
            //    {
            //        Assert.AreEqual(ms1.Length, orgData.Length);
            //
            //        using (BinaryReader reader = new BinaryReader(ms1))
            //        {
            //            var loadedPackFile = new PackFileContainer("File", reader);
            //
            //            Assert.AreEqual(packContainer.Header.Version, loadedPackFile.Header.Version);
            //            Assert.AreEqual(packContainer.Header.FileType, loadedPackFile.Header.FileType);
            //            Assert.AreEqual(1, loadedPackFile.Header.FileCount);
            //            Assert.AreEqual(0, loadedPackFile.Header.ReferenceFileCount);
            //        }
            //    }
            //}
        }
    }
}
