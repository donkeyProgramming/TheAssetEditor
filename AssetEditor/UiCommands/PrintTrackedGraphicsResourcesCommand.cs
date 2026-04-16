using System;
using System.Text;
using GameWorld.Core.Services;
using Serilog;
using Shared.Core.DependencyInjection;
using Shared.Core.ErrorHandling;
using Shared.Core.Events;
using Shared.Core.ToolCreation;

namespace AssetEditor.UiCommands
{
    public class PrintTrackedGraphicsResourcesCommand : IUiCommand
    {
        private readonly ILogger _logger = Logging.Create<PrintTrackedGraphicsResourcesCommand>();
        private readonly IScopeRepository _scopeRepository;
        private readonly IEditorManager _editorManager;

        public PrintTrackedGraphicsResourcesCommand(IScopeRepository scopeRepository, IEditorManager editorManager)
        {
            _scopeRepository = scopeRepository;
            _editorManager = editorManager;
        }

        public void Execute()
        {
            var builder = new StringBuilder();
            var allEditors = _editorManager.GetAllEditors();
            var currentEditorIndex = _editorManager.GetCurrentEditor();
            var hasCurrentEditor = currentEditorIndex >= 0 && currentEditorIndex < allEditors.Count;
            var currentEditor = hasCurrentEditor ? allEditors[currentEditorIndex] : null;

            var editorHandles = _scopeRepository.GetEditorHandles();

            builder.AppendLine();
            builder.AppendLine("=== Graphics Resource Tracker Dump ===");
            builder.AppendLine($"Scopes: {editorHandles.Count}");

            var totalTrackedResources = 0;
            foreach (var editor in editorHandles)
            {
                var isCurrentScope = currentEditor != null && ReferenceEquals(editor, currentEditor);
                var marker = isCurrentScope ? " [CURRENT]" : string.Empty;
                builder.AppendLine($"Scope: {editor.DisplayName} ({editor.GetType().Name}){marker}");

                try
                {
                    var creator = _scopeRepository.GetRequiredService<IGraphicsResourceCreator>(editor);
                    var records = creator.Records;
                    builder.AppendLine($"  Items in scope: {records.Count}");
                    if (records.Count == 0)
                    {
                        builder.AppendLine("  (no tracked graphics resources)");
                        continue;
                    }

                    foreach (var record in records)
                    {
                        totalTrackedResources++;
                        builder.AppendLine($"  - id={record.ResourceId} | {record.ResourceType} | owner={record.ScopeOwner} | source={record.SourceFile}:{record.SourceLine}::{record.SourceMember}");
                    }
                }
                catch
                {
                    builder.AppendLine("  Items in scope: 0");
                    builder.AppendLine("  (no graphics resource tracker in this scope)");
                }
            }

            builder.AppendLine($"Total tracked items across all scopes: {totalTrackedResources}");
            builder.AppendLine("=== End Graphics Resource Tracker Dump ===");
            _logger.Here().Information(builder.ToString());
        }
    }
}
