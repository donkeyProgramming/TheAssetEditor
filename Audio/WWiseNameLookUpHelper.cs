using System.Text;

namespace CommonControls.Editors.AudioEditor
{
    public static class WWiseNameLookUpHelper
    {
        public static uint ComputeWWiseHash(string name)
        {
            var lower = name.ToLower().Trim();
            var bytes = Encoding.UTF8.GetBytes(lower);

            uint hashValue = 2166136261;
            for (int byteIndex = 0; byteIndex < bytes.Length; byteIndex++)
            {
                var nameByte = bytes[byteIndex];
                hashValue = hashValue * 16777619;
                hashValue = hashValue ^ nameByte;
                hashValue = hashValue & 0xFFFFFFFF;
            }

            return hashValue;
        }
    }
}
