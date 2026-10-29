using System.IO;
using System.Linq;
using System.Windows;
using Shared.Core.ErrorHandling.Exceptions;
using Shared.Core.Services;
using SharpGLTF.Schema2;

namespace Editors.ImportExport.Exporting.Exporters.RmvToGltf.Helpers
{

    public interface IGltfSceneSaver
    {
        public void Save(ModelRoot modelRoot, string fullSystemPath);

        /// <summary>
        /// Saves a scene and, for binary glTF output, removes the exact
        /// generated texture intermediates supplied by the exporter after the
        /// save succeeds. The default implementation preserves compatibility
        /// with existing saver implementations.
        /// </summary>
        public void Save(
            ModelRoot modelRoot,
            string fullSystemPath,
            IReadOnlyCollection<string> generatedTexturePaths)
            => Save(modelRoot, fullSystemPath);
    }

    public class GltfSceneSaver : IGltfSceneSaver
    {
        private readonly IStandardDialogs _exceptionService;

        public GltfSceneSaver(IStandardDialogs exceptionService)
        {
            _exceptionService = exceptionService;
        }

        public void Save(ModelRoot modelRoot, string fullSystemPath)
            => Save(modelRoot, fullSystemPath, Array.Empty<string>());

        public void Save(
            ModelRoot modelRoot,
            string fullSystemPath,
            IReadOnlyCollection<string> generatedTexturePaths)
        {
            try
            {
                // SharpGLTF selects the container from the requested extension:
                // .gltf produces the JSON/sidecar form and .glb is embedded.
                modelRoot.Save(fullSystemPath);

                if (string.Equals(Path.GetExtension(fullSystemPath), ".glb", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var texturePath in generatedTexturePaths
                        .Where(x => string.Equals(Path.GetExtension(x), ".png", StringComparison.OrdinalIgnoreCase))
                        .Distinct(StringComparer.OrdinalIgnoreCase))
                    {
                        // These are exact paths returned by the texture
                        // handler. Never infer or wildcard-delete filenames.
                        if (File.Exists(texturePath))
                            File.Delete(texturePath);
                    }
                }
            }
            catch (Exception ex)
            {
                _exceptionService.ShowExceptionWindow(ex);
            }
        }
    }
}
