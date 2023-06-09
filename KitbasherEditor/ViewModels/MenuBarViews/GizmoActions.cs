using CommonControls.Common;
using MonoGame.Framework.WpfInterop;
using System;
using View3D.Components.Component.Selection;
using View3D.Components.Gizmo;

namespace KitbasherEditor.ViewModels.MenuBarViews
{
    public class GizmoActions : NotifyPropertyChangedImpl
    {
        GizmoComponent _gizmoComponent;
        SelectionComponent _selectionComponent;
        TransformToolViewModel _transformToolViewModel;

        public GizmoActions(TransformToolViewModel transformToolViewModel, IComponentManager componentManager)
        {
            _transformToolViewModel = transformToolViewModel;
            _gizmoComponent = componentManager.GetComponent<GizmoComponent>();
            _selectionComponent = componentManager.GetComponent<SelectionComponent>();
        }

        public void UpdateSelectionMode(GeometrySelectionMode mode)
        {
            if (!_selectionComponent.Isinitialized)
                return;

            if (mode == GeometrySelectionMode.Object)
                _selectionComponent.SetObjectSelectionMode();
            else if (mode == GeometrySelectionMode.Face)
                _selectionComponent.SetFaceSelectionMode();
            else if (mode == GeometrySelectionMode.Vertex)
                _selectionComponent.SetVertexSelectionMode();
            else if (mode == GeometrySelectionMode.Bone)
                _selectionComponent.SetBoneSelectionMode();
            else
                throw new NotImplementedException("Unkown state");
        }

        public void Cursor()
        {
            _gizmoComponent.ResetScale();
            _transformToolViewModel.SetMode(TransformToolViewModel.TransformMode.None);
            _gizmoComponent.Disable();
        }

        public void Move()
        {
            _gizmoComponent.ResetScale();
            _transformToolViewModel.SetMode(TransformToolViewModel.TransformMode.Translate);
            _gizmoComponent.SetGizmoMode(GizmoMode.Translate);
        }

        public  void Rotate()
        {
            _gizmoComponent.ResetScale();
            _transformToolViewModel.SetMode(TransformToolViewModel.TransformMode.Rotate);
            _gizmoComponent.SetGizmoMode(GizmoMode.Rotate);
        }

        public void Scale()
        {
            _gizmoComponent.ResetScale();
            _transformToolViewModel.SetMode(TransformToolViewModel.TransformMode.Scale);
            _gizmoComponent.SetGizmoMode(GizmoMode.NonUniformScale);
        }

        public void ScaleGizmoDown() => _gizmoComponent.ModifyGizmoScale(-0.5f);

        public void ScaleGizmoUp() => _gizmoComponent.ModifyGizmoScale(0.5f);
       
    }
}
