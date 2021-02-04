using System;
using System.Collections.Generic;
using System.Text;

namespace View3D.Components.Component.Selection
{
    public enum GeometrySelectionMode
    {
        Object,
        Face,
        Vertex
    };


    public delegate void SelectionStateChanged(ISelectionState state);
    public interface ISelectionState
    {
        ISelectionState Clone();
        void Clear();
        void Restore();
        GeometrySelectionMode Mode { get; }
        public event SelectionStateChanged SelectionChanged;

        int SelectionCount();
    }
}
