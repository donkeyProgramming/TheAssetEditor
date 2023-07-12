// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using CommonControls.FileTypes.PackFiles.Models;

namespace CommonControls.Common
{
    public interface IEditorViewModel
    {
        NotifyAttr<string> DisplayName { get; set; }
        PackFile MainFile { get; set; }
        bool Save();
        void Close();
        bool HasUnsavedChanges { get; set; }
      
    }

    public interface IEditorScopeResolverHint
    { 
        Type GetScopeResolverType { get;}
    }

    public interface IEditorCreator
    {
        void OpenFile(PackFile file);
        void CreateEmptyEditor(IEditorViewModel editorView);
    }

    public delegate void EditorSavedDelegate(PackFile newFile);
}
