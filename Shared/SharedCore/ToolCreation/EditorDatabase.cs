﻿using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Shared.Core.DependencyInjection;
using Shared.Core.ErrorHandling;

namespace Shared.Core.ToolCreation
{
    public interface IEditorDatabase
    {
        public void Register(EditorInfo editorInfo);

        IEditorInterface? Create(string fullFileName, EditorEnums? preferedEditor = null);
        IEditorInterface Create(EditorEnums editorEnum);

        void DestroyEditor(IEditorInterface instance);
        Type GetViewTypeFromViewModel(Type viewModelType);
        List<EditorInfo> GetEditorInfos();
    }

    public class EditorDatabase : IEditorDatabase
    {
        private readonly ILogger _logger = Logging.Create<EditorDatabase>();

        private readonly IServiceProvider _serviceProvider;
        private readonly ScopeRepository _scopeRepository;
        private readonly IToolSelectorUiProvider _toolSelectorUiProvider;

        private readonly List<EditorInfo> _editors = [];

        public EditorDatabase(IServiceProvider serviceProvider, ScopeRepository scopeRepository, IToolSelectorUiProvider toolSelectorUiProvider)
        {
            _serviceProvider = serviceProvider;
            _scopeRepository = scopeRepository;
            _toolSelectorUiProvider = toolSelectorUiProvider;
        }

        public void Register(EditorInfo editorInfo)
        {
            if (_editors.Any(x => x.EditorEnum == editorInfo.EditorEnum))
            {
                var errorMessage = $"Tool already registered - {editorInfo.EditorEnum}";
                _logger.Here().Error(errorMessage);
                throw new Exception(errorMessage);
            }

            _editors.Add(editorInfo);
        }

        public Type GetViewTypeFromViewModel(Type viewModelType)
        {
            _logger.Here().Information($"Getting view for ViewModel - {viewModelType}");
            
            var instance = _editors.First(x=>x.ViewModel ==  viewModelType);
            return instance.View;
        }

        public IEditorInterface Create(EditorEnums editorEnum) 
        {
            var editor = _editors.First(x => x.EditorEnum == editorEnum);
            return CreateEditorInternal(editor.ViewModel);
        }

        public IEditorInterface? Create(string fullFileName, EditorEnums? preferedEditor)
        {
            var allEditors = GetAllPossibleEditors(fullFileName);

            if (allEditors.Count == 0)
            {
                _logger.Here().Warning($"Trying to open file {fullFileName}, but there are no valid tools for it.");
                return null;
            }

            Type selectedEditor = null;
            if (allEditors.Count == 1)
            {
                selectedEditor = allEditors.First().ViewModel;
            }
            else if (allEditors.Count > 1 && preferedEditor != null)
            {
                var preferedEditorType = allEditors.FirstOrDefault(x => x.EditorEnum == preferedEditor);
                if(preferedEditorType == null)
                    throw new Exception($"The prefered editor {preferedEditor} can not open {fullFileName}");
                selectedEditor = preferedEditorType.ViewModel;
            }
            else
            {
                var selectedToolType = _toolSelectorUiProvider.CreateAndShow(allEditors.Select(x => x.EditorEnum));
                if (selectedToolType == EditorEnums.None)
                    return null;
                selectedEditor = allEditors.First(x => x.EditorEnum == selectedToolType).ViewModel;
            }

            return CreateEditorInternal(selectedEditor);
        }

        IEditorInterface CreateEditorInternal(Type editorType)
        {
            ApplicationStateRecorder.EditorOpened();

            var scope = _serviceProvider.CreateScope();
            var instance = scope.ServiceProvider.GetRequiredService(editorType) as IEditorInterface;
            if (instance == null)
                throw new Exception($"Type '{editorType}' is not a IEditorViewModel");
            _scopeRepository.Add(instance, scope);
            return instance;
        }

        List<EditorInfo> GetAllPossibleEditors(string fullFileName)
        {
            var extension = Regex.Match(fullFileName, @"\..*").Value;
            if (extension.Contains("{") && extension.Contains("}"))
            {
                var ext2 = Regex.Match(extension, @"\..*\.(.*)\.(.*)");
                if (ext2.Success)
                {
                    extension = "." + ext2.Groups[1].Value + "." + ext2.Groups[2].Value;
                }
            }

            var output = new List<(EditorInfo info, int priority)>();
            foreach (var toolLookUp in _editors)
            {
                var hasValidExtension = false;
         
                var priority = -1;

                foreach (var toolExtension in toolLookUp.Extensions)
                {
                    if (toolExtension.Extension == extension)
                    {
                        if (toolExtension.Priority > priority)
                        {
                            hasValidExtension = true;
                            priority = toolExtension.Priority;
                        }
                    }
                }

                var isValidForFolder = false;
                if (toolLookUp.FolderRules.Count == 0)
                {
                    isValidForFolder = true;
                }
                else
                {
                    foreach (var toolExtension in toolLookUp.FolderRules)
                    {
                        if (fullFileName.Contains(toolExtension))
                            isValidForFolder = true;
                    }
                }
 
                if (hasValidExtension && isValidForFolder)
                    output.Add((toolLookUp, priority));
            }

            if (output.Count == 0)
            {
                var error = $"Attempting to get view model for file {fullFileName}, unable to find tool based on rules";
                _logger.Here().Error(error);
            }

            return output
                .OrderByDescending(x=>x.priority)
                .Select(x=>x.info)
                .ToList();
        }

        public List<EditorInfo> GetEditorInfos() => _editors; 

        public void DestroyEditor(IEditorInterface instance) => _scopeRepository.RemoveScope(instance);
    }
}
