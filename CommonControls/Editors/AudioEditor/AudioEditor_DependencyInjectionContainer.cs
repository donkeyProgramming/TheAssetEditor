using CommonControls.Common;
using CommonControls.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.Editors.AudioEditor
{
    public class AudioEditor_DependencyInjectionContainer
    {
        public static void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<AudioEditorMainView>();
            serviceCollection.AddTransient<AudioEditorViewModel>();
        }

        public static void RegisterTools(IToolFactory factory)
        {
            factory.RegisterTool<AudioEditorViewModel, AudioEditorMainView>();
        }
    }
}
