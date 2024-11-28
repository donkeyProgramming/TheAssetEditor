using System.Windows;
using Shared.Core.ErrorHandling.Exceptions;
using SharpGLTF.Schema2;

namespace Editors.ImportExport.Exporting.Exporters.RmvToGltf.Helpers
{

    public interface IGltfSceneSaver
    {
        public void Save(ModelRoot modelRoot, string fullSystemPath);
    }

    public class GltfSceneSaver : IGltfSceneSaver
    {
        private readonly IExceptionService _exceptionService;

        public GltfSceneSaver(IExceptionService exceptionService)
        {
            _exceptionService = exceptionService;
        }

        public void Save(ModelRoot modelRoot, string fullSystemPath)
        {        
            try
            {
                modelRoot.SaveGLTF(fullSystemPath);
            }
            catch (Exception ex)
            {
                _exceptionService.CreateDialog(ex);
            }
        }
    }
}
