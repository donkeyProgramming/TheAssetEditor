using CommonControls.Editors.TextEditor;
using Microsoft.Extensions.DependencyInjection;
using SharedCore.ToolCreation;

namespace CommonControls.Editors.CampaignAnimBin
{
    public class CampaignAnimBin_DependencyInjectionContainer
    {
        public static void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<CampaignAnimBinToXmlConverter>();
            serviceCollection.AddTransient<TextEditorViewModel<CampaignAnimBinToXmlConverter>>();
        }

        public static void RegisterTools(IToolFactory factory)
        {
            factory.RegisterTool<TextEditorViewModel<CampaignAnimBinToXmlConverter>, TextEditorView>(new PathToTool(EditorEnums.XML_Editor, ".bin", @"animations\campaign\database"));
        }
    }
}
