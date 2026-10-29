using Microsoft.Xna.Framework;

namespace Editors.VfxEditor.Preview
{
    /// <summary>The effect's frame when the preview flies it: a straight line along a local axis in metres a second (as
    /// projectiles_tables.muzzle_velocity), crossing the origin halfway. World-space particles stay behind as the trail.</summary>
    public static class ProjectileFlight
    {
        // Which local axis a projectile flies along is recorded nowhere found, so the user picks it.
        public static readonly IReadOnlyList<(string Name, Vector3 Axis)> Axes =
            [("+Z", Vector3.UnitZ), ("-Z", -Vector3.UnitZ), ("+X", Vector3.UnitX), ("-X", -Vector3.UnitX), ("+Y", Vector3.UnitY), ("-Y", -Vector3.UnitY)];

        public static Func<float, Matrix> WorldAt(Vector3 axis, float speed, float flightTime) =>
            t => Matrix.CreateTranslation(axis * speed * (t - flightTime / 2));
    }
}
