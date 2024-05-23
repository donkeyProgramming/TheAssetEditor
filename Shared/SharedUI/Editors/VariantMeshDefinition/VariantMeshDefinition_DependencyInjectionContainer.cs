// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommonControls.Editors.TextEditor;
using Microsoft.Extensions.DependencyInjection;
using SharedCore.ToolCreation;

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
            factory.RegisterTool<TextEditorViewModel<VariantMeshToXmlConverter>, TextEditorView>(new ExtensionToTool(EditorEnums.XML_Editor, new[] { ".variantmeshdefinition" }));
        }
    }
}
