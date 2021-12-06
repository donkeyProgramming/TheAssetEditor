using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.MetaData.Instances
{
    public class AnimatedProp
    {
        public static AnimatedProp Create(MetaEntry metaData)
        {
            var ouput = new AnimatedProp()
            {
                StartTime = metaData.Get<float>("StartTime"),
                EndTime = metaData.Get<float>("EndTime"),

                MeshName = metaData.Get<string>("ModelName"),
                AnimationName = metaData.Get<string>("AnimationName"),
                BoneId = metaData.Get<int>("BoneID"),

                Position = metaData.Get<Vector3>("Position"),
                Orientation = new Quaternion(metaData.Get<Vector4>("Orientation")),
            };

            if (ouput.Orientation.Length() == 0)
                ouput.Orientation = Quaternion.Identity;

            return ouput;
        }


        public float StartTime { get; set; }
        public float EndTime { get; set; }

        public string MeshName { get; set; }
        public string AnimationName { get; set; }
        public int BoneId { get; set; }

        public Vector3 Position { get; set; }
        public Quaternion Orientation { get; set; }
    }
}
