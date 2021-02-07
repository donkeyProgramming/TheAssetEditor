using Common;
using KitbasherEditor.ViewModels;
using KitbasherEditor.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace KitbasherEditor
{
    public class KitbasherEditor_DependencyInjectionContainer
    {
        public static void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<KitbasherView>();
            serviceCollection.AddTransient<KitbasherViewModel>();
        }

        public static void RegisterTools(IToolFactory factory)
        {
            factory.RegisterTool<KitbasherViewModel, KitbasherView>();
        }
    }
}
