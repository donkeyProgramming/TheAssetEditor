using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace View3D.Animation.AnimationChange
{
    public abstract class AnimationChangeRule
    {
        public virtual void ApplyBeforeWorldTransform(AnimationFrame frame) { }
        public virtual void ApplyAfterWorldTransform(AnimationFrame frame) { }
    }

    //class Splice : IAnimationChangeRule
    //{ 
    //    
    //}

    //public class Transform : AnimationChangeRule
    //{
    //
    //    public void ApplyBeforeWorldTransform(AnimationFrame frame)
    //    {
    //        frame.BoneTransforms[0].Rotation += Quaternion.Identity;
    //        frame.BoneTransforms[0].Translation += Vector3.Zero;
    //    }
    //}
}
