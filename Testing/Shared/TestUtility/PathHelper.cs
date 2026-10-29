using System.Text;

namespace Test.TestingUtility.TestUtility
{
    public static class PathHelper
    {
        /// <summary>
        /// Find the "AssetEditor" folder from the test directory and return the path to the file
        /// Probably superior to the hardcoded path in the original code
        /// </summary>        
        public static string GetDataFolder(string folder, string rootDir = "TheAssetEditor")
        {
            var currentDirectory = TestContext.CurrentContext.TestDirectory;

            var rootPath = FindRepositoryRoot(currentDirectory, rootDir);
            if (rootPath == null)
                throw new Exception($"Unable to find repository root '{rootDir}' or AssetEditor.sln from test directory {currentDirectory}");

            var fullPath = CombineRelativePath(rootPath, folder);

            if (Directory.Exists(fullPath) == false)
                throw new Exception($"Unable to find data directory {fullPath}. TestFolder : {currentDirectory}. InputFolder: {folder}");

            return fullPath;
        }

        public static string GetDataFile(string fileName, string rootDir = "TheAssetEditor", string subDir = "Data")
        {
            var currentDirectory = TestContext.CurrentContext.TestDirectory;
            if (string.IsNullOrEmpty(currentDirectory))
                return "";

            var rootPath = FindRepositoryRoot(currentDirectory, rootDir);
            if (rootPath == null)
                return "";

            var fullPath = CombineRelativePath(rootPath, subDir, fileName);

            if (File.Exists(fullPath) == false)
                throw new Exception($"Unable to find data file {fileName}");

            return fullPath;
        }

        public static byte[] GetFileAsBytes(string path)
        {
            var fullPath = GetDataFile(path);
            var bytes = File.ReadAllBytes(fullPath);
            return bytes; ;
        }

        public static string GetFileContentAsString(string path)
        {
            var bytes = GetFileAsBytes(path);
            return Encoding.UTF8.GetString(bytes);
        }

        private static string? FindRepositoryRoot(string currentDirectory, string rootDir)
        {
            if (string.IsNullOrWhiteSpace(currentDirectory))
                return null;

            var directory = new DirectoryInfo(currentDirectory);
            while (directory != null)
            {
                // Keep honoring the historical rootDir argument, but prefer whichever
                // valid repository marker is nearest to the test output directory.
                if (!string.IsNullOrWhiteSpace(rootDir) &&
                    string.Equals(directory.Name, rootDir, StringComparison.OrdinalIgnoreCase))
                {
                    return directory.FullName;
                }

                if (File.Exists(Path.Combine(directory.FullName, "AssetEditor.sln")))
                    return directory.FullName;

                directory = directory.Parent;
            }

            return null;
        }

        private static string CombineRelativePath(string rootPath, params string[] parts)
        {
            var path = rootPath;
            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part))
                    continue;

                // Test data callers historically pass Windows-style paths even when
                // tests run on Linux. Normalize both separators before combining.
                var normalizedPart = part
                    .Replace('\\', Path.DirectorySeparatorChar)
                    .Replace('/', Path.DirectorySeparatorChar);
                path = Path.Combine(path, normalizedPart);
            }

            return path;
        }

    }
}
