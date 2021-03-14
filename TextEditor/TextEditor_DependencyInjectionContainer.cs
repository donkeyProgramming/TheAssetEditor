using Common;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace TextEditor
{
    public class TextEditor_DependencyInjectionContainer
    {
        public static void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<TextEditorView>();
            serviceCollection.AddTransient<TextEditorViewModel>();
        }

        public static void RegisterTools(IToolFactory factory)
        {
            factory.RegisterTool<TextEditorViewModel, TextEditorView>(".json", ".xml", ".txt", ".wsmodel", ".variantmeshdefinition");
        }
    }
}
