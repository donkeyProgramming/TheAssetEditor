using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Filetypes
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

    class ByteHelper
    {
        public static T ByteArrayToStructure<T>(byte[] bytes, int offset) where T : struct
        {
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);

            try
            {
                var p = handle.AddrOfPinnedObject() + offset;
                return (T)Marshal.PtrToStructure(p, typeof(T));
            }
            catch (Exception e)
            {
                throw;
            }
            finally
            {
                handle.Free();
            }
        }

        public static int GetSize(Type type)
        {
            return Marshal.SizeOf(type);
        }
    }
}
