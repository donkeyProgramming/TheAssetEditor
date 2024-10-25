namespace E2EVerification.Shared
{
    public static class PathHelper
    {
        public static string FileInDataFolder(string fileName)
        {
            var fullPath = Path.GetFullPath(@"..\..\..\..\..\Data\" + fileName);
            if (File.Exists(fullPath) == false)
                throw new Exception($"Unable to find data file {fileName}_");
            return fullPath;
        }

    }
}
