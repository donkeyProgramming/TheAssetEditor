using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using View3D.Components.Input;
using View3D.Rendering;
using View3D.Scene;

namespace View3D.Components.Component
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
    }

    public class ObjectSelectionState : ISelectionState
    {
        public event SelectionStateChanged SelectionChanged;
        public GeometrySelectionMode Mode => GeometrySelectionMode.Object;

        List<RenderItem> _selectionList { get; set; } = new List<RenderItem>();

        public void ModifySelection(RenderItem newSelectionItem)
        {
            if (_selectionList.Contains(newSelectionItem))
                _selectionList.Remove(newSelectionItem);
            else
                _selectionList.Add(newSelectionItem);

            SelectionChanged?.Invoke(this);
        }

        public List<RenderItem> CurrentSelection() 
        { 
            return _selectionList; 
        }

        public void Clear()
        {
            _selectionList.Clear();
            SelectionChanged?.Invoke(this);
        }

        public ISelectionState Clone()
        {
            return new ObjectSelectionState()
            {
                _selectionList = new List<RenderItem>(_selectionList)
            };
        }

        public void Restore()
        {
            SelectionChanged?.Invoke(this);
        }
    }

    public class FaceSelectionState : ISelectionState
    {
        public GeometrySelectionMode Mode => GeometrySelectionMode.Face;
        public event SelectionStateChanged SelectionChanged;

        public RenderItem RenderObject { get; set; }
        public List<int> SelectedFaces { get; set; } = new List<int>();


        public void ModifySelection(int newSelectionItem)
        {
            if (SelectedFaces.Contains(newSelectionItem))
                SelectedFaces.Remove(newSelectionItem);
            else
                SelectedFaces.Add(newSelectionItem);

            SelectionChanged?.Invoke(this);
        }

        public List<int> CurrentSelection()
        {
            return SelectedFaces;
        }

        public void Clear()
        {
            SelectedFaces.Clear();
            SelectionChanged?.Invoke(this);
        }


        public void EnsureSorted()
        {
            SelectedFaces = SelectedFaces.Distinct().OrderBy(x => x).ToList();
        }


        public ISelectionState Clone()
        {
            return new FaceSelectionState()
            {
                RenderObject = RenderObject,
                SelectedFaces = new List<int>(SelectedFaces)
            };
        }

        public void Restore()
        {
            
        }
    }

    
    public delegate void SelectionChangedDelegate(IEnumerable<RenderItem> items);
    public class SelectionManager : BaseComponent
    {
        public event SelectionChangedDelegate SelectionChanged;
        ISelectionState _currentState;

        public SelectionManager(WpfGame game ) : base(game) {}

        public override void Initialize()
        {
            CreateSelectionSate(GeometrySelectionMode.Object);
            base.Initialize();
        }

        public ISelectionState CreateSelectionSate(GeometrySelectionMode mode)
        {
            if (_currentState != null)
            {
                _currentState.Clear();
                _currentState.SelectionChanged -= SelectionManager_SelectionChanged;
            }

            switch (mode)
            {
                case GeometrySelectionMode.Object:
                    _currentState = new ObjectSelectionState();
                    break;

                case GeometrySelectionMode.Face:
                    _currentState = new FaceSelectionState();
                    break;

                default:
                    throw new Exception();
            }

            _currentState.SelectionChanged += SelectionManager_SelectionChanged;
            return _currentState;
        }

        private void SelectionManager_SelectionChanged(ISelectionState state)
        {
            if(state.Mode == GeometrySelectionMode.Object)
                SelectionChanged?.Invoke((state as ObjectSelectionState).CurrentSelection());
        }


        public ISelectionState GetState()
        {
            return _currentState;
        }

        public ISelectionState GetStateCopy()
        {
            return _currentState.Clone();
        }

        public void SetState(ISelectionState state)
        {
            _currentState.SelectionChanged -= SelectionManager_SelectionChanged;
            _currentState = state;
            _currentState.SelectionChanged += SelectionManager_SelectionChanged;
            _currentState.Restore();
        }
    }
}

