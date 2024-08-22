using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Misc;

namespace Shared.GameFormats.RigidModel.MaterialHeaders
{
    public class MaterialFactory
    {
        private readonly ILogger _logger = Logging.Create<MaterialFactory>();
        private readonly Dictionary<ModelMaterialEnum, IMaterialCreator> _materialCreators = [];

        public static MaterialFactory Create() => new MaterialFactory();

        public MaterialFactory()
        {
            // Get all the weighted materials. Different enums, but same header
            var weightedEnums = ModelMaterialEnumHelper.GetAllWeightedMaterials();
            foreach (var enumValue in weightedEnums)
                _materialCreators[enumValue] = new WeighterMaterialCreator();

            _materialCreators[ModelMaterialEnum.TerrainTiles] = new TerrainTileMaterialCreator();
            _materialCreators[ModelMaterialEnum.custom_terrain] = new CustomTerrainMaterialCreator();
        }

        public IRmvMaterial LoadMaterial(byte[] data, int offset, RmvVersionEnum rmvType, ModelMaterialEnum modelTypeEnum, long expectedMaterialSize)
        {
            try
            {
                if (_materialCreators.ContainsKey(modelTypeEnum))
                {
                    var material = _materialCreators[modelTypeEnum].Create(modelTypeEnum, rmvType, data, offset);
                    var actualMaterialSize = material.ComputeSize();

                    var bytesLeft = expectedMaterialSize - actualMaterialSize;
                    if (bytesLeft != 0)
                    {
                        var outputArray = new byte[expectedMaterialSize - actualMaterialSize];
                        Array.ConstrainedCopy(data, (int)(offset + actualMaterialSize), outputArray, 0, (int)(expectedMaterialSize - actualMaterialSize));
                        File.WriteAllBytes(DirectoryHelper.Temp + "\\ExtraData_" + modelTypeEnum + "_Start_" + offset + actualMaterialSize + "_Size_" + (expectedMaterialSize - actualMaterialSize) + ".data", outputArray);
                        throw new Exception($"Part of material {modelTypeEnum} header not read - {bytesLeft} bytes left in header.");
                    }

                    return material;
                }
                else
                {
                    _logger.Here().Error($"Could not load {modelTypeEnum} material, atttempting to load using default");
                    var material = _materialCreators[ModelMaterialEnum.default_type].Create(modelTypeEnum, rmvType, data, offset);
                    var actualMaterialSize = material.ComputeSize();

                    var bytesLeft = expectedMaterialSize - actualMaterialSize;
                    if (bytesLeft != 0)
                    {
                        var outputArray = new byte[expectedMaterialSize - actualMaterialSize];
                        Array.ConstrainedCopy(data, (int)(offset + actualMaterialSize), outputArray, 0, (int)(expectedMaterialSize - actualMaterialSize));
                        File.WriteAllBytes(DirectoryHelper.Temp + "\\ExtraData_" + modelTypeEnum + "_Start_" + offset + actualMaterialSize + "_Size_" + (expectedMaterialSize - actualMaterialSize) + ".data", outputArray);
                        throw new Exception($"Uknown material - {modelTypeEnum} header not read. Expected Size = {expectedMaterialSize} Actual Size = {actualMaterialSize}");
                    }

                    return material;
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Error loading material - {modelTypeEnum} Material Size = {expectedMaterialSize}", e);
            }
        }

        public IRmvMaterial CreateMaterial(ModelMaterialEnum modelTypeEnum)
        {
            if (_materialCreators.ContainsKey(modelTypeEnum))
            {
                var material = _materialCreators[modelTypeEnum].CreateEmpty(modelTypeEnum);
                return material;
            }

            throw new Exception($"Error Creating material - {modelTypeEnum} is not a supported material");
        }

        public byte[] Save(ModelMaterialEnum modelTypeEnum, IRmvMaterial material)
        {
            return _materialCreators[modelTypeEnum].Save(material);
        }

        public List<ModelMaterialEnum> GetSupportedMaterials() => _materialCreators.Keys.Select(x => x).ToList();
    }
}
