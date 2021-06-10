using Common;
using CommonControls.Table;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.Editors.AnimationFragment
{

    public class AnimationFragment_DependencyInjectionContainer
    {
        public static void Register(IServiceCollection serviceCollection)
        {
            //serviceCollection.AddTransient<TableView>();
            //serviceCollection.AddTransient<AnimationFragmentViewModel>();
        }
    
        public static void RegisterTools(IToolFactory factory)
        {
            //factory.RegisterTool<AnimationFragmentViewModel, TableView>(".frg");
        }
    }
}   
