using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.UICommands
{
    public enum MutationType
    {
        Add,
        Remove
    }

    public interface IAudioProjectMutationUICommand : IUiCommand
    {
        MutationType Action { get; }
        AudioProjectExplorerTreeNodeType NodeType { get; }
        void Execute(DataRow row);
    }

    public interface IAudioProjectMutationUICommandFactory
    {
        IAudioProjectMutationUICommand Create(MutationType action, AudioProjectExplorerTreeNodeType nodeType);
    }

    public sealed class AudioProjectMutationUICommandFactory(IEnumerable<IAudioProjectMutationUICommand> uiCommands) : IAudioProjectMutationUICommandFactory
    {
        private readonly Dictionary<(MutationType action, AudioProjectExplorerTreeNodeType nodeType), IAudioProjectMutationUICommand> _uiCommands = 
            uiCommands.ToDictionary(uiCommand => (uiCommand.Action, uiCommand.NodeType));

        public IAudioProjectMutationUICommand Create(MutationType action, AudioProjectExplorerTreeNodeType nodeType)
        {
            if (!_uiCommands.TryGetValue((action, nodeType), out var uiCommand))
                throw new InvalidOperationException($"No UICommand registered for {nodeType}.");
            return uiCommand;
        }
    }
}
