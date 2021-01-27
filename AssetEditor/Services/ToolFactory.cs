using Common;
using Common.ApplicationSettings;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace AssetEditor.Services
{
    public class ToolFactory
    {
        ILogger _logger = Logging.Create<ToolFactory>();
        ApplicationSettingsService _applicationSettings;

        Dictionary<Type, Type> _viewModelToViewMap = new Dictionary<Type, Type>();
        Dictionary<string, Type> _extentionToToolMap = new Dictionary<string, Type>();

        Type _defaultViewModelType;

        public ToolFactory(ApplicationSettingsService applicationSettings)
        {
            _applicationSettings = applicationSettings;
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

        public IEditorViewModel GetToolViewModelFromFileName(string filename)
        {
            if (_defaultViewModelType != null)
            {
                var instance = (IEditorViewModel)Activator.CreateInstance(_defaultViewModelType);
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
