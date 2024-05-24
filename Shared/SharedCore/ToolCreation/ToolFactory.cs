using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Shared.Core.DependencyInjection;
using Shared.Core.ErrorHandling;

namespace Shared.Core.ToolCreation
{
    public class ToolFactory : IToolFactory
    {
        private readonly ILogger _logger = Logging.Create<ToolFactory>();

        private readonly IServiceProvider _rootProvider;
        private readonly ScopeRepository _scopeRepository;
        private readonly IToolSelectorUiProvider _toolSelectorUiProvider;
        private readonly Dictionary<Type, Type> _viewModelToViewMap = new();
        private readonly Dictionary<IPackFileToToolSelector, Type> _extensionToToolMap = new();

        public ToolFactory(IServiceProvider serviceProvider, ScopeRepository scopeRepository, IToolSelectorUiProvider toolSelectorUiProvider)
        {
            _rootProvider = serviceProvider;
            _scopeRepository = scopeRepository;
            _toolSelectorUiProvider = toolSelectorUiProvider;
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
                _extensionToToolMap[toolSelector] = viewModelType;
        }

        public Type GetViewTypeFromViewModel(Type viewModelType)
        {
            _logger.Here().Information($"Getting view for ViewModel - {viewModelType}");
            return _viewModelToViewMap[viewModelType];
        }

        public ViewModel Create<ViewModel>() where ViewModel : IEditorViewModel
        {
            return (ViewModel)CreateEditorInternal(typeof(ViewModel));
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
                var selectedToolType = _toolSelectorUiProvider.CreateAndShow(allEditors.Select(x => x.EditorType));
                if (selectedToolType == EditorEnums.None)
                    return null;
                selectedEditor = allEditors.First(x => x.EditorType == selectedToolType).Type;
            }

            return CreateEditorInternal(selectedEditor);
        }

        IEditorViewModel CreateEditorInternal(Type editorType)
        {
            var scope = _rootProvider.CreateScope();
            var instance = scope.ServiceProvider.GetRequiredService(editorType) as IEditorViewModel;

            var scopeResolverHint = instance as IEditorScopeResolverHint;
            if (scopeResolverHint != null)
            {
                var solver = scope.ServiceProvider.GetRequiredService(scopeResolverHint.GetScopeResolverType) as IScopeHelper;
                solver.ResolveGlobalServices(scope.ServiceProvider);
            }

            _scopeRepository.Add(instance, scope);
            return instance;
        }

        public Window CreateAsWindow(IEditorViewModel viewModel)
        {
            var toolView = _viewModelToViewMap[viewModel.GetType()];
            var instance = (Control)Activator.CreateInstance(toolView);

            var newWindow = new Window();
            newWindow.Content = instance;
            newWindow.DataContext = viewModel;

            return newWindow;
        }

        List<ToolInformation> GetAllPossibleEditors(string filename)
        {
            var output = new List<ToolInformation>();
            foreach (var toolLoopUp in _extensionToToolMap)
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

        public void DestroyEditor(IEditorViewModel instance) => _scopeRepository.RemoveScope(instance);
    }
}
