using CommonControls.Common;
using CommonControls.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.Editors.Wtui
{
    public class TwUi_DependencyInjectionContainer
    {
        public static void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<TwUiMainView>();
            serviceCollection.AddTransient<TwUiViewModel>();
        }

        public static void RegisterTools(IToolFactory factory)
        {
            factory.RegisterTool<TwUiViewModel, TwUiMainView>(new ExtentionToTool(EditorEnums.AnimationPack_Editor, new[] { ".twui.xml" }));
            //factory.RegisterTool<TextEditorViewModel<CampaignAnimBinToXmlConverter>, TextEditorView>(new PathToTool(".bin", @"animations\database\battle\bin"));
        }
    }

    public static class TwUi_Debug
    {
        public static void Load(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var packFile = packfileService.FindFile(@"animations\animation_tables\animation_tables.animpack");
            creator.OpenFile(packFile);
        }
    }
}
