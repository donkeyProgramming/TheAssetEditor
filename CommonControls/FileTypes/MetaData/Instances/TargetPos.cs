using Microsoft.Xna.Framework;

namespace CommonControls.FileTypes.MetaData.Instances
{
    public class TargetPos
    {
        public static TargetPos Create(MetaEntry metaData)
        {
            var ouput = new TargetPos()
            {
                StartTime = metaData.Get<float>("StartTime"),
                EndTime = metaData.Get<float>("EndTime"),
                Position = metaData.Get<Vector3>("Position"),
            };

            return ouput;
        }


        public float StartTime { get; set; }
        public float EndTime { get; set; }
        public Vector3 Position { get; set; }
    }
}
