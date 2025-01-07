using System.Xml.Linq;
using Shared.GameFormats.Twui.Data;
using Shared.GameFormats.Twui.Data.DataTypes;

namespace Shared.GameFormats.Twui
{
    public class ComponentSerializer : BaseTwuiSerializer
    {
        internal static List<Component> Serialize(XElement? componentsNode)
        {
            if (componentsNode == null)
                return [];

            var output = new List<Component>();
            foreach (var componentNode in componentsNode.Elements())
            {
                var component = SerializeComponent(componentNode);
                output.Add(component);
            }

            return output;
        }

        private static Component SerializeComponent(XElement componentNode)
        {
            var output = new Component();
            output.Name = componentNode.Name.LocalName;
            output.This = AssignAttribute(output.This, componentNode);
            output.Id = AssignAttribute(output.Id, componentNode);
            output.PartOfTemplate = AssignAttribute(output.PartOfTemplate, componentNode);
            output.Uniqueguid_in_template = AssignAttribute(output.Uniqueguid_in_template, componentNode);
            output.Uniqueguid = AssignAttribute(output.Uniqueguid, componentNode);
            output.Dimensions = AssignAttribute(output.Dimensions, componentNode);
            output.Tooltips_localised = AssignAttribute(output.Tooltips_localised, componentNode);
            output.Offset = AssignAttribute(output.Offset, componentNode);
            output.Priority = AssignAttribute(output.Priority, componentNode);
            output.Component_anchor_point = AssignAttribute(output.Component_anchor_point, componentNode);
            output.Dock_offset = AssignAttribute(output.Dock_offset, componentNode);
            output.Defaultstate = AssignAttribute(output.Defaultstate, componentNode);
            output.Currentstate = AssignAttribute(output.Currentstate, componentNode);
            output.Allowhorizontalresize = AssignAttribute(output.Allowhorizontalresize, componentNode);
            output.Allowverticalresize = AssignAttribute(output.Allowverticalresize, componentNode);

            DockingParser.ConvertFrom(componentNode, out var horizontal, out var vertical);
            output.DockingVertical = vertical;
            output.DockingHorizontal = horizontal; 

            // States
            var states = componentNode.Element("states");
            output.States = SerializeComponentState(states);


            // Component images
            var componentImageNodes = componentNode.Element("componentimages");
            output.ComponentImages = SerializeComponentImage(componentImageNodes);

            return output;
        }

        static List<ComponentImage> SerializeComponentImage(XElement? xmlStatesNode)
        {
            if (xmlStatesNode == null)
                return [];

            var output = new List<ComponentImage>();
            var allComponentImages = xmlStatesNode.Elements().ToList();
            foreach (var xmlComponentImage in allComponentImages)
            {
                var image = new ComponentImage();
                image.This = AssignAttribute(image.This, xmlComponentImage);
                image.ImagePath = AssignAttribute(image.ImagePath, xmlComponentImage);
                image.UniqueGuid = AssignAttribute(image.UniqueGuid, xmlComponentImage);
                output.Add(image);
            }

            return output;
        }

        static List<ComponentState> SerializeComponentState(XElement? xmlStatesNode)
        {
            if (xmlStatesNode == null)
                return [];

            var output = new List<ComponentState>();
            var xmlAllStateNodes = xmlStatesNode.Elements().ToList();
            foreach (var xmlState in xmlAllStateNodes)
            {
                var state = new ComponentState();
                state.This = AssignAttribute(state.This, xmlState);
                state.Name = AssignAttribute(state.Name, xmlState);
                state.Width = AssignAttribute(state.Width, xmlState);
                state.Height = AssignAttribute(state.Height, xmlState);
                state.Interactive = AssignAttribute(state.Interactive, xmlState);
                state.UniqueGuid = AssignAttribute(state.UniqueGuid, xmlState);

                var xmlStateImageRoot = xmlState.Element("imagemetrics");
                state.Images = SerializeComponentStateImage(xmlStateImageRoot);
                output.Add(state);
            }

            return output;
        }

        static List<ComponentStateImage> SerializeComponentStateImage(XElement? xmlCompnentStateImage)
        {
            if (xmlCompnentStateImage == null)
                return [];

            var output = new List<ComponentStateImage>();
            var xmlStateImages = xmlCompnentStateImage.Elements();

            foreach (var xmlStateImage in xmlStateImages)
            {
                var stateImage = new ComponentStateImage();
                stateImage.This = AssignAttribute(stateImage.This, xmlStateImage);
                stateImage.UniqueGuid = AssignAttribute(stateImage.UniqueGuid, xmlStateImage);
                stateImage.Componentimage = AssignAttribute(stateImage.Componentimage, xmlStateImage);
                stateImage.Width = AssignAttribute(stateImage.Width, xmlStateImage);
                stateImage.Height = AssignAttribute(stateImage.Height, xmlStateImage);
                stateImage.Colour = AssignAttribute(stateImage.Colour, xmlStateImage);

                output.Add(stateImage);
            }

            return output;
        }
    }
}


