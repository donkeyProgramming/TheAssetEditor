using Common;
using Common.ApplicationSettings;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace AssetEditor.Services
{

    public class ToolFactory : IToolFactory
    {
        ILogger _logger = Logging.Create<ToolFactory>();
        IServiceProvider _serviceProvider;

        Dictionary<Type, Type> _viewModelToViewMap = new Dictionary<Type, Type>();
        Dictionary<string, Type> _extentionToToolMap = new Dictionary<string, Type>();

        Type _defaultViewModelType;

        public ToolFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void RegisterTool<ViewModel, View>(params string[] extentions)
                   where ViewModel : IEditorViewModel
                   where View : Control
        {
            RegisterTool<ViewModel, View>();
            foreach (var extention in extentions)
            {
                var viewModelType = typeof(ViewModel);
                _extentionToToolMap[extention] = viewModelType;
            }
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
            var extention = Path.GetExtension(filename);
            if (_extentionToToolMap.ContainsKey(extention))
            {
                var instance = (IEditorViewModel)_serviceProvider.GetService(_extentionToToolMap[extention]);
                return instance;
            }
            else if (_defaultViewModelType != null)
            {
                var instance = (IEditorViewModel)_serviceProvider.GetService(_defaultViewModelType);
                return instance;
            }
            else
            {
                var error = $"Attempting to get view model for file {filename}, unable to find tool based on extention";
                _logger.Here().Error(error);
                return null;
            }
        }
    }
}
