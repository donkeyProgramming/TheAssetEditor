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
            if (!_mouse.IsMouseButtonReleased(MouseButton.Left))
                return;

            if (_keyboard.IsKeyDown(Keys.LeftAlt))
                return;

            RenderItem bestItem = null;
            float bestDistance = float.MaxValue;
            var ray = _camera.CreateCameraRay(_mouse.Position());

            foreach (var item in _sceneManger.RenderItems)
            {
                var distance = item.Geometry.Intersect(item.ModelMatrix, ray);
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
                var currentSelection = _selectionManager.CurrentSelection();
                if (currentSelection.Count == 1 && currentSelection.FirstOrDefault() == bestItem)
                    return;

                var selectionCommand = new SelectionCommand(_selectionManager);
                selectionCommand.IsModification = _keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift);
                selectionCommand.Items.Add(bestItem);
                _commandManager.ExecuteCommand(selectionCommand);
            }
        }
    }
}
