using System.Windows;

namespace Shared.Core.PackFiles.Models
{
    public interface IDuplicatePackFileResolver
    {
        bool CheckForDuplicates { get; }
        bool KeepDuplicateFile(string fileName);
    }

    public class CaPackDuplicatePackFileResolver : IDuplicatePackFileResolver
    {
        public bool CheckForDuplicates => false;
        public bool KeepDuplicateFile(string fileName) => false;
    }

    public class CustomPackDuplicatePackFileResolver : IDuplicatePackFileResolver
    {
        public bool CheckForDuplicates => true;
        public bool KeepDuplicateFile(string fileName)
        {
            var res = MessageBox.Show($"Multiple files with the name '{fileName}' found.\n Yes = Rename and keep.\nNo = Skip", "DuplicateFile", MessageBoxButton.YesNo);
            return res == MessageBoxResult.Yes;
        }
    }
}
