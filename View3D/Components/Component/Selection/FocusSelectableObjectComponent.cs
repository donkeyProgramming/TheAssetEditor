using Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using View3D.Components.Input;
using View3D.Components.Rendering;

namespace View3D.Components.Component.Selection
{
    public class FocusSelectableObjectComponent : BaseComponent
    {
        ILogger _logger = Logging.Create<FocusSelectableObjectComponent>();

        SelectionManager _selectionManager;
        KeyboardComponent _keyboard;
        ArcBallCamera _archballCamera;
        SceneManager _sceneManager;



        public FocusSelectableObjectComponent(WpfGame game) : base(game) { }

        public override void Initialize()
        {
            _selectionManager = GetComponent<SelectionManager>();
            _keyboard = GetComponent<KeyboardComponent>();
            _archballCamera = GetComponent<ArcBallCamera>();
            _sceneManager = GetComponent<SceneManager>();

            base.Initialize();
        }

        public override void Update(GameTime gameTime)
        {
            if (_keyboard.IsKeyComboReleased(Keys.F, Keys.LeftControl))
                Focus(_selectionManager.GetState().SelectedObjects());

            base.Update(gameTime);
        }


        void Focus(IEnumerable<ISelectable> items)
        {
            if (items.Count() == 0)
                return;
            Vector3 finalPos = Vector3.Zero;
            foreach (var item in items)
                finalPos += Vector3.Transform(GetCenter(item.Geometry.BoundingBox), _sceneManager.GetWorldPosition(item));

            _archballCamera.LookAt = finalPos / items.Count();
        }

        Vector3 GetCenter(BoundingBox box)
        {
            Vector3 finalPos = Vector3.Zero;
            var corners = box.GetCorners();
            foreach (var corner in corners)
                finalPos += corner;

            return finalPos / corners.Length;
        }
    }
}
