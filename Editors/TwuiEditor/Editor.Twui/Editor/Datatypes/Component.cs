using System.Collections.ObjectModel;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Shared.Ui.BaseDialogs.MathViews;

namespace Editors.Twui.Editor.Datatypes
{
    public partial class Component : ObservableObject
    {
        [ObservableProperty] string _name  = string.Empty;
        [ObservableProperty] string _this= string.Empty;
        [ObservableProperty] string _id  = string.Empty;
        [ObservableProperty] bool _partOfTemplate = false;
        [ObservableProperty] string _uniqueguid_in_template = string.Empty;
        [ObservableProperty] string _uniqueguid = string.Empty;
        [ObservableProperty] Vector2ViewModel _dimensions = new(0, 0);
        [ObservableProperty] string _dock_point = string.Empty;
        [ObservableProperty] bool _tooltips_localised = false;
        [ObservableProperty] Vector2ViewModel _offset = new(0, 0);
        [ObservableProperty] float _priority = 100;
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

            return output; ;
        }


        private static Component SerializeComponent(XElement componentNode)
        {
            var output = new Component();
            output.Name = componentNode.Name.LocalName;
            output.This = (string)componentNode.Attribute("this");
            output.Id = (string)componentNode.Attribute("id");

            return output;
        }
    }
}
