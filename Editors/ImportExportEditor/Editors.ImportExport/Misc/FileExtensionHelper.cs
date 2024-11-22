
namespace Editors.ImportExport.Misc
{
    public static class FileExtensionHelper
    {
        public static bool IsDdsFile(string fileName)
        {
            var isDdsFile = fileName.Contains(".dds", StringComparison.InvariantCultureIgnoreCase);
            return isDdsFile;
        }

        public static bool IsDdsMaterialFile(string fileName)
        {
            var isDdsFile = IsDdsFile(fileName);
            var isMaterialFile = fileName.Contains("material", StringComparison.InvariantCultureIgnoreCase);
            return isDdsFile && isMaterialFile;
        }

        public static bool IsRmvFile(string fileName)
        {
            var isRmv = fileName.Contains(".rigid_model_v2", StringComparison.InvariantCultureIgnoreCase);
            return isRmv;
        }

        public static bool IsWsModelFile(string fileName)
        {
            var isRmv = fileName.Contains(".wsmodel", StringComparison.InvariantCultureIgnoreCase);
            return isRmv;
        }
    }
}
