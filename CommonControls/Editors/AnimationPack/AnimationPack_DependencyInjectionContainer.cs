// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using SharedCore.Misc;
using SharedCore.PackFiles;
using SharedCore.ToolCreation;

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
            factory.RegisterTool<AnimPackViewModel, AnimationPackView>(new ExtensionToTool(EditorEnums.AnimationPack_Editor, new[] { ".animpack" }));
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
