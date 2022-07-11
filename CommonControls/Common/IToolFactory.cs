using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace CommonControls.Common
{
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

        public EditorEnums EditorType {get; private set;}

        public PackFileToToolSelectorResult CanOpen(string fullPath)
        {
            var extention = Regex.Match(fullPath, @"\..*").Value;
            if (extention.Contains("{") && extention.Contains("}"))
            {
                var index = extention.IndexOf("}");
                extention = extention.Remove(0, index+1);
            }

            if (_validExtentionsCore != null)
            {
                foreach (var validExt in _validExtentionsCore)
                {
                    if (validExt == extention)
                        return new PackFileToToolSelectorResult() { CanOpen = true, IsCoreTool = true};
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
        void RegisterFileTool<ViewModel, View>(IPackFileToToolSelector toolSelector)
               where ViewModel : IEditorViewModel
               where View : Control;

        void RegisterTool<ViewModel, View>()
            where ViewModel : IEditorViewModel
            where View : Control;


        ViewModel CreateEditorViewModel<ViewModel>()
            where ViewModel : IEditorViewModel;

        Window CreateToolAsWindow(IEditorViewModel viewModel);
        IEditorViewModel GetDefaultToolViewModelFromFileName(string filename);
    }

    public class ToolFactory : IToolFactory
    {
        ILogger _logger = Logging.Create<ToolFactory>();
        IServiceProvider _serviceProvider;

        Dictionary<Type, Type> _viewModelToViewMap = new Dictionary<Type, Type>();
        Dictionary<IPackFileToToolSelector, Type> _extentionToToolMap = new Dictionary<IPackFileToToolSelector, Type>();

        public ToolFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void RegisterFileTool<ViewModel, View>(IPackFileToToolSelector toolSelector)
                   where ViewModel : IEditorViewModel
                   where View : Control
        {
            RegisterTool<ViewModel, View>();

            var viewModelType = typeof(ViewModel);
            _extentionToToolMap[toolSelector] = viewModelType;

        }

        public void RegisterTool<ViewModel, View>()
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
        }

        public Type GetViewTypeFromViewModel(Type viewModelType)
        {
            _logger.Here().Information($"Getting view for ViewModel - {viewModelType}");
            return _viewModelToViewMap[viewModelType];
        }

        public Window CreateToolAsWindow(IEditorViewModel viewModel)
        {
            var toolView = _viewModelToViewMap[viewModel.GetType()];
            var instance = (Control)Activator.CreateInstance(toolView);

            Window newWindow = new Window();
            newWindow.Content = instance;
            newWindow.DataContext = viewModel;

            return newWindow;
        }

        public Window CreateToolAsWindow<ViewModel>(out IEditorViewModel viewModelInstance)
            where ViewModel : IEditorViewModel
        {
            var viewType = _viewModelToViewMap[typeof(ViewModel)];
            var viewModelType = typeof(ViewModel);

            var view = _serviceProvider.GetService(viewType);
            viewModelInstance = _serviceProvider.GetService(viewModelType) as IEditorViewModel;

            Window newWindow = new Window();
            newWindow.Content = view;
            newWindow.DataContext = viewModelInstance;

            return newWindow;
        }

        public IEditorViewModel GetDefaultToolViewModelFromFileName(string filename)
        {
            foreach (var toolLoopUp in _extentionToToolMap)
            {
                var result = toolLoopUp.Key.CanOpen(filename);
                if (result.CanOpen && result.IsCoreTool)
                {
                    var instance = (IEditorViewModel)_serviceProvider.GetService(toolLoopUp.Value);
                    return instance;
                }
            }

            var error = $"Attempting to get view model for file {filename}, unable to find tool based on extention";
            _logger.Here().Error(error);
            return null;
        }

        public List<ToolInformation> GetAllToolViewModelFromFileName(string filename)
        {
            var output = new List<ToolInformation>();
            foreach (var toolLoopUp in _extentionToToolMap)
            {
                var result = toolLoopUp.Key.CanOpen(filename);
                if (result.CanOpen)
                    output.Add(new ToolInformation() { EditorType = toolLoopUp.Key .EditorType, IsCoreTool = result.IsCoreTool, Type = toolLoopUp.Value});
            }

            if (output.Count == 0)
            {
                var error = $"Attempting to get view model for file {filename}, unable to find tool based on extention";
                _logger.Here().Error(error);
                return new List<ToolInformation>();
            }

            return output.OrderBy(x=>x.IsCoreTool).ToList();
        }

        public IEditorViewModel CreateFromType(Type type)
        {
            var instance = (IEditorViewModel)_serviceProvider.GetService(type);
            return instance;
        }

        public ViewModel CreateEditorViewModel<ViewModel>() where ViewModel : IEditorViewModel
        {
            var instance = (ViewModel)_serviceProvider.GetService(typeof(ViewModel));
            return instance;
        }
    }

    public class ToolInformation
    { 
        public EditorEnums EditorType { get; set; }
        public bool IsCoreTool { get; set; } = false;
        public Type Type { get; set; }
    }
}
