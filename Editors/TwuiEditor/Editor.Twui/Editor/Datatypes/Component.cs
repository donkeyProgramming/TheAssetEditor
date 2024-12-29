using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Shared.Ui.BaseDialogs.MathViews;

namespace Editors.Twui.Editor.Datatypes
{
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


