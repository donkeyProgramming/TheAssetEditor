using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Scene;

namespace View3D.Rendering
{
    public class GraphicsArgs
    {
        public ArcBallCamera Camera { get; private set; }
        public GraphicsDevice GraphicsDevice { get; private set; }
        public ResourceLibary ResourceLibary { get; private set; }

        public GraphicsArgs(ArcBallCamera camera, GraphicsDevice device, ResourceLibary resourceLibary)
        {
            Camera = camera;
            GraphicsDevice = device;
            ResourceLibary = resourceLibary;
        }
    }
}
