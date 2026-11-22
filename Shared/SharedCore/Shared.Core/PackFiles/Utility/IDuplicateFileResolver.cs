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

    // For packs opened for editing: keeps the first copy of a duplicated path and records it, rather than
    // offering to rename the duplicate (a GUID-suffixed name that a save would then write into the pack)
    public class RecordingDuplicateFileResolver : IDuplicateFileResolver
    {
        private readonly List<string> _duplicates = [];

        public bool CheckForDuplicates => true;
        public IReadOnlyList<string> Duplicates { get { lock (_duplicates) return _duplicates.ToList(); } }

        public bool KeepDuplicateFile(string fileName)
        {
            lock (_duplicates)
                _duplicates.Add(fileName);
            return false;
        }
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
