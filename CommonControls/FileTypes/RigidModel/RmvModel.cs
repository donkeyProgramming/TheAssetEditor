using CommonControls.FileTypes.RigidModel.MaterialHeaders;

namespace CommonControls.FileTypes.RigidModel
{
    public class RmvModel
    {
        public RmvCommonHeader CommonHeader { get; set; }
        public IMaterial Material { get; set; }
        public RmvMesh Mesh { get; set; }

        public RmvModel()
        { }

        public RmvModel Clone()
        {
            return new RmvModel()
            {
                CommonHeader = CommonHeader,
                Material = Material.Clone(),
                Mesh = null
            };
        }
    }

}
