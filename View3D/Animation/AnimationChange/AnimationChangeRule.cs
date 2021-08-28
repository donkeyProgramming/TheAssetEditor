using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace View3D.Animation.AnimationChange
{
    public abstract class AnimationChangeRule
    {
        public virtual void TransformBone(AnimationFrame frame, int boneId, float v) { }

        public virtual void ApplyWorldTransform(AnimationFrame frame, float time) { }
    }
}
