using Filetypes.RigidModel;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FileTypes.RigidModel.MaterialHeaders
{
    public interface IMaterial
    {
        public ModelMaterialEnum MaterialId { get; set; }
        UiVertexFormat VertexType { get; set; }
        VertexFormat BinaryVertexFormat { get; set; }
        Vector3 PivotPoint { get; set; }
        AlphaMode AlphaMode { get; set; }
        string ModelName { get; set; }
        string TextureDirectory { get; set; }

        IMaterial Clone();
        uint ComputeSize();
        List<RmvTexture> GetAllTextures();
        RmvTexture? GetTexture(TexureType texureType);
        void SetTexture(TexureType texureType, string path);

        void UpdateEnumsBeforeSaving(UiVertexFormat uiVertexFormat, RmvVersionEnum outputVersion);
        void EnrichDataBeforeSaving(string[] boneNames, BoundingBox boundingBox);
    }

    public interface IMaterialCreator
    {
        IMaterial Create(ModelMaterialEnum materialId, RmvVersionEnum rmvType, byte[] dataArray, int dataOffset);
        byte[] Save(IMaterial material);
    }

    public class MaterialFactory
    {
        Dictionary<ModelMaterialEnum, IMaterialCreator> _materialCreators = new Dictionary<ModelMaterialEnum, IMaterialCreator>();

        public static MaterialFactory Create() => new MaterialFactory();

        public MaterialFactory()
        {
            _materialCreators[ModelMaterialEnum.weighted_skin_dirtmap] = new WeighterMaterialCreator();
            _materialCreators[ModelMaterialEnum.weighted_skin] = new WeighterMaterialCreator();
            _materialCreators[ModelMaterialEnum.weighted] = new WeighterMaterialCreator();
            _materialCreators[ModelMaterialEnum.default_type] = new WeighterMaterialCreator();
            _materialCreators[ModelMaterialEnum.TerrainTiles] = new TerrainTileMaterialCreator();
            _materialCreators[ModelMaterialEnum.custom_terrain] = new CustomTerrainMaterialCreator();
        }

        public IMaterial LoadMaterial(byte[] data, int offset, RmvVersionEnum rmvType, ModelMaterialEnum modelTypeEnum, long expectedMaterialSize)
        {
            if (_materialCreators.ContainsKey(modelTypeEnum))
            {
                var material = _materialCreators[modelTypeEnum].Create(modelTypeEnum, rmvType, data, offset);
                var materialSize = material.ComputeSize();

                if (materialSize != expectedMaterialSize)
                    throw new Exception($"Part of material {modelTypeEnum} header not read");

                return material;
            }

            throw new Exception($"Uknown material - {modelTypeEnum} Material Size = {expectedMaterialSize}");
        }

        public byte[] Save(ModelMaterialEnum modelTypeEnum, IMaterial material)
        {
            return _materialCreators[modelTypeEnum].Save(material);
        }

        public List<ModelMaterialEnum> GetSupportedMaterials() => _materialCreators.Keys.Select(x => x).ToList();
    }
}
