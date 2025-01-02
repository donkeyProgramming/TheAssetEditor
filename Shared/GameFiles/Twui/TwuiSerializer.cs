using System.Xml.Linq;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.Twui.Data;

namespace Shared.GameFormats.Twui
{
    public class TwuiSerializer
    {
       
        public TwuiSerializer()
        {




        }

        public TwuiFile Load(PackFile packFile)
        {
            ComponentSerializer componentSerializer = new ComponentSerializer();


            var output = new TwuiFile();

            var byteArray = packFile.DataSource.ReadData();
            using var steamReader = new MemoryStream(byteArray);

            var xmlRoot = XElement.Load(steamReader);
            output.FileMetaData.Version = (float)xmlRoot.Attribute("version");


            var hierarchyNode = xmlRoot.Element("hierarchy");
            output.Hierarchy = HierarchySerializer.Serialize(hierarchyNode);

            var componentsNode = xmlRoot.Element("components");
            output.Components = ComponentSerializer.Serialize(componentsNode);

            return output;
        }
    }
}
