using Common;
using Filetypes.RigidModel;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.SceneNodes;

namespace KitbasherEditor.Services
{
    public class ModelEditorService
    {
        ILogger _logger = Logging.Create<ModelEditorService>();

        Rmv2ModelNode _model;

        public ModelEditorService(Rmv2ModelNode model)
        {
            _model = model;
        }

        public void SetAttachmentPoints(List<RmvAttachmentPoint> attachmentPoints)
        {
            
        
        }

        public void SetSkeletonName(string skeletonName)
        {
            var header = _model.Model.Header;
            header.SkeletonName = skeletonName;
            _model.Model.Header = header;
        }
    }
}
