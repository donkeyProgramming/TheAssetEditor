using System;
using System.Collections.Generic;
using System.Linq;
using GameWorld.Core.Services;
using Shared.Core.DependencyInjection;
using Shared.Core.ErrorHandling.Exceptions;
using Shared.Core.ToolCreation;

namespace AssetEditor.Services
{
    internal class GraphicsResourceExceptionInfoProvider : IExceptionInformationProvider
    {
        private readonly IEditorManager _editorManager;
        private readonly IScopeRepository _scopeRepository;

        public GraphicsResourceExceptionInfoProvider(IEditorManager editorManager, IScopeRepository scopeRepository)
        {
            _editorManager = editorManager;
            _scopeRepository = scopeRepository;
        }

        public void HydrateExcetion(ExceptionInformation exceptionInformation)
        {
            try
            {
                var allEditors = _editorManager.GetAllEditors();
                var currentEditorIndex = _editorManager.GetCurrentEditor();
                var hasCurrentEditor = currentEditorIndex >= 0 && currentEditorIndex < allEditors.Count;
                var currentEditor = hasCurrentEditor ? allEditors[currentEditorIndex] : null;

                var editorHandles = _scopeRepository.GetEditorHandles();

                foreach (var editor in editorHandles)
                {
                    var isCurrentScope = currentEditor != null && ReferenceEquals(editor, currentEditor);

                    try
                    {
                        var creator = _scopeRepository.GetRequiredService<IGraphicsResourceCreator>(editor);
                        var records = creator.Records;
                        var resourceLines = records
                            .Select(x => $"id={x.ResourceId} | {x.ResourceType} | owner={x.ScopeOwner} | source={x.SourceFile}:{x.SourceLine}::{x.SourceMember}")
                            .ToList();

                        exceptionInformation.GraphicsResourceScopes.Add(
                            new GraphicsResourceScopeInfo(creator.ScopeOwner, isCurrentScope, records.Count, resourceLines));
                    }
                    catch
                    {
                        exceptionInformation.GraphicsResourceScopes.Add(
                            new GraphicsResourceScopeInfo($"{editor.DisplayName} ({editor.GetType().Name})", isCurrentScope, 0, new List<string>()));
                    }
                }
            }
            catch (Exception e)
            {
                exceptionInformation.CurrentEditorGraphicsResourceInfoError = e.Message;
            }
        }
    }
}
