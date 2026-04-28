using System.Reflection;

namespace Shared.Core.Misc
{
    public static class ReflectionHelper
    {

        public static object GetMemberValue(object obj, string memberName)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            var type = obj.GetType();

            // Try to get as a Property
            var propInfo = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (propInfo != null)
                return propInfo.GetValue(obj, null);

            // Try to get as a Field
            var fieldInfo = type.GetField(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (fieldInfo != null)
                return fieldInfo.GetValue(obj);

            throw new MemberAccessException($"Member '{memberName}' not found in type '{type.Name}'.");
        }

        public static T CreateShallowCopy<T>(T original) where T : class
        {
            if (original == null)
            {
                return null;
            }

            // Create a new instance of the same type as the original object.
            // Activator.CreateInstance() uses reflection to instantiate the type dynamically.
            T copy = (T)Activator.CreateInstance(original.GetType());

            // Get all public, instance properties of the class.
            PropertyInfo[] properties = original.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo property in properties)
            {
                // Check if the property can be read and written to.
                if (property.CanRead && property.CanWrite)
                {
                    // Get the value from the original object and set it on the new copy.
                    object value = property.GetValue(original);
                    property.SetValue(copy, value);
                }
            }

            return copy;
        }

    }
}
