using Editors.ImportExport.Exporting.Exporters.RmvToGltf.Helpers;
using SharpGLTF.Schema2;

namespace Test.ImportExport.Exporting.Exporters.RmvToGlft
{
    public class TestGltfSceneSaver : IGltfSceneSaver
    {
        public void Save(ModelRoot modelRoot, string fullSystemPath)
        {
            IsSaveCalled = true;
            FullSystemPath = fullSystemPath;
            ModelRoot = modelRoot;
        }

        public bool IsSaveCalled { get; set; }
        public string? FullSystemPath { get; set; }
        public ModelRoot? ModelRoot { get; set; }
    }
}
