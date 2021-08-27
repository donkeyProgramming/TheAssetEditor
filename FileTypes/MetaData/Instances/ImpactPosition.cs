using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileTypes.MetaData.Instances
{
    public class ImpactPosition
    {
        public static ImpactPosition Create(MetaEntry metaData)
        {
            var ouput = new ImpactPosition()
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
