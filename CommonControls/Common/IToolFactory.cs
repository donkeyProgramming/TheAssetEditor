using Common;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace CommonControls.Common
{
    public interface IPackFileToToolSelector
    {
        bool CanOpen(string fullPath);
    }

    public class ExtentionToTool : IPackFileToToolSelector
    {
        string[] _validExtentions;

        public ExtentionToTool(params string[] extentions)
        {
            _validExtentions = extentions;
        }

        public bool CanOpen(string fullPath)
        {
            var extention = Regex.Match(fullPath, @"\..*").Value;
            foreach (var validExt in _validExtentions)
            {
                if (validExt == extention)
                    return true;
            }

            return false;
        }
    }

    public class PathToTool : IPackFileToToolSelector
    {
        string _extention;
        string _requiredPathSubString;

        public PathToTool(string extention, string requiredPathSubString)
        {
            _extention = extention;
            _requiredPathSubString = requiredPathSubString;
        }

        public bool CanOpen(string fullPath)
        {
            var extention = Regex.Match(fullPath, @"\..*").Value;
            if (_extention == extention && fullPath.Contains(_requiredPathSubString))
                return true;
            return false;
        }
    }

    public interface IToolFactory
    {
        void RegisterTool<ViewModel, View>(IPackFileToToolSelector toolSelector)
               where ViewModel : IEditorViewModel
               where View : Control;

        void RegisterToolAsDefault<ViewModel, View>()
           where ViewModel : IEditorViewModel
           where View : Control;

        void RegisterTool<ViewModel, View>()
            where ViewModel : IEditorViewModel
            where View : Control;


        ViewModel CreateEditorViewModel<ViewModel>()
            where ViewModel : IEditorViewModel;

        Window CreateToolAsWindow(IEditorViewModel viewModel);
        IEditorViewModel GetToolViewModelFromFileName(string filename);
    }

    public class ToolFactory : IToolFactory
    {
        ILogger _logger = Logging.Create<ToolFactory>();
        IServiceProvider _serviceProvider;

        Dictionary<Type, Type> _viewModelToViewMap = new Dictionary<Type, Type>();
        Dictionary<IPackFileToToolSelector, Type> _extentionToToolMap = new Dictionary<IPackFileToToolSelector, Type>();

        Type _defaultViewModelType;

        public ToolFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void RegisterTool<ViewModel, View>(IPackFileToToolSelector toolSelector)
                   where ViewModel : IEditorViewModel
                   where View : Control
        {
            RegisterTool<ViewModel, View>();

            var viewModelType = typeof(ViewModel);
            _extentionToToolMap[toolSelector] = viewModelType;

        }

        public void RegisterToolAsDefault<ViewModel, View>()
           where ViewModel : IEditorViewModel
           where View : Control
        {
            RegisterTool<ViewModel, View>();
            _defaultViewModelType = typeof(ViewModel);
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

        public IEditorViewModel GetToolViewModelFromFileName(string filename)
        {
            foreach (var toolLoopUp in _extentionToToolMap)
            {
                if (toolLoopUp.Key.CanOpen(filename))
                {
                    var instance = (IEditorViewModel)_serviceProvider.GetService(toolLoopUp.Value);
                    return instance;
                }
            }

            if (_defaultViewModelType != null)
            {
                var instance = (IEditorViewModel)_serviceProvider.GetService(_defaultViewModelType);
                return instance;
            }

            var error = $"Attempting to get view model for file {filename}, unable to find tool based on extention";
            _logger.Here().Error(error);
            return null;
        }

        public ViewModel CreateEditorViewModel<ViewModel>() where ViewModel : IEditorViewModel
        {
            var instance = (ViewModel)_serviceProvider.GetService(typeof(ViewModel));
            return instance;
        }
    }
}
