using Shared.Core.ByteParsing;
using Shared.GameFormats.RigidModel.Transforms;

namespace Shared.GameFormats.RigidModel.MaterialHeaders
{
    public enum WeightedParamterIds
    {
        FloatParams_UvScaleX = 0,
        FloatParams_UvScaleY = 1,
        
        IntParams_Alpha_index = 0,
        IntParams_Decal_index = 1,
        IntParams_Dirt_index = 2,
        
        Vec4Params_TextureDecalTransform = 0,
    }

    public class ParamList<T>
    {
        public List<(int Index, T Value)> Values { get; private set; } = [];

        public bool TryGet(WeightedParamterIds index, out T value)
        {
            for (var i = 0; i < Values.Count; i++)
            {
                if ((int)index == Values[i].Index)
                {
                    value = Values[i].Value;
                    return true;
                }
            }

            value = default;
            return false;
        }

        public T Get(WeightedParamterIds index)
        {
            var instance = Values.First(x => x.Index == (int)index);
            return instance.Value;
        }

        public void Set(WeightedParamterIds index, T value)
        {
            for (var i = 0; i < Values.Count; i++)
            {
                if (Values[i].Index == (int)index)
                {
                    Values[i] = ((int)index, value);
                    return;
                }
            }

            Values.Add(((int)index, value));
        }

        public ParamList<T> Clone()
        { 
            var output = new ParamList<T>();
            output.Values = output.Values.ToList();
            return output;
        }

        public int GetByteSize(int itemSize) => (Values.Count * itemSize) + (Values.Count * 4); // Value + index
    }


    public static class ParamListHelper
    {
        public static ParamList<RmvVector4> LoadVec4Params(uint Vec4ParamCount, byte[] dataArray, ref int dataOffset)
        {
            var output = new ParamList<RmvVector4>();
            for (var i = 0; i < Vec4ParamCount; i++)
            {
                var index = ByteParsers.Int32.TryDecodeValue(dataArray, dataOffset, out var indexValue, out _, out _);

                var result = ByteParsers.Single.TryDecodeValue(dataArray, dataOffset + 4, out var x, out var byteLength, out var error);
                if (!result)
                    throw new Exception("Error reading RmvVector4 parameter - " + error);

                result = ByteParsers.Single.TryDecodeValue(dataArray, dataOffset + 8, out var y, out byteLength, out error);
                if (!result)
                    throw new Exception("Error reading RmvVector4 parameter - " + error);

                result = ByteParsers.Single.TryDecodeValue(dataArray, dataOffset + 12, out var z, out byteLength, out error);
                if (!result)
                    throw new Exception("Error reading RmvVector4 parameter - " + error);

                result = ByteParsers.Single.TryDecodeValue(dataArray, dataOffset + 16, out var w, out byteLength, out error);
                if (!result)
                    throw new Exception("Error reading RmvVector4 parameter - " + error);

                dataOffset += 4 * 4 + 4;

                output.Set((WeightedParamterIds)indexValue, new RmvVector4(x, y, z, w));
            }
            return output;
        }


        public static ParamList<string> LoadStringParams(uint StringParamCount, byte[] dataArray, ref int dataOffset)
        {
            var output = new ParamList<string>();
            for (var i = 0; i < StringParamCount; i++)
            {
                var index = ByteParsers.Int32.TryDecodeValue(dataArray, dataOffset, out var indexValue, out _, out _);
                var result = ByteParsers.String.TryDecode(dataArray, dataOffset + 4, out var value, out var byteLength, out var error);
                if (!result)
                {
                    throw new Exception("Error reading string parameter - " + error);
                }
                dataOffset += byteLength + 4;
                output.Set((WeightedParamterIds)indexValue, value);
            }
            return output;
        }

        public static ParamList<float> LoadFloatParams(uint FloatParamCount, byte[] dataArray, ref int dataOffset)
        {
            var output = new ParamList<float>();
            for (var i = 0; i < FloatParamCount; i++)
            {
                var index = ByteParsers.Int32.TryDecodeValue(dataArray, dataOffset, out var indexValue, out _, out _);
                var result = ByteParsers.Single.TryDecodeValue(dataArray, dataOffset + 4, out var value, out var byteLength, out var error);
                if (!result)
                {
                    throw new Exception("Error reading float parameter - " + error);
                }
                dataOffset += byteLength + 4;
                output.Set((WeightedParamterIds)indexValue, value);
            }
            return output;
        }

        public static ParamList<int> LoadIntParams(uint IntParamCount, byte[] dataArray, ref int dataOffset)
        {
            var output = new ParamList<int>();
            for (var i = 0; i < IntParamCount; i++)
            {
                var index = ByteParsers.Int32.TryDecodeValue(dataArray, dataOffset, out var indexValue, out _, out _);
                var result = ByteParsers.Int32.TryDecodeValue(dataArray, dataOffset + 4, out var value, out var byteLength, out var error);
                if (!result)
                {
                    throw new Exception("Error reading int parameter - " + error);
                }
                dataOffset += byteLength + 4;
                output.Set((WeightedParamterIds)indexValue, value);
            }
            return output;
        }


    }
}
