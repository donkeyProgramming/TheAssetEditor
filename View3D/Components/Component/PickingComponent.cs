using Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Framework.WpfInterop;
using Serilog;
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
            if (_keyboard.IsKeyReleased(Keys.F1) && _selectionManager.GeometrySelectionMode != GeometrySelectionMode.Object)
                _commandManager.ExecuteCommand(new ObjectSelectionModeCommand(_selectionManager, GeometrySelectionMode.Object));
            else if (_keyboard.IsKeyReleased(Keys.F2) && _selectionManager.GeometrySelectionMode != GeometrySelectionMode.Face)
                _commandManager.ExecuteCommand(new ObjectSelectionModeCommand(_selectionManager, GeometrySelectionMode.Face));

            if (!_mouse.IsMouseButtonReleased(MouseButton.Left) || _keyboard.IsKeyDown(Keys.LeftAlt))
                return;

            var ray = _camera.CreateCameraRay(_mouse.Position());

            // if there is an object in face mode - Pick face
            var currentSelection = _selectionManager.CurrentSelection();
            var selectedItem = currentSelection.FirstOrDefault();
            if (selectedItem != null && _selectionManager.GeometrySelectionMode == GeometrySelectionMode.Face)
            {
                if (selectedItem.Geometry.IntersectFace(ray, selectedItem.ModelMatrix, out var selectedFace))
                {
                    _logger.Here().Information($"Selected face {selectedFace} in {selectedItem.Name}");

                    FaceSelectionCommand faceSelectionCommand = new FaceSelectionCommand(_selectionManager)
                    {
                        IsModification = _keyboard.IsKeyDown(Keys.LeftShift),
                        SelectedFaces = selectedFace
                    };
                    _commandManager.ExecuteCommand(faceSelectionCommand);
                    return;
                }
            }

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
                if (currentSelection.Count == 1 && currentSelection.FirstOrDefault() == bestItem)
                    return; // Dont trigger a selection if we are selecting the same object

                var selectionCommand = new ObjectSelectionCommand(_selectionManager);
                selectionCommand.IsModification = _keyboard.IsKeyDown(Keys.LeftShift);
                selectionCommand.Items.Add(bestItem);
                _commandManager.ExecuteCommand(selectionCommand);
            }
            else
            {
                if (currentSelection.Count() != 0)
                {
                    var selectionCommand = new ObjectSelectionCommand(_selectionManager);
                    selectionCommand.ClearSelection = true;
                    _commandManager.ExecuteCommand(selectionCommand);
                }
            }
        }
    }
}
