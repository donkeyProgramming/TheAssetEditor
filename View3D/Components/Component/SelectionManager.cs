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

    public delegate void SelectionChangedDelegate(IEnumerable<RenderItem> items);
    public class SelectionManager : BaseComponent
    {
        public event SelectionChangedDelegate SelectionChanged;
        KeyboardComponent _keyboard;

        public class State
        {
            public List<RenderItem> SelectionList { get; set; } = new List<RenderItem>();
            public GeometrySelectionMode SelectionMode { get; set; } = GeometrySelectionMode.Object;
            public FaceSelection SelectedFaces { get; set; } = new FaceSelection();
        }

        State _currentState = new State();

        public SelectionManager(WpfGame game ) : base(game)
        {
        }

        public override void Initialize()
        {
            _keyboard = GetComponent<KeyboardComponent>();
            base.Initialize();
        }

        // Objects ----
        public List<RenderItem> CurrentSelection()
        {
            return new List<RenderItem>(_currentState.SelectionList);
        }


        internal void ClearSelection()
        {
            _currentState.SelectionList.Clear();
            SelectionChanged?.Invoke(_currentState.SelectionList);
        }

        internal void AddToSelection(RenderItem newSelectionItem)
        {
            _currentState.SelectionList.Add(newSelectionItem);
            SelectionChanged?.Invoke(_currentState.SelectionList);
        }

        internal void ModifySelection(RenderItem newSelectionItem)
        {
            if (_currentState.SelectionList.Contains(newSelectionItem))
                _currentState.SelectionList.Remove(newSelectionItem);
            else
                _currentState.SelectionList.Add(newSelectionItem);

            SelectionChanged?.Invoke(_currentState.SelectionList);
        }


        // Faces ----

        public FaceSelection CurrentFaceSelection()
        {
            return _currentState.SelectedFaces?.Copy();
        }

        internal void ClearFaceSelection()
        {
            _currentState.SelectedFaces.SelectedFaces.Clear();
            //SelectionChanged?.Invoke(_currentState.SelectionList);
        }

        internal void SetFaceSelection(FaceSelection face)
        {
            _currentState.SelectedFaces = face;
            _currentState.SelectedFaces.EnsureSorted();
            //SelectionChanged?.Invoke(_currentState.SelectionList);
        }




        public GeometrySelectionMode GeometrySelectionMode { get { return _currentState.SelectionMode; } set { _currentState.SelectionMode = value; } }

        public State GetState()
        {
            return new State()
            {
                SelectionList = new List<RenderItem>(_currentState.SelectionList),
                SelectionMode = _currentState.SelectionMode,
                SelectedFaces = _currentState.SelectedFaces?.Copy()
            };
        }

        public void SetState(State state)
        {
            _currentState = state;
            SelectionChanged?.Invoke(_currentState.SelectionList);
        }
    }

    public class FaceSelection
    {
        public List<int> SelectedFaces { get; set; } = new List<int>();

        public FaceSelection(int selectedFace)
        {
            SelectedFaces.Add(selectedFace);
        }

        public FaceSelection() { }

        //public void Merge(FaceSelection other)
        //{
        //    foreach (var item in other.SelectedFaces)
        //        SelectedFaces.Add(item);
        //
        //    EnsureSorted();
        //}

        public void EnsureSorted()
        {
            SelectedFaces = SelectedFaces.Distinct().OrderBy(x => x).ToList();
        }

        public FaceSelection Copy()
        {
            var item = new FaceSelection();
            item.SelectedFaces = new List<int>(SelectedFaces);
            return item;
        }
    }
}

