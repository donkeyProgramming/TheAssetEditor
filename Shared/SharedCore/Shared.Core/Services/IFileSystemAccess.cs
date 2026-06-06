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

        void FileDelete(string path);
        void FileMove(string sourceFileName, string destFileName);

        bool DirectoryExists(string path);
        void DirectoryCreateDirectory(string path);
        void DirectoryDelete(string path, bool recursive);
        void DirectoryMove(string sourceDirName, string destDirName);
        string[] DirectoryGetFiles(string path, string searchPattern, SearchOption searchOption);
        DirectoryInfo CreateDirectoryInfo(string path);

        Process? ProcessStart(string fileName, string arguments);
        Process? ProcessStart(ProcessStartInfo startInfo);
    }
}
