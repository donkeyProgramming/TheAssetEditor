using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Shared.Core.Misc
{
    public class DirectoryHelper
    {


        private const string ExplorerWindowClass = "CabinetWClass";

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);


        public static string UserDirectory { get { return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile); } }
        public static string ApplicationDirectory { get { return UserDirectory + "\\AssetEditor"; } }
        public static string SchemaDirectory { get { return ApplicationDirectory + "\\Schemas"; } }
        public static string LogDirectory { get { return ApplicationDirectory + "\\Logs"; } }
        public static string ReportsDirectory { get { return ApplicationDirectory + "\\Reports"; } }
        public static string Applications { get { return ApplicationDirectory + "\\Applications"; } }
        public static string Temp { get { return ApplicationDirectory + "\\Temp"; } }
        public static string UpdateDirectory { get { return Temp + "\\Update"; } }
        public static string AnimationIndexMappingDirectory { get { return ApplicationDirectory + "\\Animation\\BoneIndexMapping"; } }

        public static void EnsureCreated()
        {
            EnsureCreated(ApplicationDirectory);
            EnsureCreated(SchemaDirectory);
            EnsureCreated(LogDirectory);
            EnsureCreated(ReportsDirectory);
            EnsureCreated(Applications);
            EnsureCreated(Temp);
            EnsureCreated(AnimationIndexMappingDirectory);
        }

        public static void EnsureCreated(string? path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        public static void EnsureFileFolderCreated(string filePath)
        {
            var folder = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
        }

        public static bool IsFileLocked(string path)
        {
            try
            {
                using (Stream stream = new FileStream(path, FileMode.Open))
                {
                }
            }
            catch (IOException)
            {
                return true;
            }

            return false;
        }

        public static void OpenFolderAndSelectFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            // combine the arguments together
            // it doesn't matter if there is a space after ','
            var argument = "/select, \"" + filePath + "\"";

            Process.Start("explorer.exe", argument);
        }


        public static void OpenOrFocusFolder(string folderPath)
        {
            folderPath = Path.GetFullPath(folderPath);

            if (!Directory.Exists(folderPath))
            {
                Console.WriteLine("Folder does not exist.");
                return;
            }

            IntPtr foundHandle = IntPtr.Zero;

            EnumWindows((hWnd, lParam) =>
            {
                if (!IsWindowVisible(hWnd))
                    return true;

                var className = new StringBuilder(256);
                GetClassName(hWnd, className, className.Capacity);

                if (className.ToString() != ExplorerWindowClass)
                    return true;

                var windowTitle = new StringBuilder(1024);
                GetWindowText(hWnd, windowTitle, windowTitle.Capacity);

                // Explorer title usually ends with folder name
                string folderName = Path.GetFileName(folderPath);

                if (!string.IsNullOrEmpty(folderName) &&
                    windowTitle.ToString().Contains(folderName, StringComparison.OrdinalIgnoreCase))
                {
                    foundHandle = hWnd;
                    return false; // stop enumeration
                }

                return true;
            }, IntPtr.Zero);

            if (foundHandle != IntPtr.Zero)
            {
                SetForegroundWindow(foundHandle);
                Console.WriteLine("Folder already open → brought to front.");
            }
            else
            {
                Process.Start("explorer.exe", folderPath);
                Console.WriteLine("Folder not open → opened now.");
            }
        }

    }
}
