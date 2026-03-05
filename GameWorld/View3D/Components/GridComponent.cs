using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Rendering;
using Microsoft.Xna.Framework;
using Shared.Core.Settings;

namespace GameWorld.Core.Components
{
    public class GridComponent : BaseComponent
    {
        private readonly RenderEngineComponent _renderEngineComponent;
        private readonly ApplicationSettingsService _applicationSettingsService;

        public bool ShowGrid { get; set; } = true;
        public Vector3 GridColur { get; set; } = new Vector3(0, 0, 0);

        public GridComponent(RenderEngineComponent renderEngineComponent, ApplicationSettingsService applicationSettingsService)
        {
            _renderEngineComponent = renderEngineComponent;
            _applicationSettingsService = applicationSettingsService;
        }

        public override void Draw(GameTime gameTime)
        {
            if (ShowGrid == false)
                return;
            
            var gridSize = _applicationSettingsService.CurrentSettings.VisualEditorsGridSize;
            var gridColour = new Color(GridColur);
            var grid = LineHelper.CreateGrid(gridSize, gridColour);
            _renderEngineComponent.AddRenderLines(grid);
        }
    }
}
