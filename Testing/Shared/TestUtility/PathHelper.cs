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

            var index = currentDirectory.LastIndexOf(rootDir, StringComparison.InvariantCultureIgnoreCase);
            var rootPath = currentDirectory.Substring(0, index) + rootDir;
            var fullPath = Path.Combine(rootPath, folder).ToLower();

            if (Directory.Exists(fullPath) == false)
                throw new Exception($"Unable to find data directory {fullPath}. TestFolder : {currentDirectory}. InputFolder: {folder}");

            return fullPath;
        }

        public static string GetDataFile(string fileName, string rootDir = "TheAssetEditor", string subDir = "Data")
        {
            var currentDirectory = TestContext.CurrentContext.TestDirectory;
            if (string.IsNullOrEmpty(currentDirectory))
                return "";

            while (true)
            {
                var fileNameOnly = Path.GetFileName(currentDirectory); // get last foldername
                if (string.IsNullOrEmpty(fileNameOnly))
                    return "";

                if (fileNameOnly.ToLower() == rootDir.ToLower())
                    break;

                currentDirectory = Path.GetDirectoryName(currentDirectory); // go one folder UP
                if (string.IsNullOrEmpty(currentDirectory))  // reached root, nothing foun              
                    return "";
            }

            var fullPath = currentDirectory + $@"\{subDir}\" + fileName;

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

    }
}
