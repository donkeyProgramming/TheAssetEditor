using Microsoft.Xna.Framework;

namespace View3D.Components.Rendering
{
    public class ClearScreenComponent : BaseComponent
    {
        private readonly DeviceResolverComponent _deviceResolverComponent;

        public ClearScreenComponent(DeviceResolverComponent deviceResolverComponent)
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
