using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Microsoft.Xna.Framework;

namespace Shared.GameFormats.Twui
{
    public class BaseTwuiSerializer
    {
        protected static string AssignAttribute(string value, XElement xmlNode, [CallerArgumentExpression("value")] string variableName = null)
        {
            var attributeContent = GetAttributeContent(variableName, xmlNode);
            if (attributeContent == null)
                return value;

            return (string)attributeContent;
        }

        protected static float AssignAttribute(float value, XElement xmlNode, [CallerArgumentExpression("value")] string variableName = null)
        {
            var attributeContent = GetAttributeContent(variableName, xmlNode);
            if (attributeContent == null)
                return value;

            return (float)attributeContent;
        }

        protected static bool AssignAttribute(bool value, XElement xmlNode, [CallerArgumentExpression("value")] string variableName = null)
        {
            var attributeContent = GetAttributeContent(variableName, xmlNode);
            if (attributeContent == null)
                return value;

            return (bool)attributeContent;
        }

        protected static Vector2 AssignAttribute(Vector2 value, XElement xmlNode, [CallerArgumentExpression("value")] string variableName = null)
        {
            var attributeContent = GetAttributeContent(variableName, xmlNode);
            if (attributeContent == null)
                return value;
            var str = attributeContent.Value;
            var splitStr = str.Split(",");
            var v0 = float.Parse(splitStr[0]);
            var v1 = float.Parse(splitStr[1]);

            return new Vector2(v0, v1);
        }

        protected static Vector4 AssignAttribute(Vector4 value, XElement xmlNode, [CallerArgumentExpression("value")] string variableName = null)
        {
            var attributeContent = GetAttributeContent(variableName, xmlNode);
            if (attributeContent == null)
                return value;
            var str = attributeContent.Value;
            var splitStr = str.Split(",");
            var v0 = float.Parse(splitStr[0]);
            var v1 = float.Parse(splitStr[1]);
            var v2 = float.Parse(splitStr[2]);
            var v3 = float.Parse(splitStr[3]);

            return new Vector4(v0, v1, v2,v3);
        }

        protected static XAttribute? GetAttributeContent(string variableName, XElement xmlNode)
        {
            var attributeName = variableName.Split(".").Last();
            var attributeContent = xmlNode.Attribute(attributeName.ToLower());
            return attributeContent;
        }

        protected static Vector2 AssignAttribute(string attributeName, XElement xmlNode, Vector2 nodeDefaultValue)
        {
            var attributeContent = xmlNode.Attribute(attributeName.ToLower());
            if (attributeContent == null)
                return nodeDefaultValue;

            var str = attributeContent.Value;
            var splitStr = str.Split(",");
            var v0 = float.Parse(splitStr[0]);
            var v1 = float.Parse(splitStr[1]);

            return new Vector2(v0, v1);
        }
    }
}


