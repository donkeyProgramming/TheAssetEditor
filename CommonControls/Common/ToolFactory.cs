// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using CommonControls.BaseDialogs.ToolSelector;
using CommonControls.Events.Global;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using static CommonControls.Editors.AnimationPack.Converters.AnimationBinWh3FileToXmlConverter;

namespace CommonControls.Common
{

    public class ExtentionToTool : IPackFileToToolSelector
    {
        string[] _validExtentionsCore;
        string[] _validExtentionsOptimal;

        public ExtentionToTool(EditorEnums editorDisplayName, string[] coreTools, string[] optionalTools = null)
        {
            _validExtentionsCore = coreTools;
            _validExtentionsOptimal = optionalTools;
            EditorType = editorDisplayName;
        }

        public EditorEnums EditorType { get; private set; }

        public PackFileToToolSelectorResult CanOpen(string fullPath)
        {
            var extention = Regex.Match(fullPath, @"\..*").Value;
            if (extention.Contains("{") && extention.Contains("}"))
            {
                var ext2 = Regex.Match(extention, @"\..*\.(.*)\.(.*)");
                if (ext2.Success)
                {
                    extention = "." + ext2.Groups[1].Value + "." + ext2.Groups[2].Value;
                }
                //var index = extention.IndexOf("}");
                //extention = extention.Remove(0, index+1);
            }

            if (_validExtentionsCore != null)
            {
                foreach (var validExt in _validExtentionsCore)
                {
                    if (validExt == extention)
                        return new PackFileToToolSelectorResult() { CanOpen = true, IsCoreTool = true };
                }
            }

            if (_validExtentionsOptimal != null)
            {
                foreach (var validExt in _validExtentionsOptimal)
                {
                    if (validExt == extention)
                        return new PackFileToToolSelectorResult() { CanOpen = true, IsCoreTool = false };
                }
            }

            return new PackFileToToolSelectorResult() { CanOpen = false, IsCoreTool = false };
        }
    }

    public class PathToTool : IPackFileToToolSelector
    {
        string _extention;
        string _requiredPathSubString;

        public PathToTool(EditorEnums editorDisplayName, string extention, string requiredPathSubString)
        {
            _extention = extention;
            _requiredPathSubString = requiredPathSubString;
            EditorType = editorDisplayName;
        }

        public EditorEnums EditorType { get; private set; }

        public PackFileToToolSelectorResult CanOpen(string fullPath)
        {
            var extention = Regex.Match(fullPath, @"\..*").Value;
            if (_extention == extention && fullPath.Contains(_requiredPathSubString))
                return new PackFileToToolSelectorResult() { CanOpen = true, IsCoreTool = true };

            return new PackFileToToolSelectorResult() { CanOpen = false, IsCoreTool = false };
        }
    }

    public interface IToolFactory
    {
        IEditorViewModel Create(string fullFileName, bool useDefaultTool = false);
        ViewModel Create<ViewModel>() where ViewModel : IEditorViewModel;
        Window CreateAsWindow(IEditorViewModel viewModel);
        void DestroyEditor(IEditorViewModel instance);
        Type GetViewTypeFromViewModel(Type viewModelType);
        void RegisterTool<ViewModel, View>(IPackFileToToolSelector toolSelector = null)
            where ViewModel : IEditorViewModel
            where View : Control;
    }

    public class ToolFactory : IToolFactory
    {
        ILogger _logger = Logging.Create<IToolFactory>();
        IServiceProvider _serviceProvider;
        private readonly ScopeRepository _scopeRepository;
        Dictionary<Type, Type> _viewModelToViewMap = new Dictionary<Type, Type>();
        Dictionary<IPackFileToToolSelector, Type> _extentionToToolMap = new Dictionary<IPackFileToToolSelector, Type>();

        public ToolFactory(IServiceProvider serviceProvider, ScopeRepository scopeRepository)
        {
            _serviceProvider = serviceProvider;
            _scopeRepository = scopeRepository;
        }

        public void RegisterTool<ViewModel, View>(IPackFileToToolSelector toolSelector = null)
            where ViewModel : IEditorViewModel
            where View : Control
        {
            var viewModelType = typeof(ViewModel);
            var viewType = typeof(View);

            _logger.Here().Information($"Registering new tool - {viewModelType}, {viewType}");
            if (_viewModelToViewMap.ContainsKey(viewModelType))
            {
                var errorMessage = $"Tool already registered - {viewModelType}";
                _logger.Here().Error(errorMessage);
                throw new Exception(errorMessage);
            }

            _viewModelToViewMap[viewModelType] = viewType;

            if (toolSelector != null)
                _extentionToToolMap[toolSelector] = viewModelType;
        }

        public Type GetViewTypeFromViewModel(Type viewModelType)
        {
            _logger.Here().Information($"Getting view for ViewModel - {viewModelType}");
            return _viewModelToViewMap[viewModelType];
        }

        public IEditorViewModel Create(string fullFileName, bool useDefaultTool = false)
        {
            var allEditors = GetAllPossibleEditors(fullFileName);

            if (allEditors.Count == 0)
            {
                _logger.Here().Warning($"Trying to open file {fullFileName}, but there are no valid tools for it.");
                return null;
            }

            Type selectedEditor = null;
            if (allEditors.Count == 1 || useDefaultTool)
            {
                selectedEditor = allEditors.First().Type;
            }
            else
            {
                var selectedToolType = ToolSelectorWindow.CreateAndShow(allEditors.Select(x => x.EditorType));
                if (selectedToolType == EditorEnums.None)
                    return null;
                selectedEditor = allEditors.First(x => x.EditorType == selectedToolType).Type;
            }

            return CreateEditorInternal(selectedEditor);
        }

        IEditorViewModel CreateEditorInternal(Type editorType)
        {
            var scope = _serviceProvider.CreateScope();
            var instance = scope.ServiceProvider.GetService(editorType) as IEditorViewModel;

            var scopeResolverHint = instance as IEditorScopeResolverHint;
            if (scopeResolverHint != null)
            {
                var solver = scope.ServiceProvider.GetService(scopeResolverHint.GetScopeResolverType) as IScopeHelper;
                solver.ResolveGlobalServices(scope.ServiceProvider);
            }

            _scopeRepository.Add(instance, scope);

            return instance;
        }

        public Window CreateAsWindow(IEditorViewModel viewModel)
        {
            var toolView = _viewModelToViewMap[viewModel.GetType()];
            var instance = (Control)Activator.CreateInstance(toolView);

            Window newWindow = new Window();
            newWindow.Content = instance;
            newWindow.DataContext = viewModel;

            return newWindow;
        }

        List<ToolInformation> GetAllPossibleEditors(string filename)
        {
            var output = new List<ToolInformation>();
            foreach (var toolLoopUp in _extentionToToolMap)
            {
                var result = toolLoopUp.Key.CanOpen(filename);
                if (result.CanOpen)
                    output.Add(new ToolInformation() { EditorType = toolLoopUp.Key.EditorType, IsCoreTool = result.IsCoreTool, Type = toolLoopUp.Value });
            }

            if (output.Count == 0)
            {
                var error = $"Attempting to get view model for file {filename}, unable to find tool based on extension";
                _logger.Here().Error(error);
                return new List<ToolInformation>();
            }

            return output.OrderBy(x => x.IsCoreTool).ToList();
        }

        public ViewModel Create<ViewModel>() 
            where ViewModel : IEditorViewModel
        {
            return (ViewModel)CreateEditorInternal(typeof(ViewModel));

        }

        public void DestroyEditor(IEditorViewModel instance) => _scopeRepository.RemoveScope(instance);

    }

    class ToolInformation
    {
        public EditorEnums EditorType { get; set; }
        public bool IsCoreTool { get; set; } = false;
        public Type Type { get; set; }
    }

    public class PackFileToToolSelectorResult
    {
        public bool CanOpen { get; set; } = false;
        public bool IsCoreTool { get; set; } = false;
    }

    public interface IPackFileToToolSelector
    {
        PackFileToToolSelectorResult CanOpen(string fullPath);
        EditorEnums EditorType { get; }
    }


    public interface IScopeHelper
    {
        void ResolveGlobalServices(IServiceProvider serviceProvider);
    }

    public interface IScopeHelper<T> : IScopeHelper
    {
       
    }
}
