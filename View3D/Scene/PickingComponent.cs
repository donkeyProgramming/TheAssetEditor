using Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Serilog;
using System.Linq;
using View3D.Commands;
using View3D.Input;
using View3D.Rendering;

namespace View3D.Scene
{
    public class PickingComponent : IGameComponent, IDrawable, IUpdateable
    {
        ILogger _logger = Logging.Create<PickingComponent>();

        private readonly ArcBallCamera _camera;
        private readonly InputSystems _input;
        private readonly SceneManager _sceneManger;
        private readonly CommandManager _commandManager;
        private readonly SelectionManager _selectionManager;

        public event System.EventHandler<System.EventArgs> DrawOrderChanged;
        public event System.EventHandler<System.EventArgs> VisibleChanged;
        public event System.EventHandler<System.EventArgs> EnabledChanged;
        public event System.EventHandler<System.EventArgs> UpdateOrderChanged;
        public bool Visible => true;
        public bool Enabled => true;
        public int UpdateOrder => (int)UpdateOrderEnum.PickingComponent;
        public int DrawOrder => (int)DrawOrderEnum.PickingComponent;

        public PickingComponent(GraphicsArgs graphicArgs, InputSystems input, SceneManager sceneManger, SelectionManager selectionManager, CommandManager commandManager)
        {
            _camera = graphicArgs.Camera;
            _input = input;
            _sceneManger = sceneManger;
            _commandManager = commandManager;
            _selectionManager = selectionManager;
        }

        public void Initialize()
        {
       
        }

        public void Draw(GameTime gameTime)
        {
      
        }

        public void Update(GameTime gameTime)
        {
            if (!_input.Mouse.IsMouseButtonReleased(Input.MouseButton.Left))
                return;

            RenderItem bestItem = null;
            float bestDistance = float.MaxValue;
            var ray = _camera.CreateCameraRay(_input.Mouse.Position());

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
                selectionCommand.IsModification = _input.Keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift);
                selectionCommand.Items.Add(bestItem);
                _commandManager.ExecuteCommand(selectionCommand);
            }
        }
    }
}
