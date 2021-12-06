using Common;
using CommonControls.Common;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Scene;

namespace View3D
{
    public static class View3D_DependencyInjectionContainer
    {
        public static void Register(IServiceCollection serviceCollection)
        {
            //serviceCollection.AddTransient<SceneViewModel>();
            //serviceCollection.AddTransient<SceneView3D>();
        }

        public static void RegisterTools(IToolFactory factory)
        {
            //factory.RegisterTool<SceneViewModel, SceneView3D>();
        }
    }
}
