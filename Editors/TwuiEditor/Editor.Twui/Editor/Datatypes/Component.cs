using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using System.Xml.XPath;
using CommunityToolkit.Mvvm.ComponentModel;
using Shared.Ui.BaseDialogs.MathViews;

namespace Editors.Twui.Editor.Datatypes
{

    public partial class ComponentImage : ObservableObject
    {
        [ObservableProperty] public partial string This { get; set; } = string.Empty;
        [ObservableProperty] public partial string UniqueGuid { get; set; } = string.Empty;
        [ObservableProperty] public partial string ImagePath { get; set; } = string.Empty;
    }

    public partial class State : ObservableObject
    {
        [ObservableProperty] public partial string This { get; set; } = string.Empty;
        [ObservableProperty] public partial string Name { get; set; } = string.Empty;
        [ObservableProperty] public partial float Width { get; set; } = 0;
        [ObservableProperty] public partial float Height { get; set; } = 0;
        [ObservableProperty] public partial bool Interactive { get; set; } = false;
        [ObservableProperty] public partial string UniqueGuid { get; set; } = string.Empty;

        [ObservableProperty] public partial ObservableCollection<StateImage> Images { get; set; } = [];

        //<component_text
        /*
         					<component_text
						text="Rewards"
						textvalign="Center"
						texthalign="Center"
						textlocalised="true"
						textlabel="StateText_52b2fba0"
						font_m_font_name="Norse-Bold"
						font_m_size="16"
						font_m_colour="#4A0000FF"
						fontcat_name="grudges_subheader"/>
         */
    }


    public partial class StateImage : ObservableObject
    {
        [ObservableProperty] public partial string This { get; set; } = string.Empty;
        [ObservableProperty] public partial string UniqueGuid { get; set; } = string.Empty;
        [ObservableProperty] public partial string Componentimage { get; set; } = string.Empty;
        [ObservableProperty] public partial float Width { get; set; } = 0;
        [ObservableProperty] public partial float Height { get; set; } = 0;
        [ObservableProperty] public partial string Colour { get; set; } = string.Empty;
    }


    public partial class Component : ObservableObject
    {
        [ObservableProperty]public partial string This { get; set; } = string.Empty;
        [ObservableProperty]public partial string Id { get; set; } = string.Empty;
        [ObservableProperty]public partial bool PartOfTemplate { get; set; } = false;
        [ObservableProperty]public partial string Uniqueguid_in_template { get; set; } = string.Empty;
        [ObservableProperty]public partial string Uniqueguid { get; set; } = string.Empty;
        [ObservableProperty]public partial Vector2ViewModel Dimensions { get; set; } = new(0, 0);
        [ObservableProperty]public partial string Dock_point { get; set; } = string.Empty;
        [ObservableProperty]public partial bool Tooltips_localised { get; set; } = false;
        [ObservableProperty]public partial Vector2ViewModel Offset { get; set; } = new(0, 0);
        [ObservableProperty]public partial float Priority { get; set; } = 100;
        [ObservableProperty]public partial Vector2ViewModel Component_anchor_point { get; set; } = new(0, 0);
        [ObservableProperty]public partial Vector2ViewModel Dock_offset { get; set; } = new(0, 0);
        [ObservableProperty]public partial string Defaultstate { get; set; } = string.Empty;
        [ObservableProperty]public partial string Currentstate { get; set; } = string.Empty;
        [ObservableProperty]public partial bool Allowhorizontalresize { get; set; } = false;
        [ObservableProperty]public partial bool Allowverticalresize { get; set; } = false;


        [ObservableProperty] public partial ObservableCollection<ComponentImage> ComponentImages { get; set; } = [];
        [ObservableProperty] public partial ObservableCollection<State> States { get; set; } = [];

        //LayoutEngine
    }


    public class ComponentSerializer
    {
        internal static ObservableCollection<Component> Serialize(XElement componentsNode)
        {
            var output = new ObservableCollection<Component>();
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

            output.This = AssignAttribute(output.This, componentNode);
            output.Id = AssignAttribute(output.Id, componentNode);
            output.PartOfTemplate = AssignAttribute(output.PartOfTemplate, componentNode);
            output.Uniqueguid_in_template = AssignAttribute(output.Uniqueguid_in_template, componentNode);
            output.Uniqueguid = AssignAttribute(output.Uniqueguid, componentNode);
            output.Dimensions = AssignAttribute(output.Dimensions, componentNode);
            output.Dock_point = AssignAttribute(output.Dock_point, componentNode);
            output.Tooltips_localised = AssignAttribute(output.Tooltips_localised, componentNode);
            output.Offset = AssignAttribute(output.Offset, componentNode);
            output.Priority = AssignAttribute(output.Priority, componentNode);
            output.Component_anchor_point = AssignAttribute(output.Component_anchor_point, componentNode);
            output.Dock_offset = AssignAttribute(output.Dock_offset, componentNode);
            output.Defaultstate = AssignAttribute(output.Defaultstate, componentNode);
            output.Currentstate = AssignAttribute(output.Currentstate, componentNode);
            output.Allowhorizontalresize = AssignAttribute(output.Allowhorizontalresize, componentNode);
            output.Allowverticalresize = AssignAttribute(output.Allowverticalresize, componentNode);

            if (output.Id == "root")
            { 
            
            }

            var states = componentNode.Element("states");
            output.States = SerializeState(states);

            var componentImageNodes = componentNode.Element("componentimages");
            output.ComponentImages = SerializeComponentImage(componentImageNodes);


            return output;
        }

        static ObservableCollection<ComponentImage> SerializeComponentImage(XElement? xmlStatesNode)
        {
            if (xmlStatesNode == null)
                return [];
            
            var output = new ObservableCollection<ComponentImage>();
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

        static ObservableCollection<State> SerializeState(XElement? xmlStatesNode)
        {
            if (xmlStatesNode == null)
                return [];

            var output = new ObservableCollection<State>();
            var xmlAllStateNodes = xmlStatesNode.Elements().ToList();
            foreach (var xmlState in xmlAllStateNodes)
            {
                var state = new State();
                state.This = AssignAttribute(state.This, xmlState);
                state.Name = AssignAttribute(state.Name, xmlState);
                state.Width = AssignAttribute(state.Width, xmlState);
                state.Height = AssignAttribute(state.Height, xmlState);
                state.Interactive = AssignAttribute(state.Interactive, xmlState);
                state.UniqueGuid = AssignAttribute(state.UniqueGuid, xmlState);

                // Handle images
                var xmlStateImageRoot = xmlState.Element("imagemetrics")?.Elements();
                if (xmlStateImageRoot != null)
                {
                    foreach (var xmlStateImage in xmlStateImageRoot)
                    {
                        var stateImage = new StateImage();
                        stateImage.This = AssignAttribute(stateImage.This, xmlStateImage);
                        stateImage.UniqueGuid = AssignAttribute(stateImage.UniqueGuid, xmlStateImage);
                        stateImage.Componentimage = AssignAttribute(stateImage.Componentimage, xmlStateImage);
                        stateImage.Width = AssignAttribute(stateImage.Width, xmlStateImage);
                        stateImage.Height = AssignAttribute(stateImage.Height, xmlStateImage);
                        stateImage.Colour = AssignAttribute(stateImage.Colour, xmlStateImage);

                        state.Images.Add(stateImage);
                    }
                }

                output.Add(state);
            }


            return output;
        }



        static string AssignAttribute(string value, XElement xmlNode, [CallerArgumentExpression("value")] string variableName = null)
        {
            var attributeContent = GetAttributeContent(variableName, xmlNode);
            if (attributeContent == null)
                return value;

            return (string)attributeContent;
        }

        static float AssignAttribute(float value, XElement xmlNode, [CallerArgumentExpression("value")] string variableName = null)
        {
            var attributeContent = GetAttributeContent(variableName, xmlNode);
            if (attributeContent == null)
                return value;

            return (float)attributeContent;
        }

        static bool AssignAttribute(bool value, XElement xmlNode, [CallerArgumentExpression("value")] string variableName = null)
        {
            var attributeContent = GetAttributeContent(variableName, xmlNode);
            if (attributeContent == null)
                return value;

            return (bool)attributeContent;
        }

        static Vector2ViewModel AssignAttribute(Vector2ViewModel value, XElement xmlNode, [CallerArgumentExpression("value")] string variableName = null)
        {
            var attributeContent = GetAttributeContent(variableName, xmlNode);
            if (attributeContent == null)
                return value;
            var str = attributeContent.Value;
            var splitStr = str.Split(",");
            var v0 = float.Parse(splitStr[0]);
            var v1 = float.Parse(splitStr[1]);

            return new Vector2ViewModel(v0, v1);
        }



        static XAttribute? GetAttributeContent(string variableName, XElement xmlNode)
        {
            var attributeName = variableName.Split(".").Last();
            var attributeContent = xmlNode.Attribute(attributeName.ToLower());
            return attributeContent;
        }


        static Vector2ViewModel AssignAttribute(string attributeName, XElement xmlNode, Vector2ViewModel nodeDefaultValue)
        {
            var attributeContent = xmlNode.Attribute(attributeName.ToLower());
            if (attributeContent == null)
                return nodeDefaultValue;

            var str = attributeContent.Value;
            var splitStr = str.Split(",");
            var v0 = float.Parse(splitStr[0]);
            var v1 = float.Parse(splitStr[1]);

            return new Vector2ViewModel(v0, v1);
        }
    }
}


