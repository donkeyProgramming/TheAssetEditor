using System.Collections.Generic;
using GameWorld.Core.SceneNodes;

namespace GameWorld.Core.Components.Selection
{
    public enum GeometrySelectionMode
    {
        Object,
        Face,
        Vertex,
        Bone
    };


    public delegate void SelectionStateChanged(ISelectionState state, bool sendEvent);
    public interface ISelectionState
    {
        ISelectionState Clone();
        void Clear();
        GeometrySelectionMode Mode { get; }
        public event SelectionStateChanged SelectionChanged;

        int SelectionCount();
        ISelectable GetSingleSelectedObject();
        List<ISelectable> SelectedObjects();
    }
}
