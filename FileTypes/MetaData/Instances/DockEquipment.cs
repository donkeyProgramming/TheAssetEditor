using System;
using System.Collections.Generic;
using System.Text;

namespace FileTypes.MetaData.Instances
{
    public class DockEquipment
    {
        public static DockEquipment Create(MetaEntry metaData)
        {
            var ouput = new DockEquipment()
            {
               // StartTime = metaData.Get<float>("StartTime"),
               // EndTime = metaData.Get<float>("EndTime"),

                PropBoneId = metaData.Get<int>("PropBoneId"),
            };

            return ouput;
        }


        public float StartTime { get; set; }
        public float EndTime { get; set; }

        public int PropBoneId{ get; set; }

    }
}
