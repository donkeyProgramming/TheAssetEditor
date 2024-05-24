using CommonControls.Editors.TextEditor;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.ToolCreation;
using Shared.Ui.Editors.TextEditor;

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
