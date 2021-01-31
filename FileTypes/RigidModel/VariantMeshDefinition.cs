using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Filetypes.RigidModel
{
    public class VariantMeshDefinition
    {
        public class VariantMeshReference
        {
            [JsonProperty("@definition")]
            public string @definition { get; set; }
        }

        public class VariantMesh
        {
            [JsonProperty("@model")]
            public string Name { get; set; }
        }

        public class SLOT
        {
            [JsonProperty("@name")]
            public string Name { get; set; }

            [JsonProperty("VARIANT_MESH")]
            [JsonConverter(typeof(SingleOrArrayConverter<VariantMesh>))]
            public List<VariantMesh> VariantMeshes { get; set; } = new List<VariantMesh>();

            [JsonProperty("@attach_point")]
            public string AttachPoint { get; set; } = "";


            [JsonProperty("VARIANT_MESH_REFERENCE")]
            [JsonConverter(typeof(SingleOrArrayConverter<VariantMeshReference>))]
            public List<VariantMeshReference> VariantMeshReferences { get; set; } = new List<VariantMeshReference>();

            public override string ToString()
            {
                var refCount = VariantMeshReferences.Count;
                var meshCount = VariantMeshes.Count;
                var str =  $"{Name}";
                if(!string.IsNullOrEmpty(AttachPoint))
                    str+=$" - AP:{AttachPoint}";
                if (refCount != 0)
                    str += $" - Refs:{refCount}";
                if (meshCount != 0)
                    str += $" - Meshes:{meshCount}";
                return str;
            }
        }

        public class VARIANTMESH
        {
            [JsonConverter(typeof(SingleOrArrayConverter<SLOT>))]
            public List<SLOT> SLOT { get; set; }
        }

        public class VariantMeshFile
        {
            public VARIANTMESH VARIANT_MESH { get; set; }
        }

        public static VariantMeshFile Create(string fileContent)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(fileContent);
            string json = JsonConvert.SerializeXmlNode(doc, Newtonsoft.Json.Formatting.Indented);
            VariantMeshFile output = JsonConvert.DeserializeObject<VariantMeshFile>(json);

            return output;
        }
    }
}
