using System.Text;

namespace Shared.TestUtility
{
    public static class PathHelper
    {
        public static string File(string fileName)
        {
            var fullPath = Path.GetFullPath(@"..\..\..\..\..\Data\" + fileName);
            if (System.IO.File.Exists(fullPath) == false)
                throw new Exception($"Unable to find data file {fileName}");
            return fullPath;
        }

        public static string Folder(string fileName)
        {
            var fullPath = Path.GetFullPath(@"..\..\..\..\..\Data\" + fileName);
            if (Directory.Exists(fullPath) == false)
                throw new Exception($"Unable to find data directory {fullPath}");
            return fullPath;
        }

        public static string Folder2(string fileName)
        {
            var fullPath = Path.GetFullPath(@"..\..\..\..\..\..\Data\" + fileName);
            if (Directory.Exists(fullPath) == false)
                throw new Exception($"Unable to find data directory {fullPath}");
            return fullPath;
        }


        public static byte[] GetFileAsBytes(string path)
        {
            var fullPath = File(path);
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
