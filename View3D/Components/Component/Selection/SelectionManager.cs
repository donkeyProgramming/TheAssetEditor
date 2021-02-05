using Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System;
using System.Collections.Generic;
using View3D.Components.Input;
using View3D.Components.Rendering;
using View3D.Rendering;
using View3D.Scene;

namespace View3D.Components.Component.Selection
{
    public delegate void SelectionChangedDelegate(IEnumerable<RenderItem> items);
    public class SelectionManager : BaseComponent
    {
        public event SelectionChangedDelegate SelectionChanged;

        ILogger _logger = Logging.Create<SelectionManager>();
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

                case GeometrySelectionMode.Vertex:
                    _currentState = new VertexSelectionState();
                    break;

                default:
                    throw new Exception();
            }

            _currentState.SelectionChanged += SelectionManager_SelectionChanged;
            return _currentState;
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

        private void SelectionManager_SelectionChanged(ISelectionState state)
        {
            if(state.Mode == GeometrySelectionMode.Object)
                SelectionChanged?.Invoke((state as ObjectSelectionState).CurrentSelection());
        }
    }
}

