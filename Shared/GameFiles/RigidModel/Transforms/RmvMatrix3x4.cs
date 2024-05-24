namespace Shared.GameFormats.RigidModel.Transforms
{
    [Serializable]
    public struct RmvMatrix3x4
    {
        RmvVector4 Row0;
        RmvVector4 Row1;
        RmvVector4 Row2;

        public bool IsIdentity()
        {

            if (Row0.X == 1 && Row0.Y == 0 && Row0.Z == 0 && Row0.W == 0)
            {
                if (Row1.X == 0 && Row1.Y == 1 && Row1.Z == 0 && Row1.W == 0)
                {
                    if (Row2.X == 0 && Row2.Y == 0 && Row2.Z == 1 && Row2.W == 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static RmvMatrix3x4 Identity()
        {
            var newMatrix = new RmvMatrix3x4();

            newMatrix.Row0 = new RmvVector4(1, 0, 0, 0);
            newMatrix.Row1 = new RmvVector4(0, 1, 0, 0);
            newMatrix.Row2 = new RmvVector4(0, 0, 1, 0);
            return newMatrix;
        }

        public RmvMatrix3x4 Clone()
        {
            return new RmvMatrix3x4()
            {
                Row0 = Row0,
                Row1 = Row1,
                Row2 = Row2,
            };
        }
    }
}
