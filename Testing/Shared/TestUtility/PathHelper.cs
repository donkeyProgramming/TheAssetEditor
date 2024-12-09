using System.ComponentModel.Design.Serialization;
using System.Text;
using Shared.Core.Services;

namespace Shared.TestUtility
{
    public static class PathHelper
    {
        /// <summary>
        /// Find the "AssetEditor" folder from the test directory and return the path to the file
        /// Probably superior to the hardcoded path in the original code
        /// </summary>        
        public static string FileFromDataFolder(string fileName, string rootDir = "TheAssetEditor", string subDir = "Data")
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

            return fullPath;
        }

        public static byte[] GetFileAsBytes(string path)
        {
            var fullPath = FileFromDataFolder(path);
            var bytes = System.IO.File.ReadAllBytes(fullPath);
            return bytes; ;
        }

        public static string GetFileContentAsString(string path)
        {
            var bytes = GetFileAsBytes(path);
            return Encoding.UTF8.GetString(bytes);
        }

    }
}
