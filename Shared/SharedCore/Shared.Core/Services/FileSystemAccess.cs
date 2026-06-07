using System.Diagnostics;
using System.IO;
using Serilog;
using Shared.Core.ErrorHandling;

namespace Shared.Core.Services
{
    public class FileSystemAccess : IFileSystemAccess
    {
        private readonly ILogger _logger = Logging.Create<FileSystemAccess>();

        public string? PathGetDirectoryName(string path) => Path.GetDirectoryName(path);
        public string PathGetFileName(string path) => Path.GetFileName(path);

        public void FileWriteAllBytes(string path, byte[] bytes)
        {
            File.WriteAllBytes(path, bytes);
            _logger.Here().Information($"Wrote {bytes.Length} byte(s) to '{path}'");
        }

        public byte[] FileReadAllBytes(string path)
        {
            var bytes = File.ReadAllBytes(path);
            _logger.Here().Information($"Read {bytes.Length} byte(s) from '{path}'");
            return bytes;
        }

        public bool FileExists(string path) => File.Exists(path);

        public void FileDelete(string path)
        {
            File.Delete(path);
            _logger.Here().Information($"Deleted file '{path}'");
        }

        public void FileMove(string sourceFileName, string destFileName)
        {
            File.Move(sourceFileName, destFileName, overwrite: true);
            _logger.Here().Information($"Moved file '{sourceFileName}' to '{destFileName}'");
        }

        public bool DirectoryExists(string path) => Directory.Exists(path);

        public void DirectoryCreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
            _logger.Here().Information($"Created directory '{path}'");
        }

        public void DirectoryDelete(string path, bool recursive)
        {
            Directory.Delete(path, recursive);
            _logger.Here().Information($"Deleted directory '{path}' (recursive={recursive})");
        }

        public void DirectoryMove(string sourceDirName, string destDirName)
        {
            Directory.Move(sourceDirName, destDirName);
            _logger.Here().Information($"Moved directory '{sourceDirName}' to '{destDirName}'");
        }

        public string[] DirectoryGetFiles(string path, string searchPattern, SearchOption searchOption)
        {
            var files = Directory.GetFiles(path, searchPattern, searchOption);
            _logger.Here().Information($"Enumerated {files.Length} file(s) in '{path}' with pattern '{searchPattern}' and option '{searchOption}'");
            return files;
        }

        public DirectoryInfo CreateDirectoryInfo(string path)
        {
            _logger.Here().Information($"Creating DirectoryInfo for '{path}'");
            return new(path);
        }

        public Process? ProcessStart(string fileName, string arguments)
        {
            _logger.Here().Information($"Starting process '{fileName}' with arguments '{arguments}'");
            var process = Process.Start(fileName, arguments);
            if (process == null)
                _logger.Here().Warning($"Process start returned null for '{fileName}'");
            return process;
        }

        public Process? ProcessStart(ProcessStartInfo startInfo)
        {
            _logger.Here().Information($"Starting process '{startInfo.FileName}' with arguments '{startInfo.Arguments}'");
            var process = Process.Start(startInfo);
            if (process == null)
                _logger.Here().Warning($"Process start returned null for '{startInfo.FileName}'");
            return process;
        }
    }
}
