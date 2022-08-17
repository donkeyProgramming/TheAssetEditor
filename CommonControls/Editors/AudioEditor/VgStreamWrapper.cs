using CommonControls.Common;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CommonControls.Editors.AudioEditor
{
    public static class VgStreamWrapper
    {
        static readonly ILogger _logger = Logging.CreateStatic(typeof(VgStreamWrapper));

        public static string GetVgStreamFolder() => $"{DirectoryHelper.Temp}\\VgStream";
        public static string GetAudioFolder() => $"{DirectoryHelper.Temp}\\Audio";
        public static void EnsureCreated() => GetCliPath();

        public static bool ExportFile(string fileName, byte[] data, out string outputSoundFilePath, bool keepWem = true)
        {
            outputSoundFilePath = null;
            var cliPath = GetCliPath();
            var wemName = $"{fileName}.wem";
            var wavName = $"{fileName}.wav";

            try
            {
                File.WriteAllBytes(wemName, data);
            }
            catch (Exception e)
            {
                _logger.Here().Error(e.Message);
                return false;
            }

            try
            {
                using var pProcess = new System.Diagnostics.Process();
                pProcess.StartInfo.FileName = cliPath;
                pProcess.StartInfo.Arguments = $"-o \"{wavName}\" \"{wemName}\"";
                pProcess.StartInfo.UseShellExecute = false;
                pProcess.StartInfo.RedirectStandardOutput = true;
                pProcess.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                pProcess.StartInfo.CreateNoWindow = true;
                pProcess.Start();
                var output = pProcess.StandardOutput.ReadToEnd();
                _logger.Here().Information(output);
                pProcess.WaitForExit();

                outputSoundFilePath = wavName;

                if(!keepWem)
                    File.Delete(wemName);

                return true;
            }
            catch (Exception e)
            {
                _logger.Here().Error(e.Message);
                return false;
            }
        }

        static public string GetCliPath()
        {
            DirectoryHelper.EnsureCreated(GetVgStreamFolder());
            DirectoryHelper.EnsureCreated(GetAudioFolder());

            var vgStreamCli = $"{GetVgStreamFolder()}\\test.exe";
            if (File.Exists(vgStreamCli))
                return vgStreamCli;

            var executingAssembly = Assembly.GetExecutingAssembly();
            string folderName = string.Format("{0}.Resources.VgStream.", executingAssembly.GetName().Name);

            var vgStreamFiles = executingAssembly
                .GetManifestResourceNames()
                .Where(r => r.StartsWith(folderName, StringComparison.InvariantCultureIgnoreCase)).ToArray();

            foreach (var file in vgStreamFiles)
            {
                var fileName = file.Replace(folderName, "", StringComparison.InvariantCultureIgnoreCase);
                var outputFileName = $"{GetVgStreamFolder()}\\{fileName}";
                using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(file);
                using var fStream = new FileStream(outputFileName, FileMode.OpenOrCreate);
                stream!.CopyTo(fStream);
            }

            return vgStreamCli;
        }
    }
}
