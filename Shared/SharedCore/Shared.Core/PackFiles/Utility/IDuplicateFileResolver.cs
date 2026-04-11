using System.Windows;

namespace Shared.Core.PackFiles.Utility
{
    public interface IDuplicateFileResolver
    {
        bool CheckForDuplicates { get; }
        bool KeepDuplicateFile(string fileName);
    }

    public class CaPackDuplicateFileResolver : IDuplicateFileResolver
    {
        public bool CheckForDuplicates => false;
        public bool KeepDuplicateFile(string fileName) => false;
    }

    public class CustomPackDuplicateFileResolver : IDuplicateFileResolver
    {
        public bool CheckForDuplicates => true;
        public bool KeepDuplicateFile(string fileName)
        {
            var res = MessageBox.Show($"Multiple files with the name '{fileName}' found.\n Yes = Rename and keep.\nNo = Skip", "DuplicateFile", MessageBoxButton.YesNo);
            return res == MessageBoxResult.Yes;
        }
    }
}
