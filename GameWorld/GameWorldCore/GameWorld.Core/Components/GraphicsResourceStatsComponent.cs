using System.Text;
using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Rendering.RenderItems;
using GameWorld.Core.Services;
using GameWorld.Core.Utility;
using Microsoft.Xna.Framework;

namespace GameWorld.Core.Components
{
    public class GraphicsResourceStatsComponent : BaseComponent
    {
        private readonly IDeviceResolver _deviceResolver;
        private readonly RenderEngineComponent _renderEngineComponent;
        private readonly IGraphicsResourceCreator _graphicsResourceCreator;

        public GraphicsResourceStatsComponent(IDeviceResolver deviceResolver, RenderEngineComponent renderEngineComponent, IGraphicsResourceCreator graphicsResourceCreator)
        {
            _deviceResolver = deviceResolver;
            _renderEngineComponent = renderEngineComponent;
            _graphicsResourceCreator = graphicsResourceCreator;
        }


        public override void Draw(GameTime gameTime)
        {
            var records = _graphicsResourceCreator.Records;
            var groupedByType = records
                .GroupBy(x => x.ResourceType)
                .OrderBy(x => x.Key)
                .ToList();

            var builder = new StringBuilder();
            builder.AppendLine($"GR Total: {records.Count}");

            foreach (var group in groupedByType)
                builder.AppendLine($"{group.Key}: {group.Count()}");

            var displayText = builder.ToString().TrimEnd();
            var textSize = _renderEngineComponent.DefaultFont.MeasureString(displayText);
            var viewport = _deviceResolver.Device.Viewport;
            var position = new Vector2(
                viewport.Width - textSize.X - 5,
                viewport.Height - textSize.Y - 5);

            var renderItem = new FontRenderItem(_renderEngineComponent, displayText, position, Color.LightGreen);
            _renderEngineComponent.AddRenderItem(RenderBuckedId.Font, renderItem);
        }
    }
}
