using CommonControls.Common;
using CommonControls.Editors.TextEditor;
using Microsoft.Extensions.DependencyInjection;

namespace CommonControls.Editors.VariantMeshDefinition
{
    public class VariantMeshDefinition_DependencyInjectionContainer
    {
        public static void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<VariantMeshToXmlConverter>();
            serviceCollection.AddTransient<TextEditorViewModel<VariantMeshToXmlConverter>>();
        }

        public static void RegisterTools(IToolFactory factory)
        {
            factory.RegisterTool<TextEditorViewModel<VariantMeshToXmlConverter>, TextEditorView>(new ExtentionToTool(EditorEnums.XML_Editor, new[] { ".variantmeshdefinition" }));
        }
    }
}
