using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using SharpDX.MediaFoundation;

namespace Editors.ImportExport.Importing.Importers.PngToDds.Helpers
{
    public class ColorChannels
    {
        public static byte[] PtrToBytes(IntPtr ptr, int index, int size)
        {
            byte[] bytes = new byte[size];
            Marshal.Copy(ptr, bytes, index, size);
            return bytes;
        }

        public static void BytesToPtr(byte[] bytes, int index, IntPtr ptr)
        {
            Marshal.Copy(bytes, index, ptr, bytes.Length);
        }

        public static void GammaRGBA(ref byte r, ref byte g, ref byte b, ref byte a, float gamma)
        {
            var rgbaFloat = new Vector4((float)r, (float)g, (float)b, (float)a) / 255.0f;

            r = (byte)(Math.Pow(rgbaFloat.X, gamma) * 255.0f);
            g = (byte)(Math.Pow(rgbaFloat.Y, gamma) * 255.0f);
            b = (byte)(Math.Pow(rgbaFloat.Z, gamma) * 255.0f);
            a = (byte)(Math.Pow(rgbaFloat.W, gamma) * 255.0f);
        }

        public static byte GammaComponent(byte c, float gamma)
        {           
            var processed = (byte) (Math.Pow( ((double)c/255.0f ), gamma) * 255.0f);

            return processed;
        }



        public float gamma_accurate_component(float linear_val)
        {
            const float srgb_gamma_ramp_inflection_point = 0.0031308f;

            if (linear_val <= srgb_gamma_ramp_inflection_point)
            {
                return 12.92f * linear_val;
            }
            else
            {
                const float a = 0.055f;

                return (float)((1.0 + a) * Math.Pow(linear_val, 1.0f / 2.4f)) - a;
            }
        }


        public nint Value { get; set; }

        public byte[] Channels { get; set; }

        public byte this[int index]
        {
            get
            {
                if (index < 0 || index > 3)
                    throw new IndexOutOfRangeException("Index must be between 0 and 3.");

                byte[] intBytes = BitConverter.GetBytes(Value);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(intBytes);

                return intBytes[index];
            }
            set
            {
                if (index < 0 || index > 3)
                    throw new IndexOutOfRangeException("Index must be between 0 and 3.");

                byte[] intBytes = BitConverter.GetBytes(Value);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(intBytes);

                intBytes[index] = value;

                if (BitConverter.IsLittleEndian)
                    Array.Reverse(intBytes);

                Value = BitConverter.ToInt32(intBytes, 0);
            }
        }

        public ColorChannels(nint rgbaValue)
        {
            Value = rgbaValue;



        }
    }
}
