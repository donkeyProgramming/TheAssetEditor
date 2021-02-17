using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Text;

namespace View3D.Components.Rendering
{
    public class ClearScreenComponent : BaseComponent
    {
        public ClearScreenComponent(WpfGame game) : base(game)
        {
            DrawOrder = (int)ComponentDrawOrderEnum.ClearScreenComponent;
        }

        public override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
        }
    }
}
