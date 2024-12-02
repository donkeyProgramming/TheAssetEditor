using System.Windows;
using Shared.Core.ErrorHandling.Exceptions;
using Shared.Core.Services;
using SharpGLTF.Schema2;

namespace Editors.ImportExport.Exporting.Exporters.RmvToGltf.Helpers
{

    public interface IGltfSceneSaver
    {
        public void Save(ModelRoot modelRoot, string fullSystemPath);
    }

    public class GltfSceneSaver : IGltfSceneSaver
    {
        private readonly IStandardDialogs _exceptionService;

        public GltfSceneSaver(IStandardDialogs exceptionService)
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
                _exceptionService.ShowExceptionWindow(ex);
            }
        }
    }
}
