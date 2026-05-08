using System.Diagnostics;
using System.IO;

namespace Shared.Core.Services
{
    public class FileSystemAccess : IFileSystemAccess
    {
        public string? PathGetDirectoryName(string path) => Path.GetDirectoryName(path);
        public string PathGetFileName(string path) => Path.GetFileName(path);

        public void FileWriteAllBytes(string path, byte[] bytes) => File.WriteAllBytes(path, bytes);
        public byte[] FileReadAllBytes(string path) => File.ReadAllBytes(path);
        public bool FileExists(string path) => File.Exists(path);

        public bool DirectoryExists(string path) => Directory.Exists(path);
        public string[] DirectoryGetFiles(string path, string searchPattern, SearchOption searchOption) => Directory.GetFiles(path, searchPattern, searchOption);
        public DirectoryInfo CreateDirectoryInfo(string path) => new(path);

        public Process? ProcessStart(string fileName, string arguments) => Process.Start(fileName, arguments);
        public Process? ProcessStart(ProcessStartInfo startInfo) => Process.Start(startInfo);
    }
}
