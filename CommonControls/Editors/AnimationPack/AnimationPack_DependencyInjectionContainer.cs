using CommonControls.Common;
using CommonControls.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CommonControls.Editors.AnimationPack
{
    public class AnimationPack_DependencyInjectionContainer
    {
        public static void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<AnimationPackView>();
            serviceCollection.AddTransient<AnimPackViewModel>();
        }

        public static void RegisterTools(IToolFactory factory)
        {
            factory.RegisterFileTool<AnimPackViewModel, AnimationPackView>(new ExtentionToTool(EditorEnums.AnimationPack_Editor, new[] { ".animpack" }));
            //factory.RegisterTool<TextEditorViewModel<CampaignAnimBinToXmlConverter>, TextEditorView>(new PathToTool(".bin", @"animations\database\battle\bin"));
        }
    }

    public static class AnimationPackEditor_Debug
    {
        public static void Load(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var packFile = packfileService.FindFile(@"animations\animation_tables\animation_tables.animpack");
            creator.OpenFile(packFile);
        }
    }
}
