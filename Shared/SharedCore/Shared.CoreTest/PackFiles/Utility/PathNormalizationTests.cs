using Shared.Core.PackFiles.Utility;

namespace Shared.CoreTest.PackFiles.Utility
{
    internal class PathNormalizationTests
    {
        [Test]
        public void NormalizeFileName_ReplacesSeparators_TrimsAndLowerCases()
        {
            var result = PathNormalization.NormalizeFileName("  Folder/Sub/File.TXT  ");

            Assert.That(result, Is.EqualTo("folder\\sub\\file.txt"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void NormalizeDirectoryPath_EmptyInput_ReturnsEmptyString(string? input)
        {
            var result = PathNormalization.NormalizeDirectoryPath(input);

            Assert.That(result, Is.EqualTo(string.Empty));
        }

        [Test]
        public void NormalizeDirectoryPath_ReplacesSeparatorsAndTrimsOuterSlashes()
        {
            var result = PathNormalization.NormalizeDirectoryPath("  \\Folder/Sub Dir\\Child\\  ");

            Assert.That(result, Is.EqualTo("Folder\\Sub Dir\\Child"));
        }

        [Test]
        public void NormalizeDirectoryPath_PreservesInnerPathCasing()
        {
            var result = PathNormalization.NormalizeDirectoryPath("Mixed/Case/Folder");

            Assert.That(result, Is.EqualTo("Mixed\\Case\\Folder"));
        }
    }
}