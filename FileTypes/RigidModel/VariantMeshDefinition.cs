using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Filetypes.RigidModel
{
    public class VariantMeshDefinition
    {
        public class VariantMesh
        {
            [XmlAttribute("model")]
            public string ModelReference { get; set; }

            [XmlElement("SLOT")]
            public List<SLOT> ChildSlots { get; set; }

            [XmlElement("META_DATA")]
            public List<MetaData> MetaDataList { get; set; }

            public void FixStrings()
            {
                if(ModelReference != null)
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


            [XmlElement("VARIANT_MESH")]
            public List<VariantMesh> ChildMeshes { get; set; }

            [XmlElement("VARIANT_MESH_REFERENCE")]
            public List<VariantMeshRef> ChildReferences { get; set; }

            public void FixStrings()
            {
                if(Name != null)
                    Name = Name.ToLower().Replace("//", "\\");

                foreach (var item in ChildMeshes)
                    item.FixStrings();

                foreach (var item in ChildReferences)
                    item.FixStrings();
            }
        }



    }
}
