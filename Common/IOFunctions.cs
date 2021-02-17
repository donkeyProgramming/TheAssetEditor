using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Common
{
    public class IOFunctions
    {
        static public byte[] staticBuffer = new byte[1024];
        public static string TheadUnsafeReadZeroTerminatedAscii(BinaryReader reader)
        {
            var index = 0;
            byte ch2 = reader.ReadByte();
            while (ch2 != 0)
            {
                staticBuffer[index++] = ch2;
                ch2 = reader.ReadByte();
            }

            return Encoding.UTF8.GetString(staticBuffer, 0, index);
        }
    }
}
