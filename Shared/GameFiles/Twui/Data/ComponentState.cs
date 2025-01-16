namespace Shared.GameFormats.Twui.Data
{
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


}


