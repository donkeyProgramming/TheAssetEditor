using Common;
using CommonControls.Common;
using CommonControls.Editors.TextEditor;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

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
            factory.RegisterTool<TextEditorViewModel<CampaignAnimBinToXmlConverter>, TextEditorView>(new PathToTool(".bin", @"animations\campaign\database"));
        }
    }
}
