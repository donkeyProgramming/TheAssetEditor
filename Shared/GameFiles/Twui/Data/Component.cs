using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace Shared.GameFormats.Twui.Data
{

    // If values are not set, null is probably better? 
    // Stuff like Dimensions are not always set, and 0,0 is very different then not set
    [DebuggerDisplay("{Name} - {This}")]
    public class Component
    {
        public string Name { get; set; } = string.Empty;
        public string This { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public bool PartOfTemplate { get; set; } = false;
        public string Uniqueguid_in_template { get; set; } = string.Empty;
        public string Uniqueguid { get; set; } = string.Empty;
        public Vector2 Dimensions { get; set; } = new(0, 0);
        public string Dock_point { get; set; } = string.Empty;
        public bool Tooltips_localised { get; set; } = false;
        public Vector2 Offset { get; set; } = new(0, 0);
        public float Priority { get; set; } = 100;
        public Vector2 Component_anchor_point { get; set; } = new(0, 0);
        public Vector2 Dock_offset { get; set; } = new(0, 0);
        public string Defaultstate { get; set; } = string.Empty;
        public string Currentstate { get; set; } = string.Empty;
        public bool Allowhorizontalresize { get; set; } = false;
        public bool Allowverticalresize { get; set; } = false;

        public List<ComponentImage> ComponentImages { get; set; } = [];
        public List<ComponentState> States { get; set; } = [];

        //LayoutEngine
    }

    public class ComponentImage
    {
        public string This { get; set; } = string.Empty;
        public string UniqueGuid { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
    }

    public class ComponentState
    {
        public string This { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public float Width { get; set; } = 0;
        public float Height { get; set; } = 0;
        public bool Interactive { get; set; } = false;
        public string UniqueGuid { get; set; } = string.Empty;

        public List<ComponentStateImage> Images { get; set; } = [];

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


    public class ComponentStateImage
    {
        public string This { get; set; } = string.Empty;
        public string UniqueGuid { get; set; } = string.Empty;
        public string Componentimage { get; set; } = string.Empty;
        public float Width { get; set; } = 0;
        public float Height { get; set; } = 0;
        public string Colour { get; set; } = string.Empty;
    }


}


