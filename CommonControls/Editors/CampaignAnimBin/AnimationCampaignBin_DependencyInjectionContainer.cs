// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommonControls.Common;
using CommonControls.Editors.TextEditor;
using Microsoft.Extensions.DependencyInjection;

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
