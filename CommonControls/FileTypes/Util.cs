using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace CommonControls.FileTypes
{
    public static class Util
    {
        public static string SanatizeFixedString(string str)
        {
            var idx = str.IndexOf('\0');
            if (idx != -1)
                return str.Substring(0, idx);
            return str;
        }
    }


    public class ByteHelper
    {
        public static T ByteArrayToStructure<T>(byte[] bytes, int offset) where T : struct
        {
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);

            try
            {
                var p = handle.AddrOfPinnedObject() + offset;
                return (T)Marshal.PtrToStructure(p, typeof(T));
            }
            finally
            {
                handle.Free();
            }
        }

        public static byte[] GetBytes<T>(T data) where T : struct
        {
            int size = Marshal.SizeOf(data);
            byte[] arr = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(data, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }

        static public T DeepCopy<T>(T obj)
        {
            BinaryFormatter s = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                s.Serialize(ms, obj);
                ms.Position = 0;
                T t = (T)s.Deserialize(ms);

                return t;
            }
        }

        static public byte[] CreateFixLengthString(string str, int maxLength)
        {
            byte[] output = new byte[maxLength];
            var byteValues = Encoding.UTF8.GetBytes(str);
            for (int i = 0; i < byteValues.Length && i < maxLength; i++)
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
    }
}
