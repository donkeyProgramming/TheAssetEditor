using System.Diagnostics;
using Shared.Core.Misc;

namespace Editors.Audio.Utility
{
    public static class BnkToXmlConverter
    {
        public static void Convert(string wwiserPath, string bnkSystemFilePath, bool openFolder = false)
        {
            var command = $"{wwiserPath} {bnkSystemFilePath}";
            ExecuteCommand(command);
            if (openFolder)
                DirectoryHelper.OpenFolderAndSelectFile(bnkSystemFilePath);
        }

        public static void ExecuteCommand(string command)
        {
            var processInfo = new ProcessStartInfo("cmd.exe", "/K " + command)
            {
                CreateNoWindow = true,
                UseShellExecute = true,
            };
            using var process = Process.Start(processInfo);
        }
    }
}
