using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using View3D.Components.Input;
using View3D.Rendering;
using View3D.Scene;

namespace View3D.Components.Component
{
    public delegate void SelectionChangedDelegate(IEnumerable<RenderItem> items);
    public class SelectionManager : BaseComponent
    {
        public event SelectionChangedDelegate SelectionChanged;
        KeyboardComponent _keyboard;

        public class State
        {
            public List<RenderItem> SelectionList { get; set; } = new List<RenderItem>();
            public GeometrySelectionMode SelectionMode { get; set; } = GeometrySelectionMode.Object;
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



        public List<RenderItem> CurrentSelection()
        {
            return new List<RenderItem>(_currentState.SelectionList);
        }

        public void SetCurrentSelection(List<RenderItem> renderItems)
        {
            _currentState.SelectionList = new List<RenderItem>(renderItems);
            SelectionChanged?.Invoke(_currentState.SelectionList);
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

        public GeometrySelectionMode GeometrySelectionMode { get { return _currentState.SelectionMode; } set { _currentState.SelectionMode = value; } }

        public State GetState()
        {
            return new State()
            {
                SelectionList = new List<RenderItem>(_currentState.SelectionList),
                SelectionMode = _currentState.SelectionMode
            };
        }

        public void SetState(State state)
        {
            _currentState = state;
            SelectionChanged?.Invoke(_currentState.SelectionList);
        }
    }
}

