using Microsoft.Xna.Framework;

namespace GameWorld.Core.Vfx
{
    /// <summary>
    /// A vector field (vfx/vectorfields/*.dds). All 56 vanilla ones are uncompressed A16B16G16R16F volumes, 8 to 32 cells a
    /// side, without mips; x runs along a row, y down the rows and z through the slices, with Y up (measured 2026-10-12 on
    /// the radial, direction and xz vortex fields).
    /// </summary>
    public class VfxVectorField
    {
        const int Header = 128;
        const int HalfFloatRgba = 113;   // D3DFMT_A16B16G16R16F, the FourCC of every vanilla field
        readonly Vector3[] _cells;
        readonly int _width, _height, _depth;

        VfxVectorField(int width, int height, int depth, Vector3[] cells) => (_width, _height, _depth, _cells) = (width, height, depth, cells);

        /// <summary>Null for anything but a volume in that format.</summary>
        public static VfxVectorField? Read(byte[] dds)
        {
            if (dds.Length < Header || dds[0] != 'D' || dds[1] != 'D' || dds[2] != 'S' || BitConverter.ToInt32(dds, 84) != HalfFloatRgba)
                return null;
            var (height, width, depth) = (BitConverter.ToInt32(dds, 12), BitConverter.ToInt32(dds, 16), Math.Max(1, BitConverter.ToInt32(dds, 24)));
            if (width < 1 || height < 1 || Header + (long)width * height * depth * 8 > dds.Length)
                return null;
            var cells = new Vector3[width * height * depth];
            for (var i = 0; i < cells.Length; i++)
            {
                var at = Header + i * 8;
                cells[i] = new Vector3((float)BitConverter.ToHalf(dds, at), (float)BitConverter.ToHalf(dds, at + 2), (float)BitConverter.ToHalf(dds, at + 4));
            }
            return new VfxVectorField(width, height, depth, cells);
        }

        /// <summary>The field at a point in the volume's own units, 0 to 1 across it: trilinear between cell centres, the volume
        /// repeating outside itself.</summary>
        public Vector3 Sample(Vector3 uvw)
        {
            static (int Low, int High, float Weight) Axis(float u, int cells)
            {
                var x = u * cells - 0.5f;
                var floor = MathF.Floor(x);
                var low = (int)(((long)floor % cells + cells) % cells);
                return (low, (low + 1) % cells, x - floor);
            }

            var (x0, x1, fx) = Axis(uvw.X, _width);
            var (y0, y1, fy) = Axis(uvw.Y, _height);
            var (z0, z1, fz) = Axis(uvw.Z, _depth);
            Vector3 At(int x, int y, int z) => _cells[(z * _height + y) * _width + x];
            Vector3 Row(int y, int z) => Vector3.Lerp(At(x0, y, z), At(x1, y, z), fx);
            Vector3 Slice(int z) => Vector3.Lerp(Row(y0, z), Row(y1, z), fy);
            return Vector3.Lerp(Slice(z0), Slice(z1), fz);
        }
    }
}
