using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.Twui.Editor.ComponentEditor;
using Editors.Twui.Editor.Rendering;
using GameWorld.Core.WpfWindow;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.ToolCreation;

namespace Editors.Twui.Editor
{
    public partial class TwuiEditor : ObservableObject, IEditorInterface, ISaveableEditor, IFileEditor
    {
        private readonly IEventHub _eventHub;
        private readonly TwuiRenderComponent _renderComponent;

        [ObservableProperty] string _displayName = "Twui Editor";

        public bool HasUnsavedChanges { get; set; } = false;
        public PackFile CurrentFile { get; set; }

        [ObservableProperty] public partial TwuiContext? ParsedTwuiFile { get; set; }
        [ObservableProperty] public partial string HierarchyFilter { get; set; } = "";
        [ObservableProperty] ComponentManger _componentManager;
        [ObservableProperty] IWpfGame _scene;
        private readonly IPackFileService _packFileService;

        public TwuiEditor(IEventHub eventHub, ComponentManger componentEditor, TwuiRenderComponent renderComponent, IWpfGame wpfGame, IPackFileService packFileService)
        {
            _eventHub = eventHub;
            _componentManager = componentEditor;
            _renderComponent = renderComponent;
            _scene = wpfGame;
            _packFileService = packFileService;
            wpfGame.ForceEnsureCreated();
            renderComponent.Initialize();

            wpfGame.AddComponent(renderComponent);
        }

        public bool Save() { return true; }
        public void Close() { }

        public void LoadFile(PackFile file)
        {
            if (file == CurrentFile)
                return;

            DisplayName = "Twui Editor:" + Path.GetFileName(file.Name);

            var contextBuilder = new ContextBuilder(_packFileService);
            ParsedTwuiFile = contextBuilder.Create(file);

            ComponentManager.SetFile(ParsedTwuiFile);
            _renderComponent.SetFile(ParsedTwuiFile);
            ApplyHierarchyFilter();
        }

        /// <summary>Shows components whose name contains the filter text, along with their
        /// parents so they can still be reached in the tree. An empty filter shows everything.</summary>
        [RelayCommand]
        public void ApplyHierarchyFilter()
        {
            if (ParsedTwuiFile == null)
                return;

            var filter = HierarchyFilter.Trim();
            foreach (var component in ParsedTwuiFile.Componenets)
                ApplyHierarchyFilter(component, filter);
        }

        static bool ApplyHierarchyFilter(TwuiComponent component, string filter)
        {
            var anyChildVisible = false;
            foreach (var child in component.Children ?? [])
                anyChildVisible |= ApplyHierarchyFilter(child, filter);

            component.IsVisibleInHierarchy = filter.Length == 0
                || anyChildVisible
                || component.Name.Contains(filter, StringComparison.OrdinalIgnoreCase);
            return component.IsVisibleInHierarchy;
        }
    }
}
