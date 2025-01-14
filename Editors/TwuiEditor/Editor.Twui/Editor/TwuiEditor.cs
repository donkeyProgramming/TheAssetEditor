using System.ComponentModel.DataAnnotations;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using Editors.Twui.Editor.ComponentEditor;
using Editors.Twui.Editor.Events;
using Editors.Twui.Editor.Rendering;
using Shared.Core.Events;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;
using Shared.Core.ToolCreation;
using Shared.GameFormats.Twui;
using Shared.GameFormats.Twui.Data;

namespace Editors.Twui.Editor
{

    public class Hi : ObservableObject
    { 
    }


    public partial class TwuiEditor : ObservableObject, IEditorInterface, ISaveableEditor, IFileEditor
    {
        private readonly IEventHub _eventHub;

        [ObservableProperty] string _displayName = "Twui Editor";

        public bool HasUnsavedChanges { get; set; } = false;
        public PackFile CurrentFile { get; set; }

        [ObservableProperty] TwuiFile _parsedTwuiFile;
   
        [ObservableProperty] ComponentManger _componentManager;
        [ObservableProperty] IWpfGame _scene;

        public TwuiEditor(IEventHub eventHub, ComponentManger componentEditor, TwuiRenderComponent previewBuilder, IWpfGame wpfGame)
        {
            _eventHub = eventHub;
            _componentManager = componentEditor;
            _scene = wpfGame;

            wpfGame.ForceEnsureCreated();
            previewBuilder.Initialize();

            wpfGame.AddComponent(previewBuilder);
        }

        public bool Save() { return true; } 
        public void Close() { }

        public void LoadFile(PackFile file)
        {
            if (file == CurrentFile)
                return;

            var serializer = new TwuiSerializer();
            ParsedTwuiFile = serializer.Load(file);
            DisplayName = "Twui Editor:" + Path.GetFileName(file.Name);


            Hack_addVisualStuffForDebugging(ParsedTwuiFile.Components, ParsedTwuiFile.Hierarchy.RootItems);

            ComponentManager.SetFile(ParsedTwuiFile);
            _eventHub.Publish(new RedrawTwuiEvent(ParsedTwuiFile, null));



        }

        void Hack_addVisualStuffForDebugging(List<Component> components, List<HierarchyItem> rootItems)
        {
            foreach (var item in rootItems)
            {
                var comp = components.FirstOrDefault(x => x.This == item.Id);
                if(comp != null)
                    item.Priority = " - " + comp.Priority;

                Hack_addVisualStuffForDebugging(components, item.Children);
            }
        }

    }
}
