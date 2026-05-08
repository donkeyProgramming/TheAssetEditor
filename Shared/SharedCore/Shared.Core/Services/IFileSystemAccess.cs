using System.Diagnostics;

namespace Shared.Core.Services
{
    public interface IFileSystemAccess
    {
        string? PathGetDirectoryName(string path);
        string PathGetFileName(string path);

        void FileWriteAllBytes(string path, byte[] bytes);
        byte[] FileReadAllBytes(string path);
        bool FileExists(string path);

        bool DirectoryExists(string path);
        string[] DirectoryGetFiles(string path, string searchPattern, SearchOption searchOption);
        DirectoryInfo CreateDirectoryInfo(string path);

        Process? ProcessStart(string fileName, string arguments);
        Process? ProcessStart(ProcessStartInfo startInfo);
    }
}
