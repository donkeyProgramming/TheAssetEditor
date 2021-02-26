using Common;
using View3D.SceneNodes;

namespace KitbasherEditor.ViewModels.SceneExplorerNodeViews
{
    public class ModelSceneNodeViewModel : NotifyPropertyChangedImpl, ISceneNodeViewModel
    {
        string _name;
        public string TestName { get { return _name; } set { SetAndNotify(ref _name, value); } }

        public ModelSceneNodeViewModel(Rmv2ModelNode node)
        {
            TestName = node.Name;
        }
    }
}
