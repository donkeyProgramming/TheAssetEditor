
namespace Shared.GameFormats.RigidModel.Transforms
{
    [Serializable]
    public struct RmvTransform
    {
        public RmvVector3 Pivot;
        public RmvMatrix3x4 Matrix0;
        public RmvMatrix3x4 Matrix1;
        public RmvMatrix3x4 Matrix2;

        public bool IsIdentityPivot()
        {
            if (!Pivot.IsAllZero())
                return false;
            return true;
        }

        public bool IsIdentityMatrices()
        {
            if (!Matrix0.IsIdentity())
                return false;

            if (!Matrix1.IsIdentity())
                return false;

            if (!Matrix2.IsIdentity())
                return false;

            return true;
        }

        internal static RmvTransform CreateIdentity()
        {
            var newTransform = new RmvTransform()
            {
                Pivot = new RmvVector3(0, 0, 0),
                Matrix0 = RmvMatrix3x4.Identity(),
                Matrix1 = RmvMatrix3x4.Identity(),
                Matrix2 = RmvMatrix3x4.Identity(),
            };
            return newTransform;
        }
    }
}
