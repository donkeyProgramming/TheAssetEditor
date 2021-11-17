using Filetypes.RigidModel;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace FileTypes.RigidModel.MaterialHeaders
{
    public interface IMaterial
    {
        VertexFormat VertexType { get; set; }
        VertexFormat BinaryVertexFormat { get; set; }
        Vector3 PivotPoint { get; set; }
        AlphaMode AlphaMode { get; set; }
        string ModelName { get; set; }
         
        IMaterial Clone();
        uint ComputeSize();
        RmvTexture? GetTexture(TexureType texureType);
    }

    public interface IMaterialCreator
    {
        IMaterial Create(RmvVersionEnum rmvType, byte[] buffer, int offset);
        byte[] Save(IMaterial material);
    }

    public class MaterialFactory
    {
        Dictionary<ModelMaterialEnum, IMaterialCreator> _materialCreators = new Dictionary<ModelMaterialEnum, IMaterialCreator>();

        public static MaterialFactory Create() => new MaterialFactory();

        public MaterialFactory()
        {
            _materialCreators[ModelMaterialEnum.weighted] = new WeighterMaterialCreator();
            _materialCreators[ModelMaterialEnum.default_type] = new WeighterMaterialCreator();
            _materialCreators[ModelMaterialEnum.TerrainTiles] = new TerrainTileMaterialCreator();
        }

        public IMaterial LoadMaterial(byte[] data, int offset, RmvVersionEnum rmvType, ModelMaterialEnum modelTypeEnum)
        {
            if(_materialCreators.ContainsKey(modelTypeEnum))
                return _materialCreators[modelTypeEnum].Create(rmvType, data, offset);

            throw new Exception($"Uknown material - {modelTypeEnum}");
        }

        public byte[] Save(ModelMaterialEnum modelTypeEnum, IMaterial material)
        {
            return _materialCreators[modelTypeEnum].Save(material);
        }
    }
}
