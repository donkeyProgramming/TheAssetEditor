using Shared.Core.PackFiles.Models.Containers;

namespace Shared.CoreTest.PackFiles.Models.Containers
{
    [TestFixture(typeof(CachedPackFileContainer))]
    [TestFixture(typeof(PackFileContainer))]
    [TestFixture(typeof(SystemFolderContainer))]
    internal class PackFileContainerTests_GetAllFilesByFolder : PackFileContainerTests_TestBase
    {
        public PackFileContainerTests_GetAllFilesByFolder(Type containerType) : base(containerType) { }

        [Test]
        public void GetAllFilesByFolder_ReturnsExpectedFolderCount()
        {
            var result = _container.GetAllFilesByFolder();

            // Folders: folder, other, audio, (root), models, models\textures, models\textures\specular, scripts, folder_a, folder_b, compressed
            Assert.That(result.Count, Is.GreaterThanOrEqualTo(10));
        }

        [Test]
        public void GetAllFilesByFolder_KeysAreSorted()
        {
            var result = _container.GetAllFilesByFolder();
            var keys = result.Keys.ToList();

            for (var i = 1; i < keys.Count; i++)
            {
                Assert.That(string.Compare(keys[i - 1], keys[i], StringComparison.InvariantCultureIgnoreCase), Is.LessThan(0),
                    $"Keys not sorted: '{keys[i - 1]}' should come before '{keys[i]}'");
            }
        }

        [Test]
        public void GetAllFilesByFolder_ContainsExpectedFolders()
        {
            var result = _container.GetAllFilesByFolder();

            Assert.That(result.ContainsKey("folder"), Is.True);
            Assert.That(result.ContainsKey("other"), Is.True);
            Assert.That(result.ContainsKey("audio"), Is.True);
            Assert.That(result.ContainsKey("models"), Is.True);
            Assert.That(result.ContainsKey(@"models\textures"), Is.True);
            Assert.That(result.ContainsKey(@"models\textures\specular"), Is.True);
            Assert.That(result.ContainsKey("scripts"), Is.True);
            Assert.That(result.ContainsKey("folder_a"), Is.True);
            Assert.That(result.ContainsKey("folder_b"), Is.True);
            Assert.That(result.ContainsKey("compressed"), Is.True);
        }

        [Test]
        public void GetAllFilesByFolder_FolderContainsCorrectFiles()
        {
            var result = _container.GetAllFilesByFolder();

            var audioFiles = result["audio"];
            Assert.That(audioFiles, Does.Contain("sound.wem"));
            Assert.That(audioFiles, Does.Contain("music.wem"));
            Assert.That(audioFiles, Does.Contain("battle_sound.wem"));
            Assert.That(audioFiles, Does.Contain("voice.wem_temp"));
            Assert.That(audioFiles, Does.Contain("voice.wem.{sdf}"));
            Assert.That(audioFiles, Does.Contain("voice.txt"));
            Assert.That(audioFiles.Count, Is.EqualTo(6));
        }

        [Test]
        public void GetAllFilesByFolder_ModelsFolder_ContainsCorrectFiles()
        {
            var result = _container.GetAllFilesByFolder();

            var modelsFiles = result["models"];
            Assert.That(modelsFiles, Does.Contain("unit.model"));
            Assert.That(modelsFiles, Does.Contain("vehicle.model"));
            Assert.That(modelsFiles.Count, Is.EqualTo(2));
        }

        [Test]
        public void GetAllFilesByFolder_NestedFolder_ContainsCorrectFiles()
        {
            var result = _container.GetAllFilesByFolder();

            var textureFiles = result[@"models\textures"];
            Assert.That(textureFiles, Does.Contain("diffuse.dds"));
            Assert.That(textureFiles, Does.Contain("normal.dds"));
            Assert.That(textureFiles.Count, Is.EqualTo(2));
        }

        [Test]
        public void GetAllFilesByFolder_DeeplyNestedFolder_ContainsCorrectFiles()
        {
            var result = _container.GetAllFilesByFolder();

            var specularFiles = result[@"models\textures\specular"];
            Assert.That(specularFiles, Does.Contain("gloss.dds"));
            Assert.That(specularFiles.Count, Is.EqualTo(1));
        }

        [Test]
        public void GetAllFilesByFolder_SingleFileFolder_ContainsCorrectFile()
        {
            var result = _container.GetAllFilesByFolder();

            var folderFiles = result["folder"];
            Assert.That(folderFiles, Does.Contain("file.txt"));
            Assert.That(folderFiles.Count, Is.EqualTo(1));
        }

        [Test]
        public void GetAllFilesByFolder_FilesAreNotDuplicatedAcrossFolders()
        {
            var result = _container.GetAllFilesByFolder();

            // shared.txt exists in both folder_a and folder_b
            var folderAFiles = result["folder_a"];
            var folderBFiles = result["folder_b"];
            Assert.That(folderAFiles, Does.Contain("shared.txt"));
            Assert.That(folderBFiles, Does.Contain("shared.txt"));
            Assert.That(folderAFiles.Count, Is.EqualTo(1));
            Assert.That(folderBFiles.Count, Is.EqualTo(1));
        }

        [Test]
        public void GetAllFilesByFolder_TotalFileCountMatchesGetAllFiles()
        {
            var result = _container.GetAllFilesByFolder();
            var totalFiles = result.Values.Sum(files => files.Count);
            var allFiles = _container.GetAllFiles();

            Assert.That(totalFiles, Is.EqualTo(allFiles.Count));
        }

        [Test]
        public void GetAllFilesByFolder_FilesWithinEachFolderAreSorted()
        {
            var result = _container.GetAllFilesByFolder();

            foreach (var (folder, files) in result)
            {
                for (var i = 1; i < files.Count; i++)
                {
                    Assert.That(StringComparer.Ordinal.Compare(files[i - 1], files[i]), Is.LessThan(0),
                        $"Files not sorted in folder '{folder}': '{files[i - 1]}' should come before '{files[i]}'");
                }
            }
        }

        [Test]
        public void GetAllFilesByFolder_AudioFolder_FilesAreSortedByOrdinal()
        {
            var result = _container.GetAllFilesByFolder();
            var audioFiles = result["audio"];

            // Ordinal sort: battle_sound.wem, music.wem, sound.wem, voice.txt, voice.wem.{sdf}, voice.wem_temp
            var expected = new[] { "battle_sound.wem", "music.wem", "sound.wem", "voice.txt", "voice.wem.{sdf}", "voice.wem_temp" };
            Assert.That(audioFiles, Is.EqualTo(expected));
        }
    }
}
