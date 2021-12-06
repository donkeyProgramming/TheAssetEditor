using Microsoft.Xna.Framework;

namespace CommonControls.FileTypes.MetaData.Instances
{
    public class Effect
    {
        public static Effect Create(MetaEntry metaData)
        {
            var ouput = new Effect()
            {
                StartTime = metaData.Get<float>("StartTime"),
                EndTime = metaData.Get<float>("EndTime"),
                Name = metaData.Get<string>("VFX Name"),
                Tracking = metaData.Get<bool>("Tracking"),
                NodeIndex = metaData.Get<int>("NodeIndex"),
                Position = metaData.Get<Vector3>("Position"),
                Orientation = metaData.Get<Vector4>("Orientation"),
            };

            return ouput;
        }

        public float StartTime { get; set; }
        public float EndTime { get; set; }
        public string Name { get; set; }
        public bool Tracking { get; set; }
        public int NodeIndex { get; set; }
        public Vector3 Position { get; set; }
        public Vector4 Orientation { get; set; }
    }
}
