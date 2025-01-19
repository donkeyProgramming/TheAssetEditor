using Microsoft.Xna.Framework;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.Twui;
using Shared.GameFormats.Twui.Data;
using System.Collections.ObjectModel;
using Xceed.Wpf.AvalonDock.Layout;
using Xceed.Wpf.Toolkit.PropertyGrid.Converters;

namespace Editors.Twui.Editor
{
    public class ContextBuilder
    {
        private readonly IPackFileService _packFileService;

        public ContextBuilder(IPackFileService packFileService)
        {
            _packFileService = packFileService;
        }

        public TwuiContext Create(PackFile packFile, bool isTemplate = false)
        {
            var serializer = new TwuiSerializer();
            var parsedFile = serializer.Load(packFile);
            var components = CreateComponents(parsedFile.Hierarchy.RootItems, parsedFile.Components, isTemplate);

            var output = new TwuiContext()
            {
                FileName = packFile.Name,
                Componenets = components
            };

            return output;
        }

        ObservableCollection<TwuiComponent> CreateComponents(List<HierarchyItem> hierarchyItems, List<Component> components, bool isTemplate)
        {
            ObservableCollection<TwuiComponent> output = [];

            foreach (var item in hierarchyItems)
            {
                var fileComponent = components.FirstOrDefault(x => x.This == item.Id);
                if (fileComponent == null)
                    continue;

                var componentLocation = new TwuiLocation()
                {
                    DockingHorizontal = fileComponent.DockingHorizontal,
                    DockingVertical = fileComponent.DockingVertical,
                    Component_anchor_point = fileComponent.Component_anchor_point,
                    Dimensions = fileComponent.Dimensions,
                    Dock_offset = fileComponent.Dock_offset,
                    Offset = fileComponent.Offset
                };

                var stateList = new ObservableCollection<TwuiComponentState>();
                foreach (var fileState in fileComponent.States)
                {
                    var imageList = new ObservableCollection<TwuiComponentImage>();
                    foreach (var fileImageState in fileState.Images)
                    {
                        var imageLocation = new TwuiLocation()
                        {
                            Dimensions = new Vector2(fileImageState.Width, fileImageState.Height),
                            Component_anchor_point = new Vector2(0.5f, 0.5f),
                            DockingHorizontal = fileImageState.DockingHorizontal,
                            DockingVertical = fileImageState.DockingVertical,
                            Dock_offset = fileImageState.Dock_offset,
                            Offset = fileImageState.Offset
                        };

                        var image = new TwuiComponentImage()
                        {
                            Id = fileImageState.UniqueGuid,
                            Location = imageLocation,
                            Path = fileComponent.ComponentImages.FirstOrDefault(x => x.This == fileImageState.Componentimage)?.ImagePath
                        };
                        imageList.Add(image);
                    }

                    var state = new TwuiComponentState()
                    {
                        Height = fileState.Height,
                        Width = fileState.Width,
                        Id = fileState.This,
                        Name = fileState.Name,
                        ImageList = imageList
                    };

                    stateList.Add(state);
                }

                var component = new TwuiComponent()
                {
                    Name = item.Name,
                    Id = fileComponent.This,
                    Location = componentLocation,
                    Priority = fileComponent.Priority,
                    IsPartOfTemplate = isTemplate,
                    TemplateName = fileComponent.Template_id,
                    


                    States = stateList,
                    CurrentState = stateList.FirstOrDefault(x => x.Id == fileComponent.Currentstate)
                };

                var children = CreateComponents(item.Children, components, isTemplate);


                // Resolve Templates
                if (string.IsNullOrWhiteSpace(component.TemplateName) == false)
                {   
                    //ui/templates/round_small_button.twui.xml
                    var templatePackFile = _packFileService.FindFile($"ui\\templates\\{component.TemplateName}.twui.xml");
                    if (templatePackFile != null)
                    {
                    

                        var templateContext = Create(templatePackFile, true);

                        if (component.TemplateName == "round_small_button")
                        {

                        }
        
                        // Skip the root node and merge! 
                      //  Todo!
                        var templateComponentsToAdd = templateContext.Componenets.FirstOrDefault();
                        if (templateComponentsToAdd != null)
                        {
                        
                            foreach (var templateChild in templateComponentsToAdd.Children)
                            {
                                templateChild.Location.Offset = new Vector2(0, 0);
                                children.Add(templateChild);
                            }
                        }
                    }
                }

                component.Children = children;
                output.Add(component);
            }

            return output;
        }
    }
}
