using Microsoft.Xna.Framework;

namespace FileTypes.MetaData.Instances
{
    public class Transform
    {
        public static Transform Create(MetaEntry metaData)
        {
            var ouput = new Transform()
            {
                StartTime = metaData.Get<float>("StartTime"),
                EndTime = metaData.Get<float>("EndTime"),

                TargetNode = metaData.Get<int>("TargetNode"),
                Position = metaData.Get<Vector3>("Position"),
                Orientation = metaData.Get<Vector4>("Orientation"),
            };

            return ouput;
        }


        public float StartTime { get; set; }
        public float EndTime { get; set; }
        public int TargetNode { get; set; }
        public Vector3 Position { get; set; }
        public Vector4 Orientation { get; set; }
    }
}
