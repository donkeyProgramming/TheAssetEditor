using System.Windows;
using SharpGLTF.Schema2;

namespace Editors.ImportExport.Exporting.Exporters.RmvToGltf.Helpers
{

    public interface IGltfSceneSaver
    {
        public void Save(ModelRoot modelRoot, string fullSystemPath);
    }

    public class GltfSceneSaver : IGltfSceneSaver
    {
        public void Save(ModelRoot modelRoot, string fullSystemPath)
        {
            try
            {
                modelRoot.SaveGLTF(fullSystemPath);
            }
            catch (Exception e)
            {
                MessageBox.Show($"Error saving GLTF file. SharpGLTF Error:\n{e.Message}");
            }
        }
    }
}
