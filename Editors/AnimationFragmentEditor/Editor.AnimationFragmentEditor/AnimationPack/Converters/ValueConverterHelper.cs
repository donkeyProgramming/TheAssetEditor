using System.Collections;

namespace Editors.AnimationFragmentEditor.AnimationPack.Converters
{
    public static class ValueConverterHelper
    {
        public static bool ValidateBoolArray(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            var parts = value.Split(",");
            if (parts.Length != 6)
                return false;

            for (int i = 0; i < 6; i++)
            {
                var str = parts[i].Trim();
                if (bool.TryParse(str, out _) == false)
                    return false;
            }
            return true;
        }

        public static string ConvertIntToBoolArray(int value)
        {
            var bitArray = new BitArray(new int[] { value });
            var bits = new bool[bitArray.Count];
            bitArray.CopyTo(bits, 0);

            string[] strArray = new string[6];
            for (int i = 0; i < 6; i++)
                strArray[i] = bits[i].ToString();

            return string.Join(", ", strArray);
        }

        public static int CreateWeaponFlagInt(string strArray)
        {
            var values = strArray.Split(",").Select(x => bool.Parse(x)).ToArray();
            BitArray b = new BitArray(new int[] { 0 });
            for (int i = 0; i < 6; i++)
                b[i] = values[i];

            int[] array = new int[1];
            b.CopyTo(array, 0);
            return array[0];
        }

    }
}
