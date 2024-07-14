using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace GameWorld.Core.Components.Rendering
{
    internal static class RasterStateHelper
    {
        static public void Rebuild(Dictionary<RasterizerStateEnum, RasterizerState> renderState, bool cullingEnabled, bool bigSceneDepthBias )
        {
            foreach (var item in renderState.Values)
                item.Dispose();

            renderState.Clear();

            var cullMode = cullingEnabled ? CullMode.CullCounterClockwiseFace : CullMode.None;
            var bias = bigSceneDepthBias ? 0 : 0;
            var depthOffsetBias = 0.00005f;

            renderState[RasterizerStateEnum.Normal] = new RasterizerState
            {
                FillMode = FillMode.Solid,
                CullMode = cullMode,
                DepthBias = bias,
                DepthClipEnable = true,
                MultiSampleAntiAlias = true
            };

            renderState[RasterizerStateEnum.Wireframe] = new RasterizerState
            {
                FillMode = FillMode.WireFrame,
                CullMode = cullMode,
                DepthBias = bias - depthOffsetBias,
                DepthClipEnable = true,
                MultiSampleAntiAlias = true
            };

            renderState[RasterizerStateEnum.SelectedFaces] = new RasterizerState
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.None,
                DepthBias = bias - depthOffsetBias,
                DepthClipEnable = true,
                MultiSampleAntiAlias = true
            };
        }
    }
}
