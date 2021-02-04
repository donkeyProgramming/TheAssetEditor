using Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using View3D.Commands;
using View3D.Components.Input;
using View3D.Components.Rendering;
using View3D.Rendering;

namespace View3D.Components.Component
{
    public class PickingComponent : BaseComponent
    {
        ILogger _logger = Logging.Create<PickingComponent>();

        ArcBallCamera _camera;
        SceneManager _sceneManger;
        CommandManager _commandManager;
        SelectionManager _selectionManager;
        MouseComponent _mouse;
        KeyboardComponent _keyboard;

        public PickingComponent(WpfGame game) : base(game)
        {
            UpdateOrder = (int)ComponentUpdateOrderEnum.PickingComponent;
            DrawOrder = (int)ComponentDrawOrderEnum.PickingComponent;
        }

        public override void Initialize()
        {
            _camera = GetComponent<ArcBallCamera>();
            _mouse = GetComponent<MouseComponent>();
            _keyboard = GetComponent<KeyboardComponent>();
            _sceneManger = GetComponent<SceneManager>();
            _commandManager = GetComponent<CommandManager>();
            _selectionManager = GetComponent<SelectionManager>();

            base.Initialize();     
        }

        public override void Update(GameTime gameTime)
        {
            var currentState = _selectionManager.GetState();
            var ray = _camera.CreateCameraRay(_mouse.Position());

            if (ChangeSelectionState(currentState))
                return;

            if (!_mouse.IsMouseButtonReleased(MouseButton.Left) || _keyboard.IsKeyDown(Keys.LeftAlt))
                return;

            if (SelectFaces(ray, currentState))
                return;

            SelectObjects(ray, currentState);
        }


        void SelectObjects(Ray ray, ISelectionState currentState)
        {
            RenderItem bestItem = null;
            float bestDistance = float.MaxValue;

            foreach (var item in _sceneManger.RenderItems)
            {
                var distance = item.Geometry.Intersect(ray, item.ModelMatrix);
                if (distance != null)
                {
                    if (distance < bestDistance)
                    {
                        bestDistance = distance.Value;
                        bestItem = item;
                    }
                }
            }

            if (bestItem != null)
            {
                var objectState = currentState as ObjectSelectionState;
                var currentSelectionCount = objectState?.CurrentSelection().Count();
                var currentItem = objectState?.CurrentSelection().FirstOrDefault();

                if (currentSelectionCount == 1 && currentItem == bestItem)
                    return; // Dont trigger a selection if we are selecting the same object

                var selectionCommand = new ObjectSelectionCommand(_selectionManager);
                selectionCommand.IsModification = _keyboard.IsKeyDown(Keys.LeftShift);
                selectionCommand.Items.Add(bestItem);
                _commandManager.ExecuteCommand(selectionCommand);
            }
            else
            {
                var objectState = currentState as ObjectSelectionState;
                var currentSelectionCount = objectState?.CurrentSelection().Count();

                if (currentSelectionCount != 0)
                {
                    var selectionCommand = new ObjectSelectionCommand(_selectionManager);
                    selectionCommand.ClearSelection = true;
                    _commandManager.ExecuteCommand(selectionCommand);
                }
            }
        }

        bool SelectFaces(Ray ray, ISelectionState currentState )
        {
            if (currentState.Mode == GeometrySelectionMode.Face)
            {
                var faceState = currentState as FaceSelectionState;

                if (faceState.RenderObject.Geometry.IntersectFace(ray, faceState.RenderObject.ModelMatrix, out var selectedFace))
                {
                    _logger.Here().Information($"Selected face {selectedFace} in {faceState.RenderObject.Name}");

                    FaceSelectionCommand faceSelectionCommand = new FaceSelectionCommand(_selectionManager)
                    {
                        IsModification = _keyboard.IsKeyDown(Keys.LeftShift),
                        SelectedFaces = new List<int>() { selectedFace.Value }
                    };
                    _commandManager.ExecuteCommand(faceSelectionCommand);
                    return true;
                }
            }

            return false;
        }

        bool ChangeSelectionState(ISelectionState selectionState)
        {
            if (_keyboard.IsKeyReleased(Keys.F1) && _selectionManager.GetState().Mode != GeometrySelectionMode.Object)
            {
                _commandManager.ExecuteCommand(new ObjectSelectionModeCommand(_selectionManager, GeometrySelectionMode.Object));
                return true;
            }

            else if (_keyboard.IsKeyReleased(Keys.F2) && _selectionManager.GetState().Mode != GeometrySelectionMode.Face)
            {
                var objectSelectonState = selectionState as ObjectSelectionState;
                if (objectSelectonState.CurrentSelection().Count == 1)
                {
                    _commandManager.ExecuteCommand(new ObjectSelectionModeCommand(objectSelectonState.CurrentSelection().First(), _selectionManager, GeometrySelectionMode.Face));
                    return true;
                }
            }

            return false;
        }
    }
}
