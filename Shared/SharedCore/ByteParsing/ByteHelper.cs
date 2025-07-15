using System.Collections;
using System.Runtime.InteropServices;
using System.Text;

namespace Shared.Core.ByteParsing
{
    public class ByteHelper
    {
        public static T ByteArrayToStructure<T>(byte[] bytes, int offset) where T : struct
        {
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);

            var objectSize = GetSize<T>();
            if (offset + objectSize > bytes.Length)
                throw new Exception($"Object {typeof(T)} does not fit into the remaining buffer [offset{offset} + Size{objectSize} => byteBuffer{bytes.Length}]");

            try
            {
                var p = handle.AddrOfPinnedObject() + offset;
                var pointerToStruct = Marshal.PtrToStructure(p, typeof(T));
                if (pointerToStruct == null)
                    throw new Exception("Object is null after getting pointerToStruct");
                return (T)pointerToStruct;
            }
            finally
            {
                handle.Free();
            }
        }


        public static ReadOnlySpan<T> LoadArray<T>(byte[] bytes, int offset, int totalBytesToRead) where T:struct
        {
            var span = bytes.AsSpan(offset, totalBytesToRead);
            var structSpan = MemoryMarshal.Cast<byte, T>(span);
            return structSpan;
        }

        public static byte[] GetBytes<T>(T data) where T : struct
        {
            var size = Marshal.SizeOf(data);
            var arr = new byte[size];

            var ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(data, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }

        static public byte[] CreateFixLengthString(string str, int maxLength)
        {
            var output = new byte[maxLength];
            var byteValues = Encoding.UTF8.GetBytes(str);
            for (var i = 0; i < byteValues.Length && i < maxLength; i++)
                output[i] = byteValues[i];
            return output;
        }

        public static int GetSize(Type type)
        {
            return Marshal.SizeOf(type);
        }

        public static int GetSize<T>()
        {
            return Marshal.SizeOf(typeof(T));
        }

        public static uint GetPropertyTypeSize<T>(T property)
        {
            if (property == null)
            {
                var type = typeof(T);
                if (Nullable.GetUnderlyingType(type) != null)
                    type = Nullable.GetUnderlyingType(type);
                return (uint)Marshal.SizeOf(type);
            }

            if (property is IList)
                return (uint)Marshal.SizeOf(typeof(T).GetGenericArguments()[0]);

            if (property is Enum)
                return (uint)Marshal.SizeOf(Enum.GetUnderlyingType(property.GetType()));

            return (uint)Marshal.SizeOf(property);
        }
    }
}
