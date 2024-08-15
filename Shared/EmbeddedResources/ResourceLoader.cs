using System.Reflection;
using System.Windows.Media.Imaging;
using Shared.Core.ErrorHandling;

namespace Shared.EmbeddedResources
{
    public static class ResourceLoader
    {
        public static string LoadString(string resourcePath)
        {
            if (resourcePath == null)
            {
                throw new Exception("Resource not found: " + resourcePath);
            }
            using var stream = GetResourceStream(resourcePath);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        public static string[] LoadStringArray(string resourcePath)
        {
            return LoadString(resourcePath)
                .Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        }

        public static BitmapImage LoadBitmapImage(string resourcePath)
        {
            using var stream = GetResourceStream(resourcePath);

            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = stream;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze();

            return bitmapImage;
        }

        public static string GetDevelopmentDataFolder()
        {
            var workingDirectory = Environment.CurrentDirectory;
            var projectDirectory = Directory.GetParent(workingDirectory)?.Parent?.Parent?.Parent?.FullName;
            if (projectDirectory == null)
                throw new Exception($"Unable to find project director from working directory - {workingDirectory}");
            return projectDirectory + "\\Data";
        }

        private static Stream GetResourceStream(string resourcePath)
        {
            var fullPath = "Shared.EmbeddedResources." + resourcePath;
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(fullPath);

            if(stream == null) 
            {
                var logger = Logging.CreateStatic(typeof(ResourceLoader));
                var resourceNames = Assembly.GetExecutingAssembly().GetManifestResourceNames();

                var errorMessage = $"Unable to load embedded resource {fullPath}. Remember the string is case sensitive and does not handle spaces!";
                logger.Here().Error(errorMessage);
                logger.Here().Information($"{resourceNames.Length} Resources loaded");
                foreach (var resourceName in resourceNames)
                    logger.Here().Information($"\t{resourceName}");

                throw new Exception(errorMessage);
            }

            return stream;
        }
    }
}
