using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.Core.ErrorHandling.Exceptions;
using SharpGLTF.Schema2;
using System.Windows;
using Shared.Core.Services;

namespace Editors.ImportExport.Importing.Importers.GltfToRmv.Helper
{
    public interface IGltfSceneLoader
    {
        public ModelRoot? Load(GltfImporterSettings settings);
    }

    public class GltfSceneLoader : IGltfSceneLoader
    {
        private readonly IStandardDialogs _exceptionService;

        public GltfSceneLoader(IStandardDialogs exceptionService)
        {
            _exceptionService = exceptionService;
        }

        public ModelRoot? Load(GltfImporterSettings settings)
        {
            ModelRoot? modelRoot = null;
            try
            {
                modelRoot = ModelRoot.Load(settings.InputGltfFile);
            }
            catch (Exception ex)
            {
                _exceptionService.ShowExceptionWindow(ex);
                return null;
            }

            return modelRoot;

        }
    }

}
