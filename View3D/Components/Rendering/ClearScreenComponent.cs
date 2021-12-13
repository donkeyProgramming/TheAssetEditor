using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Text;

namespace View3D.Components.Rendering
{
    public class ClearScreenComponent : BaseComponent
    {
        DeviceResolverComponent _deviceResolver;
        public ClearScreenComponent(IComponentManager component) : base(component)
        {
            DrawOrder = (int)ComponentDrawOrderEnum.ClearScreenComponent;
        }

        public override void Initialize()
        {
            _deviceResolver = ComponentManager.GetComponent<DeviceResolverComponent>();
            base.Initialize();
        }

        public override void Draw(GameTime gameTime)
        {
            _deviceResolver.Device.Clear(Color.CornflowerBlue);
        }
    }
}
