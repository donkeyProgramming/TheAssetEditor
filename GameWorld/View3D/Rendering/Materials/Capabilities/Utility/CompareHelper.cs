using Microsoft.Xna.Framework;

namespace GameWorld.Core.Rendering.Materials.Capabilities.Utility
{
    public static class CompareHelper
    {

        static public bool Compare(bool self, bool other, string attributeName, out (bool Result, string Message) result)
        {
            if (self != other)
            {
                result = (false, $"{attributeName} - Different values ({self} vs {other})");
                return false;
            }

            result = (true, "");
            return true;
        }

        static public bool Compare(float self, float other, string attributeName, out (bool Result, string Message) result)
        {
            if (self != other)
            {
                result = (false, $"{attributeName} - Different values ({self} vs {other})");
                return false;
            }

            result = (true, "");
            return true;
        }

        static public bool Compare(TextureInput self, TextureInput other, string attributeName, out (bool Result, string Message) result)
        {
            if (self.TexturePath != other.TexturePath)
            {
                result = (false, $"{attributeName} - Different values ({self} vs {other})");
                return false;
            }

            result = (true, "");
            return true;
        }

        static public bool Compare(Vector2 self, Vector2 other, string attributeName, out (bool Result, string Message) result)
        {
            if (self != other)
            {
                result = (false, $"{attributeName} - Different values ({self} vs {other})");
                return false;
            }

            result = (true, "");
            return true;
        }

        static public bool Compare(Vector3 self, Vector3 other, string attributeName, out (bool Result, string Message) result)
        {
            if (self != other)
            {
                result = (false, $"{attributeName} - Different values ({self} vs {other})");
                return false;
            }

            result = (true, "");
            return true;
        }

        static public bool Compare(Vector4 self, Vector4 other, string attributeName, out (bool Result, string Message) result)
        {
            if (self != other)
            {
                result = (false, $"{attributeName} - Different values ({self} vs {other})");
                return false;
            }

            result = (true, "");
            return true;
        }

    }
}
