﻿using System;
using System.Collections.Generic;
using System.Text;
using View3D.Rendering;
using View3D.SceneNodes;

namespace View3D.Components.Component.Selection
{
    public enum GeometrySelectionMode
    {
        Object,
        Face,
        Vertex
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