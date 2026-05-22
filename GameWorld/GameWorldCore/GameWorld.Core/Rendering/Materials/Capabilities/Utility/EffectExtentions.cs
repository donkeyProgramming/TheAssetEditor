using System;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;

namespace GameWorld.Core.Rendering.Materials.Capabilities.Utility
{
    public static class EffectExtentions
    {
        public static EffectParameter GetParameter(this Effect effect, string parameterName)
        {
            var param = effect.Parameters.FirstOrDefault(x => x.Name == parameterName);
            if (param == null)
                throw new Exception($"Parameter {parameterName} is not a part of {effect.Name}");

            return param;
        }
    }
}
