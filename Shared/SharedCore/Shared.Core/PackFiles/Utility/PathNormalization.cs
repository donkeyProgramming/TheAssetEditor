namespace Shared.Core.PackFiles.Utility
{
    public static class PathNormalization
    {
        public static string NormalizeFileName(string path)
        {
            return path.Replace('/', '\\').ToLower().Trim();
        }
    }
}
