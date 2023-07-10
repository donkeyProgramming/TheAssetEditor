// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommonControls.Common;
using Microsoft.Extensions.DependencyInjection;

namespace CommonControls.Editors.TextEditor
{
    public class TextEditor_DependencyInjectionContainer
    {
        public static void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<TextEditorView>();
            serviceCollection.AddTransient<DefaultTextConverter>();
            serviceCollection.AddTransient<TextEditorViewModel<DefaultTextConverter>>();
        }

        public static void RegisterTools(IToolFactory factory)
        {
            factory.RegisterTool<TextEditorViewModel<DefaultTextConverter>, TextEditorView>(
                new ExtentionToTool(
                    EditorEnums.XML_Editor,
                    new[] { ".json", ".xml", ".txt", ".wsmodel", ".xml.material", ".anim.meta.xml", ".anm.meta.xml", ".snd.meta.xml", ".bmd.xml", ".csv", ".bnk.xml" }));
        }
    }
}
