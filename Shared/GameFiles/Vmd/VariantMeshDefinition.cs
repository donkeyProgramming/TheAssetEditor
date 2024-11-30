using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles.Models;
using static Shared.GameFormats.Vmd.VariantMeshDefinition;

namespace Shared.GameFormats.Vmd
{
    public static class VariantMeshDefinitionLoader
    {

        public static VariantMesh Load(string fileContent, bool strict = false)
        {
            var xRoot = new XmlRootAttribute("VARIANT_MESH");

            var xmlserializer = new XmlSerializer(typeof(VariantMesh), xRoot);
            using var stringReader = new StringReader(fileContent);
            var reader = XmlReader.Create(stringReader);

            object result = null;
            if (strict)
                result = xmlserializer.Deserialize(reader, new UnknownXmlDataThrower().EventHandler);
            else
                result = xmlserializer.Deserialize(reader);

            var typedObject = result as VariantMesh;
            typedObject.FixStrings();
            return typedObject;
        }

        public static VariantMesh Load(PackFile pf, bool strict = false)
        {
            var vmdContent = Encoding.UTF8.GetString(pf.DataSource.ReadData());
            return Load(vmdContent, strict);
        }
    }


    public class VariantMeshDefinition
    {
        public class VariantMesh
        {
            [XmlAttribute("model")]
            public string ModelReference { get; set; }

            [XmlAttribute("imposter_model")]
            public string ImposterModel { get; set; }

            [XmlAttribute("decal_diffuse")]
            public string DecalDiffuse { get; set; }
            [XmlAttribute("decal_normal")]
            public string DecalNormal { get; set; }


            [XmlAttribute("use_different_attach_point_parts")]
            public string use_different_attach_point_parts { get; set; }


            [XmlElement("SLOT")]
            public List<SLOT> ChildSlots { get; set; }

            [XmlElement("META_DATA")]
            public List<MetaData> MetaDataList { get; set; }

            public void FixStrings()
            {
                if (ModelReference != null)
                    ModelReference = ModelReference.ToLower().Replace("//", "\\");
                foreach (var item in ChildSlots)
                    item.FixStrings();
            }
        }

        public class VariantMeshRef
        {
            [XmlAttribute("definition")]
            public string Reference { get; set; }

            public void FixStrings()
            {
                if (Reference != null)
                    Reference = Reference.ToLower().Replace("//", "\\");
            }
        }

        public class MetaData
        {
            [XmlText()]
            public string Value { get; set; }
        }

        public class SLOT
        {
            [XmlAttribute("attach_point")]
            public string AttachmentPoint { get; set; }

            [XmlAttribute("probability")]
            public string Probability { get; set; }

            [XmlAttribute("name")]
            public string Name { get; set; }


            [XmlAttribute("use_different_attach_point_parts")]
            public string use_different_attach_point_parts { get; set; }


            [XmlElement("VARIANT_MESH")]
            public List<VariantMesh> ChildMeshes { get; set; }

            [XmlElement("VARIANT_MESH_REFERENCE")]
            public List<VariantMeshRef> ChildReferences { get; set; }

            public void FixStrings()
            {
                if (Name != null)
                    Name = Name.ToLower().Replace("//", "\\");

                foreach (var item in ChildMeshes)
                    item.FixStrings();

                foreach (var item in ChildReferences)
                    item.FixStrings();
            }
        }



    }
}
