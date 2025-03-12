using Editors.Audio.AudioEditor.AudioProjectExplorer;

namespace Editors.Audio.AudioEditor.Events
{
    public class NodeSelectedEvent
    {
        public TreeNode SelectedNode { get; }

        public NodeSelectedEvent(TreeNode selectedNode)
        {
            SelectedNode = selectedNode;
        }
    }
}
