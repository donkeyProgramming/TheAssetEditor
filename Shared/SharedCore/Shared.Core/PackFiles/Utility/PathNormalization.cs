namespace Shared.Core.PackFiles.Utility
{
    public static class PathNormalization
    {
        public static string NormalizeFileName(string path)
        {
            return path.Replace('/', '\\').ToLower().Trim();
        }

        public static string NormalizeDirectoryPath(string? directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                return string.Empty;

            return directoryPath.Replace('/', '\\').Trim().Trim('\\');
        }
    }
}
