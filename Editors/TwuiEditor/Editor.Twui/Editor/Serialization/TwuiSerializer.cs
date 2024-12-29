using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Editors.Twui.Editor.Datatypes;
using Shared.Core.PackFiles.Models;

namespace Editors.Twui.Editor.Serialization
{
    public class TwuiSerializer
    {

        public TwuiSerializer() 
        { 
            
        
        
        
        }

        public TwuiFile Load(PackFile packFile)
        {
            var output = new TwuiFile();

            var byteArray = packFile.DataSource.ReadData();
            using var steamReader = new MemoryStream(byteArray);

            var xmlRoot = XElement.Load(steamReader);
            output.FileMetaData.Version = (float)xmlRoot.Attribute("version");


            var hierarchyNode = xmlRoot.Element("hierarchy");
            output.Hierarchy = Hierarchy.Serialize(hierarchyNode);

            var componentsNode = xmlRoot.Element("components");
            output.Components = ComponentSerializer.Serialize(componentsNode);

            return output;
        }          
    }
}
