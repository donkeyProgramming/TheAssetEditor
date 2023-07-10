// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

namespace CommonControls.FileTypes.Vmd
{
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
