using System;
using System.IO;

namespace CommonControls.Common
{
    public class DirectoryHelper
    {
        public static string UserDirectory { get { return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile); } }
        public static string ApplicationDirectory { get { return UserDirectory + "\\AssetEditor"; } }
        public static string SchemaDirectory { get { return ApplicationDirectory + "\\Schemas"; } }
        public static string LogDirectory { get { return ApplicationDirectory + "\\Logs"; } }
        public static string AnimationIndexMappingDirectory { get { return ApplicationDirectory + "\\Animation\\BoneIndexMapping"; } }

        public static void EnsureCreated()
        {
            EnsureCreated(ApplicationDirectory);
            EnsureCreated(SchemaDirectory);
            EnsureCreated(LogDirectory);
            EnsureCreated(AnimationIndexMappingDirectory);
        }

        public static void EnsureCreated(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
    }
}
