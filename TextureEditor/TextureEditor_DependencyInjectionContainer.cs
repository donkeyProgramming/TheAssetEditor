using CommonControls.Common;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using TextureEditor.ViewModels;
using TextureEditor.Views;

namespace TextureEditor
{
    public class TextureEditor_DependencyInjectionContainer
    {
        public static void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<TexturePreviewView>();
            serviceCollection.AddTransient<TextureEditorViewModel>();
            serviceCollection.AddTransient<IEditorViewModel, TextureEditorViewModel>();
        }

        public static void RegisterTools(IToolFactory factory)
        {
            factory.RegisterFileTool<TextureEditorViewModel, TexturePreviewView>(new ExtentionToTool(EditorEnums.Texture_Editor, new[] { ".dds", ".png", ".jpeg" }));
        }
    }
}
