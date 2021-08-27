using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace View3D.Animation.AnimationChange
{
    public abstract class AnimationChangeRule
    {
        public virtual void ApplyBeforeWorldTransform(AnimationFrame frame) { }
        public virtual void ApplyRule(AnimationFrame frame, int boneId, float v) { }

        public virtual void ApplyRuleAfter(AnimationFrame frame, float time) { }
    }
}
