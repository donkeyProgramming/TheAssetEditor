using Microsoft.Xna.Framework;
using View3D.Utility;

namespace View3D.Components.Rendering
{
    public class ClearScreenComponent : BaseComponent
    {
        private readonly DeviceResolverComponent _deviceResolverComponent;

        public ClearScreenComponent(ComponentManagerResolver componentManagerResolver, DeviceResolverComponent deviceResolverComponent) : base(componentManagerResolver.ComponentManager)
        {
            DrawOrder = (int)ComponentDrawOrderEnum.ClearScreenComponent;
            _deviceResolverComponent = deviceResolverComponent;
        }

        public override void Draw(GameTime gameTime)
        {
            _deviceResolverComponent.Device.Clear(Color.CornflowerBlue);
        }
    }
}
