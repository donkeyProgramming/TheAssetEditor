using View3D.SceneNodes;

namespace KitbasherEditor.Services
{
    public class ModelEditorService
    {
        Rmv2ModelNode _model;

        public ModelEditorService(Rmv2ModelNode model)
        {
            _model = model;
        }

        public void SetSkeletonName(string skeletonName)
        {
            var header = _model.Model.Header;
            header.SkeletonName = skeletonName;
            _model.Model.Header = header;
        }
    }
}
