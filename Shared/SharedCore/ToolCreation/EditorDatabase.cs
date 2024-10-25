using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Shared.Core.DependencyInjection;
using Shared.Core.ErrorHandling;

namespace Shared.Core.ToolCreation
{
    public class EditorInfo
    {
        public record ExtentionInfo(string Extention, int Priority);

        public EditorInfo(EditorEnums editorEnum, Type view, Type viewModel)
        {
            EditorEnum = editorEnum;
            View = view;
            ViewModel = viewModel;
        }
        public List<ExtentionInfo> Extensions { get; set; } = new List<ExtentionInfo>();
        public List<string> FolderRules { get; set; } = new List<string>();
        public string ToolbarName { get; set; } = "";
        public bool AddToolbarButton { get; set; } = false;
        public bool IsToolbarButtonEnabled { get; set; } = false;
        public EditorEnums EditorEnum { get; }
        public Type View { get; }
        public Type ViewModel { get; }
    }


    public static class EditorInfoPriorites
    {
        public static int Low => 0;
        public static int Default => 50;
        public static int High => 100;
    }

    public class EditorInfoBuilder
    {
        protected EditorInfo _instance;
        public static EditorInfoBuilder Create<TViewModel, TView>(EditorEnums editorType) where TViewModel : IEditorViewModel
        {
            return new EditorInfoBuilder()
            {
                _instance = new EditorInfo(editorType, typeof(TView), typeof(TViewModel))
            };
        }

        public EditorInfoBuilder AddToToolbar(string toolbarLabel, bool enabled = true)
        {
            _instance.ToolbarName = toolbarLabel;
            _instance.AddToolbarButton = true;
            _instance.IsToolbarButtonEnabled = enabled;
            return this;
        }

        public EditorInfoBuilder AddExtention(string extention, int priority)
        {
            // Ensure type is IFileEditor

            _instance.Extensions.Add(new EditorInfo.ExtentionInfo(extention.Trim().ToLower(), priority));
            return this;
        }

        public EditorInfoBuilder ValidForFoldersContaining(string filter)
        {
            _instance.FolderRules.Add(filter.Trim().ToLower());
            return this;
        }

        public void Build(IEditorDatabase editorDatabase)
        {
            _instance.Extensions = _instance.Extensions.OrderBy(x => x.Priority).ToList();
            editorDatabase.Register(_instance);
        }
    }

    public interface IEditorDatabase
    {
        public void Register(EditorInfo editorInfo);

        IEditorViewModel Create(string fullFileName, EditorEnums? preferedEditor = null);
        IEditorViewModel Create(EditorEnums editorEnum);

        void DestroyEditor(IEditorViewModel instance);
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

        public IEditorViewModel Create(EditorEnums editorEnum) 
        {
            var editor = _editors.First(x => x.EditorEnum == editorEnum);
            return CreateEditorInternal(editor.ViewModel);
        }

        public IEditorViewModel Create(string fullFileName, EditorEnums? preferedEditor)
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

        IEditorViewModel CreateEditorInternal(Type editorType)
        {
            var scope = _serviceProvider.CreateScope();
            var instance = scope.ServiceProvider.GetRequiredService(editorType) as IEditorViewModel;
            if (instance == null)
                throw new Exception($"Type '{editorType}' is not a IEditorViewModel");
            _scopeRepository.Add(instance, scope);
            return instance;
        }

        List<EditorInfo> GetAllPossibleEditors(string fullFileName)
        {
            var extention = Regex.Match(fullFileName, @"\..*").Value;
            if (extention.Contains("{") && extention.Contains("}"))
            {
                var ext2 = Regex.Match(extention, @"\..*\.(.*)\.(.*)");
                if (ext2.Success)
                {
                    extention = "." + ext2.Groups[1].Value + "." + ext2.Groups[2].Value;
                }
            }

            var output = new List<(EditorInfo info, int priority)>();
            foreach (var toolLookUp in _editors)
            {
                var hasValidExtention = false;
         
                var priority = -1;

                foreach (var toolExtention in toolLookUp.Extensions)
                {
                    if (toolExtention.Extention == extention)
                    {
                        if (toolExtention.Priority > priority)
                        {
                            hasValidExtention = true;
                            priority = toolExtention.Priority;
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
                    foreach (var toolExtention in toolLookUp.FolderRules)
                    {
                        if (fullFileName.Contains(toolExtention))
                            isValidForFolder = true;
                    }
                }
 
                if (hasValidExtention && isValidForFolder)
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

        public void DestroyEditor(IEditorViewModel instance) => _scopeRepository.RemoveScope(instance);
    }
}
