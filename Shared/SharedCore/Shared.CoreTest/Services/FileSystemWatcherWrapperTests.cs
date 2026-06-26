using Shared.Core.PackFiles.Utility;
using Shared.Core.Services;

namespace Shared.CoreTest.Services
{
    [TestFixture]
    internal class FileSystemWatcherWrapperTests
    {
        [Test]
        public void Dispose_DoesNotThrow()
        {
            var wrapper = new FileSystemWatcherWrapper();
            Assert.DoesNotThrow(() => wrapper.Dispose());
        }

        [Test]
        public void SetPath_PropagatesCorrectly()
        {
            using var wrapper = new FileSystemWatcherWrapper();
            var tempDir = Path.GetTempPath();

            wrapper.Path = tempDir;

            Assert.That(wrapper.Path, Is.EqualTo(tempDir));
        }

        [Test]
        public void EnableRaisingEvents_DefaultFalse()
        {
            using var wrapper = new FileSystemWatcherWrapper();
            wrapper.Path = Path.GetTempPath();

            Assert.That(wrapper.EnableRaisingEvents, Is.False);
        }

        [Test]
        public void IncludeSubdirectories_DefaultFalse()
        {
            using var wrapper = new FileSystemWatcherWrapper();
            wrapper.Path = Path.GetTempPath();

            Assert.That(wrapper.IncludeSubdirectories, Is.False);
        }

        [Test]
        public void IncludeSubdirectories_SetTrue_PropagatesCorrectly()
        {
            using var wrapper = new FileSystemWatcherWrapper();
            wrapper.Path = Path.GetTempPath();

            wrapper.IncludeSubdirectories = true;

            Assert.That(wrapper.IncludeSubdirectories, Is.True);
        }
    }
}
